#region

using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Bsw.FayeDotNet.Messages;
using Bsw.WebSocket4NetSslExt.Socket;
using WebSocket4Net;

#endregion

namespace Bsw.FayeDotNet.Client
{
    internal class FayeConnection : FayeClientBase,
                                    IFayeConnection
    {
        internal const string ALREADY_DISCONNECTED = "Already disconnected";
        private readonly IWebSocket _socket;
        private readonly Dictionary<string, List<Action<string>>> _subscribedChannels;

        private static readonly TimeSpan StandardCommandTimeout = new TimeSpan(0,
                                                                               0,
                                                                               10);

        public string ClientId { get; private set; }

        public FayeConnection(IWebSocket socket,
                              HandshakeResponseMessage handshakeResponse) : base(socket)
        {
            _socket = socket;
            ClientId = handshakeResponse.ClientId;
            _socket.MessageReceived += SocketMessageReceived;
            _subscribedChannels = new Dictionary<string, List<Action<string>>>();
        }

        private void SocketMessageReceived(object sender,
                                           MessageReceivedEventArgs e)
        {
            var message = Converter.Deserialize<DataMessage>(e.Message);
            var channel = message.Channel;
            if (_subscribedChannels.ContainsKey(channel))
            {
                _subscribedChannels[channel].ForEach(handler => handler(message.Data.ToString()));
            }
        }

        public async Task Disconnect()
        {
            if (_socket.State == WebSocketState.Closed)
            {
                throw new FayeConnectionException(ALREADY_DISCONNECTED);
            }
            _socket.MessageReceived -= SocketMessageReceived;
            DisconnectResponseMessage disconResult;
            try
            {
                var disconnectMessage = new DisconnectRequestMessage(ClientId);
                disconResult = await ExecuteControlMessage<DisconnectResponseMessage>(message: disconnectMessage,
                                                                                      timeoutValue:
                                                                                          StandardCommandTimeout);
            }
            catch (TimeoutException)
            {
                throw new NotImplementedException();
            }
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
            var message = new SubscriptionRequestMessage(ClientId,
                                                         channel);
            SubscriptionResponseMessage result;
            try
            {
                result = await ExecuteControlMessage<SubscriptionResponseMessage>(message: message,
                                                                                  timeoutValue: StandardCommandTimeout);
            }
            catch (TimeoutException)
            {
                throw new NotImplementedException();
            }
            if (!result.Successful) throw new NotImplementedException();
            var handlers = _subscribedChannels.ContainsKey(channel)
                               ? _subscribedChannels[channel]
                               : new List<Action<string>>();
            if (!handlers.Contains(messageReceived))
            {
                handlers.Add(messageReceived);
            }
            _subscribedChannels[channel] = handlers;
        }

        public Task Unsubscribe(string channel)
        {
            throw new NotImplementedException();
        }

        public void Publish(string channel,
                            string message)
        {
            var msg = new DataMessage(channel: channel,
                                      clientId: ClientId,
                                      data: message);
            var json = Converter.Serialize(msg);
            _socket.Send(json);
        }
    }
}