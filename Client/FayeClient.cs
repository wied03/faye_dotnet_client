#region

using System;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Bsw.FayeDotNet.Messages;
using Bsw.WebSocket4NetSslExt.Socket;
using MsBw.MsBwUtility.Tasks;
using WebSocket4Net;

#endregion

namespace Bsw.FayeDotNet.Client
{
    /// <summary>
    ///     Implementation with 10 second default timeout
    /// </summary>
    public class FayeClient : FayeClientBase,
                              IFayeClient
    {
        internal const string CONNECTION_TYPE_ERROR_FORMAT =
            "We only support 'websocket' and the server only supports [{0}] so we cannot communicate";

        private const int FIRST_MESSAGE_INDEX = 1;
        private readonly IWebSocket _socket;
        private Advice _advice;

        private static readonly Advice DefaultAdvice = new Advice(reconnect: Reconnect.Retry,
                                                                  interval: new TimeSpan(0),
                                                                  timeout: new TimeSpan(0,
                                                                                        0,
                                                                                        60));

        public FayeClient(IWebSocket socket) : base(socket: socket,
                                                    messageCounter: FIRST_MESSAGE_INDEX)
        {
            _socket = socket;
            _advice = DefaultAdvice;
        }

        public async Task<IFayeConnection> Connect()
        {
            await OpenWebSocket();
            var handshakeResponse = await Handshake();
            SendConnect(handshakeResponse.ClientId);
            return new FayeConnection(socket: _socket,
                                      handshakeResponse: handshakeResponse,
                                      messageCounter: MessageCounter,
                                      advice: _advice,
                                      handshakeTimeout: HandshakeTimeout);
        }

        private async Task<HandshakeResponseMessage> Handshake()
        {
            var message = new HandshakeRequestMessage(supportedConnectionTypes: new[] {ONLY_SUPPORTED_CONNECTION_TYPE},
                                                      id: MessageCounter++);
            HandshakeResponseMessage result;
            try
            {
                result = await ExecuteSynchronousMessage<HandshakeResponseMessage>(message,
                                                                                   HandshakeTimeout);
            }
            catch (TimeoutException)
            {
                throw new HandshakeException(HandshakeTimeout);
            }
            if (!result.Successful) throw new HandshakeException(result.Error);
            if (result.SupportedConnectionTypes.Contains(ONLY_SUPPORTED_CONNECTION_TYPE)) return result;
            var flatTypes = result
                .SupportedConnectionTypes
                .Select(ct => "'" + ct + "'")
                .Aggregate((c1,
                            c2) => c1 + "," + c2);
            var error = string.Format(CONNECTION_TYPE_ERROR_FORMAT,
                                      flatTypes);
            throw new HandshakeException(error);
        }

        private async Task<T> ExecuteSynchronousMessage<T>(BaseFayeMessage message,
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
            var newAdvice = ParseAdvice(task.Result);
            if (newAdvice != null)
            {
                _advice = newAdvice;
            }
            return Converter.Deserialize<T>(task.Result.Message);
        }
    }
}