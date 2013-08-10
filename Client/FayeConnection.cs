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
        private readonly HandshakeResponseMessage _handshakeResponse;

        public FayeConnection(IWebSocket socket,
                              HandshakeResponseMessage handshakeResponse) : base(socket)
        {
            _handshakeResponse = handshakeResponse;
            _socket = socket;
            ClientId = handshakeResponse.ClientId;
        }

        public string ClientId { get; private set; }

        public async Task Disconnect()
        {
            if (_socket.State == WebSocketState.Closed)
            {
                throw new FayeConnectionException(ALREADY_DISCONNECTED);
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
                // TODO: Fix the timeout value
                result = await ExecuteControlMessage<SubscriptionResponseMessage>(message,
                                                                                  new TimeSpan(0,
                                                                                               0,
                                                                                               10));
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

        public Task Publish(string channel,
                            object message)
        {
            throw new NotImplementedException();
        }
    }
}