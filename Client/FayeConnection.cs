#region

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Bsw.FayeDotNet.Messages;
using Bsw.FayeDotNet.Transports;
using Bsw.FayeDotNet.Utilities;
using MsBw.MsBwUtility.Enum;
using MsBw.MsBwUtility.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NLog;

#endregion

namespace Bsw.FayeDotNet.Client
{
    internal class FayeConnection : FayeClientBase,
                                    IFayeConnection
    {
        internal const string WILDCARD_CHANNEL_ERROR_FORMAT =
            "Wildcard channels (you tried to subscribe/unsubscribe from {0}) are not currently supported with this client";

        internal const string NOT_SUBSCRIBED =
            "You cannot unsubscribe from channel '{0}' because you were not subscribed to it first";

        private readonly ITransportConnection _connection;
        private readonly Dictionary<string, List<Action<string>>> _subscribedChannels;
        private readonly Dictionary<int, TaskCompletionSource<MessageReceivedArgs>> _synchronousMessageEvents;
        private readonly Logger _logger;
        public event ConnectionEvent ConnectionLost;
        public event ConnectionEvent ConnectionReestablished;
        private Advice _advice;

        public string ClientId { get; private set; }

        // internal to only allow FayeClient to instantiate this class
        internal FayeConnection(ITransportConnection connection,
                                HandshakeResponseMessage handshakeResponse,
                                int messageCounter,
                                Advice advice,
                                TimeSpan handshakeTimeout,
                                string connectionId) : base(messageCounter: messageCounter,
                                                            handshakeTimeout: handshakeTimeout)
        {
            _connection = connection;
            ClientId = handshakeResponse.ClientId;
            _connection.MessageReceived += SocketMessageReceived;
            _subscribedChannels = new Dictionary<string, List<Action<string>>>();
            _synchronousMessageEvents = new Dictionary<int, TaskCompletionSource<MessageReceivedArgs>>();
            _advice = advice;
            _connection.ConnectionLost += SocketConnectionLost;
            _connection.ConnectionReestablished += SocketConnectionReestablished;
            _logger = LoggerFetcher.GetLogger(connectionId,
                                              this);
        }

        private void SocketConnectionLost(object sender,
                                          EventArgs args)
        {
            if (ConnectionLost != null)
            {
                ConnectionLost(this,
                               new EventArgs());
            }
        }

        private void SocketConnectionReestablished(object sender,
                                                   EventArgs e)
        {
            Task.Factory.StartNew(ReestablishConnection);
        }

        private void ReestablishConnection()
        {
            SendConnect(ClientId,
                        _connection);
            _logger.Info("Connection re-established");
            if (ConnectionReestablished != null)
            {
                ConnectionReestablished(this,
                                        new EventArgs());
            }
        }

        private async Task<T> ExecuteSynchronousMessage<T>(BaseFayeMessage message) where T : BaseFayeMessage
        {
            return await ExecuteSynchronousMessage<T>(message: message,
                                                      timeoutValue: _advice.Timeout);
        }

        private async Task<T> ExecuteSynchronousMessage<T>(BaseFayeMessage message,
                                                           TimeSpan timeoutValue) where T : BaseFayeMessage
        {
            var json = Converter.Serialize(message);
            var tcs = new TaskCompletionSource<MessageReceivedArgs>();
            _synchronousMessageEvents[message.Id] = tcs;
            _connection.Send(json);
            var task = tcs.Task;
            var result = await task.Timeout(timeoutValue);
            if (result == Result.Timeout)
            {
                var timeoutException = new TimeoutException(timeoutValue,
                                                            json);
                _logger.ErrorException("Timeout problem, rethrowing",
                                       timeoutException);
                throw timeoutException;
            }
            _synchronousMessageEvents.Remove(message.Id);
            return Converter.Deserialize<T>(task.Result.Message);
        }

        private void SocketMessageReceived(object sender,
                                           MessageReceivedArgs e)
        {
            var array = JsonConvert.DeserializeObject<JArray>(e.Message);
            dynamic messageObj = array[0];
            var newAdvice = ParseAdvice(messageObj);
            if (newAdvice != null)
            {
                _advice = newAdvice;
                SetRetry(_advice,
                         _connection);
            }
            if (HandleSynchronousReply(messageObj,
                                       e)) return;
            if (HandleConnectResponse(messageObj)) return;
            var message = Converter.Deserialize<DataMessage>(e.Message);
            var channel = message.Channel;
            var messageData = message.Data.ToString(CultureInfo.InvariantCulture);
            _logger.Debug("Message data received for channel '{0}' is '{1}",
                          channel,
                          messageData);
            _subscribedChannels[channel].ForEach(handler => handler(messageData));
        }

        private bool HandleConnectResponse(dynamic message)
        {
            if (message.channel != MetaChannels.Connect.StringValue()) return false;
            _logger.Debug("Received connect response");
            return true;
        }

        private bool HandleSynchronousReply(dynamic message,
                                            MessageReceivedArgs e)
        {
            int messageId = message.id;
            bool isControlMessage = message.data == null;
            if (!isControlMessage || !_synchronousMessageEvents.ContainsKey(messageId)) return false;
            _synchronousMessageEvents[messageId].SetResult(e);
            return true;
        }

        public async Task Disconnect()
        {
            var connectionState = _connection.ConnectionState;
            var considerDisconnected = (connectionState == ConnectionState.Disconnected) ||
                                       (connectionState == ConnectionState.Lost && !_connection.RetryEnabled);
            if (considerDisconnected)
            {
                _logger.Info("Already disconnected");
                return;
            }
            _logger.Info("Disconnecting from FAYE server");
            var disconnectMessage = new DisconnectRequestMessage(clientId: ClientId,
                                                                 id: MessageCounter++);
            _connection.NotifyOfPendingServerDisconnection();
            var closedTcs = new TaskCompletionSource<bool>();
            _connection.ConnectionClosed += (sender,
                                             args) => closedTcs.SetResult(true);
            var disconResult = await ExecuteSynchronousMessage<DisconnectResponseMessage>(message: disconnectMessage);
            if (!disconResult.Successful)
            {
                throw new NotImplementedException();
            }
            // wait 60 seconds for the server to close the connection on its own (which it does after getting a disconnect call)
            await closedTcs.Task.WithTimeout(s => s,
                                             new TimeSpan(0,
                                                          0,
                                                          60));
        }

        public async Task Subscribe(string channel,
                                    Action<string> messageReceivedAction)
        {
            if (channel.Contains("*"))
            {
                throw new SubscriptionException(string.Format(WILDCARD_CHANNEL_ERROR_FORMAT,
                                                              channel));
            }
            if (_subscribedChannels.ContainsKey(channel))
            {
                _logger.Debug("Adding additional event for channel '{0}'",
                              channel);
                AddLocalChannelHandler(channel,
                                       messageReceivedAction);
                return;
            }
            await ExecuteSubscribe(channel);
            AddLocalChannelHandler(channel,
                                   messageReceivedAction);
            _logger.Info("Successfully subscribed to channel '{0}'",
                         channel);
        }

        private async Task ExecuteSubscribe(string channel)
        {
            _logger.Debug("Subscribing to channel '{0}'",
                          channel);
            var message = new SubscriptionRequestMessage(clientId: ClientId,
                                                         subscriptionChannel: channel,
                                                         id: MessageCounter++);

            var result = await ExecuteSynchronousMessage<SubscriptionResponseMessage>(message: message);
            if (!result.Successful) throw new SubscriptionException(result.Error);
        }

        private void AddLocalChannelHandler(string channel,
                                            Action<string> messageReceived)
        {
            var handlers = _subscribedChannels.ContainsKey(channel)
                               ? _subscribedChannels[channel]
                               : new List<Action<string>>();
            if (!handlers.Contains(messageReceived))
            {
                handlers.Add(messageReceived);
            }
            _subscribedChannels[channel] = handlers;
        }

        public async Task Unsubscribe(string channel)
        {
            if (channel.Contains("*"))
            {
                var error = string.Format(WILDCARD_CHANNEL_ERROR_FORMAT,
                                          channel);
                throw new SubscriptionException(error);
            }
            if (!_subscribedChannels.ContainsKey(channel))
            {
                var error = string.Format(NOT_SUBSCRIBED,
                                          channel);
                throw new SubscriptionException(error);
            }
            _logger.Debug("Unsubscribing from channel '{0}'",
                          channel);
            var message = new UnsubscribeRequestMessage(clientId: ClientId,
                                                        subscriptionChannel: channel,
                                                        id: MessageCounter++);

            var result = await ExecuteSynchronousMessage<UnsubscribeResponseMessage>(message: message);
            if (!result.Successful) throw new SubscriptionException(result.Error);
            _subscribedChannels.Remove(channel);
        }

        public async Task Publish(string channel,
                                  string message)
        {
            _logger.Debug("Publishing to channel '{0}' message '{1}'",
                          channel,
                          message);
            var msg = new DataMessage(channel: channel,
                                      clientId: ClientId,
                                      data: message,
                                      id: MessageCounter++);
            var result = await ExecuteSynchronousMessage<PublishResponseMessage>(message: msg);
            if (!result.Successful)
            {
                throw new PublishException(result.Error);
            }
        }
    }
}