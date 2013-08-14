#region

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using Bsw.FayeDotNet.Messages;
using Bsw.WebSocket4NetSslExt.Socket;
using MsBw.MsBwUtility.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using WebSocket4Net;

#endregion

namespace Bsw.FayeDotNet.Client
{
    internal class FayeConnection : FayeClientBase,
                                    IFayeConnection
    {
        internal const string ALREADY_DISCONNECTED = "Already disconnected";

        internal const string WILDCARD_CHANNEL_ERROR_FORMAT =
            "Wildcard channels (you tried to subscribe/unsubscribe from {0}) are not currently supported with this client";

        internal const string NOT_SUBSCRIBED =
            "You cannot unsubscribe from channel '{0}' because you were not subscribed to it first";

        private readonly IWebSocket _socket;
        private readonly Dictionary<string, List<Action<string>>> _subscribedChannels;
        private readonly Dictionary<int, TaskCompletionSource<MessageReceivedEventArgs>> _synchronousMessageEvents;
        public event ConnectionEvent ConnectionLost;
        public event ConnectionEvent ConnectionReestablished;

        private static readonly TimeSpan StandardCommandTimeout = new TimeSpan(0,
                                                                               0,
                                                                               10);

        // 5 seconds
        private static readonly TimeSpan RetryTimeout = new TimeSpan(0,
                                                                     0,
                                                                     5);

        private Advice _advice;

        public string ClientId { get; private set; }

        public FayeConnection(IWebSocket socket,
                              HandshakeResponseMessage handshakeResponse,
                              int messageCounter,
                              Advice advice,
                              TimeSpan handshakeTimeout) : base(socket: socket,
                                                                messageCounter: messageCounter,
                                                                handshakeTimeout: handshakeTimeout)
        {
            _socket = socket;
            ClientId = handshakeResponse.ClientId;
            _socket.MessageReceived += SocketMessageReceived;
            _subscribedChannels = new Dictionary<string, List<Action<string>>>();
            _synchronousMessageEvents = new Dictionary<int, TaskCompletionSource<MessageReceivedEventArgs>>();
            _advice = advice;
            _socket.Closed += SocketClosed;
        }

        private void SocketClosed(object sender,
                                  EventArgs e)
        {
            if (ConnectionLost != null)
            {
                ConnectionLost(this,
                               new EventArgs());
            }
            Task.Factory.StartNew(() =>
                                  {
                                      Thread.Sleep(RetryTimeout);
                                      OpenWebSocket().Wait();
                                      if (ConnectionReestablished != null)
                                      {
                                          ConnectionReestablished(this,
                                                                  new EventArgs());
                                      }
                                  });
        }

        private async Task<T> ExecuteSynchronousMessage<T>(BaseFayeMessage message,
                                                           TimeSpan timeoutValue) where T : BaseFayeMessage
        {
            var json = Converter.Serialize(message);
            var tcs = new TaskCompletionSource<MessageReceivedEventArgs>();
            _synchronousMessageEvents[message.Id] = tcs;
            _socket.Send(json);
            var task = tcs.Task;
            var result = await task.Timeout(timeoutValue);
            if (result == Result.Timeout)
            {
                throw new TimeoutException();
            }
            _synchronousMessageEvents.Remove(message.Id);
            return Converter.Deserialize<T>(task.Result.Message);
        }

        private void SocketMessageReceived(object sender,
                                           MessageReceivedEventArgs e)
        {
            var newAdvice = ParseAdvice(e);
            if (newAdvice != null)
            {
                _advice = newAdvice;
            }
            if (HandleSynchronousReply(e)) return;
            var message = Converter.Deserialize<DataMessage>(e.Message);
            var channel = message.Channel;
            var messageData = message.Data.ToString(CultureInfo.InvariantCulture);
            Logger.Debug("Message data received for channel '{0}' is '{1}",
                         channel,
                         messageData);
            _subscribedChannels[channel].ForEach(handler => handler(messageData));
        }

        private bool HandleSynchronousReply(MessageReceivedEventArgs e)
        {
            var array = JsonConvert.DeserializeObject<JArray>(e.Message);
            dynamic receivedAnonObj = array[0];
            int messageId = receivedAnonObj.id;
            bool isControlMessage = receivedAnonObj.data == null;
            if (!isControlMessage || !_synchronousMessageEvents.ContainsKey(messageId)) return false;
            _synchronousMessageEvents[messageId].SetResult(e);
            return true;
        }

        public async Task Disconnect()
        {
            if (_socket.State == WebSocketState.Closed)
            {
                throw new FayeConnectionException(ALREADY_DISCONNECTED);
            }
            Logger.Info("Disconnecting from FAYE server");
            // We don't need the retry handler anymore
            _socket.Closed -= SocketClosed;
            var disconnectMessage = new DisconnectRequestMessage(clientId: ClientId,
                                                                 id: MessageCounter++);
            var disconResult = await ExecuteSynchronousMessage<DisconnectResponseMessage>(message: disconnectMessage,
                                                                                          timeoutValue:
                                                                                              StandardCommandTimeout);
            if (!disconResult.Successful)
            {
                throw new NotImplementedException();
            }

            var tcs = new TaskCompletionSource<bool>();
            EventHandler closed = (sender,
                                   args) => tcs.SetResult(true);
            _socket.Closed += closed;
            _socket.Close("Disconnection Requested");
            await tcs.Task;
        }

        public async Task Subscribe(string channel,
                                    Action<string> messageReceived)
        {
            if (channel.Contains("*"))
            {
                throw new SubscriptionException(string.Format(WILDCARD_CHANNEL_ERROR_FORMAT,
                                                              channel));
            }
            if (_subscribedChannels.ContainsKey(channel))
            {
                Logger.Debug("Adding additional event for channel '{0}'",
                             channel);
                AddLocalChannelHandler(channel,
                                       messageReceived);
                return;
            }
            Logger.Debug("Subscribing to channel '{0}'",
                         channel);
            var message = new SubscriptionRequestMessage(clientId: ClientId,
                                                         subscriptionChannel: channel,
                                                         id: MessageCounter++);

            var result = await ExecuteSynchronousMessage<SubscriptionResponseMessage>(message: message,
                                                                                      timeoutValue:
                                                                                          StandardCommandTimeout);

            if (!result.Successful) throw new SubscriptionException(result.Error);
            AddLocalChannelHandler(channel,
                                   messageReceived);
            Logger.Info("Successfully subscribed to channel '{0}'",
                        channel);
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
            Logger.Debug("Unsubscribing from channel '{0}'",
                         channel);
            var message = new UnsubscribeRequestMessage(clientId: ClientId,
                                                        subscriptionChannel: channel,
                                                        id: MessageCounter++);

            var result = await ExecuteSynchronousMessage<UnsubscribeResponseMessage>(message: message,
                                                                                     timeoutValue:
                                                                                         StandardCommandTimeout);

            if (!result.Successful) throw new SubscriptionException(result.Error);
            _subscribedChannels.Remove(channel);
        }

        public async Task Publish(string channel,
                                  string message)
        {
            Logger.Debug("Publishing to channel '{0}' message '{1}'",
                         channel,
                         message);
            var msg = new DataMessage(channel: channel,
                                      clientId: ClientId,
                                      data: message,
                                      id: MessageCounter++);
            var result = await ExecuteSynchronousMessage<PublishResponseMessage>(message: msg,
                                                                                 timeoutValue: StandardCommandTimeout);
            if (!result.Successful)
            {
                throw new PublishException(result.Error);
            }
        }
    }
}