#region

using System;
using System.Threading.Tasks;
using Bsw.FayeDotNet.Messages;
using Bsw.FayeDotNet.Serialization;
using Bsw.WebSocket4NetSslExt.Socket;
using MsBw.MsBwUtility.Tasks;
using WebSocket4Net;

#endregion

namespace Bsw.FayeDotNet.Client
{
    public abstract class FayeClientBase
    {
        internal const string ONLY_SUPPORTED_CONNECTION_TYPE = "websocket";
        private readonly IWebSocket _socket;
        protected readonly FayeJsonConverter Converter;

        protected FayeClientBase(IWebSocket socket)
        {
            _socket = socket;
            Converter = new FayeJsonConverter();
        }

        protected async Task<T> ExecuteSynchronousMessage<T>(BaseFayeMessage message,
                                                             TimeSpan timeoutValue) where T : BaseFayeMessage
        {
            var json = Converter.Serialize(message);
            var tcs = new TaskCompletionSource<MessageReceivedEventArgs>();
            EventHandler<MessageReceivedEventArgs> received = (sender,
                                                               args) => tcs.SetResult(args);
            _socket.MessageReceived += received;
            _socket.Send(json);
            var task = tcs.Task;
            var result = await task.Timeout(timeoutValue);
            if (result == Result.Timeout)
            {
                throw new TimeoutException();
            }
            _socket.MessageReceived -= received;
            return Converter.Deserialize<T>(task.Result.Message);
        }

        protected void SendConnect(string clientId)
        {
            var message = new ConnectRequestMessage(clientId: clientId,
                                                    connectionType: ONLY_SUPPORTED_CONNECTION_TYPE);
            var json = Converter.Serialize(message);
            _socket.Send(json);
        }
    }
}