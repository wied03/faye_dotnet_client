#region

using System;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Bsw.FayeDotNet.Messages;
using Bsw.WebSocket4NetSslExt.Socket;
using MsBw.MsBwUtility.Tasks;
using NLog;
using SuperSocket.ClientEngine;
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
                                                                  timeout: new TimeSpan(0, 0, 60));
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        public FayeClient(IWebSocket socket) : base(socket: socket,
                                                    messageCounter: FIRST_MESSAGE_INDEX)
        {
            _socket = socket;
            HandshakeTimeout = new TimeSpan(0,
                                            0,
                                            10);
            _advice = DefaultAdvice;
        }

        public TimeSpan HandshakeTimeout { get; set; }

        public async Task<IFayeConnection> Connect()
        {
            await OpenWebSocket();
            var handshakeResponse = await Handshake();
            SendConnect(handshakeResponse.ClientId);
            return new FayeConnection(socket: _socket,
                                      handshakeResponse: handshakeResponse,
                                      messageCounter: MessageCounter,
                                      advice: _advice);
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

        private async Task OpenWebSocket()
        {
            Logger.Debug("Connecting to websocket");
            var tcs = new TaskCompletionSource<bool>();
            EventHandler socketOnOpened = (sender,
                                           args) => tcs.SetResult(true);
            _socket.Opened += socketOnOpened;
            Exception exception = null;
            EventHandler<ErrorEventArgs> socketOnError = (sender,
                                                          args) =>
                                                         {
                                                             exception = args.Exception;
                                                             tcs.SetResult(false);
                                                         };
            _socket.Error += socketOnError;
            _socket.Open();
            var task = tcs.Task;
            var result = await task.Timeout(HandshakeTimeout);
            try
            {
                if (result == Result.Timeout)
                {
                    var error = string.Format("Timed out, waited {0} milliseconds to connect via websockets",
                                              HandshakeTimeout.TotalMilliseconds);
                    throw new FayeConnectionException(error);
                }
                if (!task.Result)
                {
                    throw exception;
                }
            }
            finally
            {
                _socket.Error -= socketOnError;
                _socket.Opened -= socketOnOpened;
            }
            Logger.Debug("Connected to websocket");
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