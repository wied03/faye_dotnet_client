#region

using System;
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

        private static readonly TimeSpan StandardCommandTimeout = new TimeSpan(0,
                                                                               0,
                                                                               10);

        public string ClientId { get; private set; }

        public FayeConnection(IWebSocket socket,
                              HandshakeResponseMessage handshakeResponse) : base(socket)
        {
            _socket = socket;
            ClientId = handshakeResponse.ClientId;
            _socket.MessageReceived += _socket_MessageReceived;
        }

        private void _socket_MessageReceived(object sender,
                                             MessageReceivedEventArgs e)
        {
            var foo = "hi";
        }

        public async Task Disconnect()
        {
            if (_socket.State == WebSocketState.Closed)
            {
                throw new FayeConnectionException(ALREADY_DISCONNECTED);
            }
            _socket.MessageReceived -= _socket_MessageReceived;
            DisconnectResponseMessage disconResult;
            try
            {
                var disconnectMessage = new DisconnectRequestMessage(ClientId);
                disconResult = await ExecuteControlMessage<DisconnectResponseMessage>(message: disconnectMessage,
                                                                                       timeoutValue: StandardCommandTimeout);
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
                                    Action<object> messageReceived)
        {
            var message = new SubscriptionRequestMessage(ClientId,
                                                         channel);
            SubscriptionResponseMessage result;
            try
            {
                result = await ExecuteControlMessage<SubscriptionResponseMessage>(message,
                                                                                  StandardCommandTimeout);
            }
            catch (TimeoutException)
            {
                throw new NotImplementedException();
            }
            if (result.Successful)
            {
                return;
            }
            throw new NotImplementedException();
        }

        public Task Unsubscribe(string channel)
        {
            throw new NotImplementedException();
        }

        public void Publish(string channel,
                            object message)
        {
            var msg = new DataMessageRequest(channel: channel,
                                             clientId: ClientId,
                                             data: message);
            var json = Converter.Serialize(msg);
            _socket.Send(json);
        }
    }
}