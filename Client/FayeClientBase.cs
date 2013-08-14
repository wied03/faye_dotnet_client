#region

using System;
using System.Linq;
using System.Threading.Tasks;
using Bsw.FayeDotNet.Messages;
using Bsw.FayeDotNet.Serialization;
using Bsw.WebSocket4NetSslExt.Socket;
using MsBw.MsBwUtility.Enum;
using MsBw.MsBwUtility.Tasks;
using NLog;
using SuperSocket.ClientEngine;

#endregion

namespace Bsw.FayeDotNet.Client
{
    public abstract class FayeClientBase
    {
        internal const string ONLY_SUPPORTED_CONNECTION_TYPE = "websocket";
        internal const string CONNECTION_TYPE_ERROR_FORMAT =
            "We only support 'websocket' and the server only supports [{0}] so we cannot communicate";
        private readonly IWebSocket _socket;
        protected readonly FayeJsonConverter Converter;
        protected int MessageCounter;

        // 10 seconds
        private static readonly TimeSpan DefaultHandshakeTimeout = new TimeSpan(0,
                                                                                0,
                                                                                10);

        private readonly Logger _logger;

        protected FayeClientBase(IWebSocket socket,
                                 int messageCounter,
                                 Logger logger)
            : this(socket,
                   messageCounter,
                   DefaultHandshakeTimeout,
                   logger)
        {
        }

        protected FayeClientBase(IWebSocket socket,
                                 int messageCounter,
                                 TimeSpan handshakeTimeout,
                                 Logger logger)
        {
            _logger = logger;
            _socket = socket;
            Converter = new FayeJsonConverter();
            MessageCounter = messageCounter;
            HandshakeTimeout = handshakeTimeout;
        }

        public TimeSpan HandshakeTimeout { get; set; }

        protected void SendConnect(string clientId)
        {
            var message = new ConnectRequestMessage(clientId: clientId,
                                                    connectionType: ONLY_SUPPORTED_CONNECTION_TYPE,
                                                    id: MessageCounter++);
            var json = Converter.Serialize(message);
            SocketSend(json);
        }

        protected void SocketSend(string data)
        {
            _logger.Debug("Sending message '{0}'",
                         data);
            _socket.Send(data);
        }

        private static TimeSpan FromMilliSecondsStr(string milliseconds)
        {
            var ms = Convert.ToInt32(milliseconds);
            return new TimeSpan(0,
                                0,
                                0,
                                0,
                                ms);
        }

        internal async Task<HandshakeResponseMessage> Handshake()
        {
            var message = new HandshakeRequestMessage(supportedConnectionTypes: new[] { ONLY_SUPPORTED_CONNECTION_TYPE },
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

        protected abstract Task<T> ExecuteSynchronousMessage<T>(BaseFayeMessage message,
                                                                TimeSpan timeoutValue) where T : BaseFayeMessage;

        protected static Advice ParseAdvice(dynamic message)
        {
            if (message.advice == null) return null;
            var advice = message.advice;
            var timeout = FromMilliSecondsStr((string) advice.timeout);
            var interval = FromMilliSecondsStr((string) advice.interval);
            var reconnect = ((string) advice.reconnect).EnumValue<Reconnect>();
            return new Advice(reconnect: reconnect,
                              interval: interval,
                              timeout: timeout);
        }

        protected async Task OpenWebSocket()
        {
            _logger.Debug("Connecting to websocket");
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
                    var error = String.Format("Timed out, waited {0} milliseconds to connect via websockets",
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
            _logger.Debug("Connected to websocket");
        }
    }
}