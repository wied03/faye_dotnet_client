#region

using System;
using Bsw.FayeDotNet.Messages;
using Bsw.FayeDotNet.Serialization;
using Bsw.WebSocket4NetSslExt.Socket;

#endregion

namespace Bsw.FayeDotNet.Client
{
    public abstract class FayeClientBase
    {
        internal const string ONLY_SUPPORTED_CONNECTION_TYPE = "websocket";
        private readonly IWebSocket _socket;
        protected readonly FayeJsonConverter Converter;
        protected int MessageCounter;

        protected FayeClientBase(IWebSocket socket, int messageCounter)
        {
            _socket = socket;
            Converter = new FayeJsonConverter();
            MessageCounter = messageCounter;
        }

        protected void SendConnect(string clientId)
        {
            var message = new ConnectRequestMessage(clientId: clientId,
                                                    connectionType: ONLY_SUPPORTED_CONNECTION_TYPE,
                                                    id: MessageCounter++);
            var json = Converter.Serialize(message);
            _socket.Send(json);
        }
    }
}