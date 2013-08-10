#region

using System;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Bsw.FayeDotNet.Messages;
using Bsw.FayeDotNet.Serialization;
using Bsw.WebSocket4NetSslExt.Socket;
using MsBw.MsBwUtility.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using NLog;
using WebSocket4Net;
using ErrorEventArgs = SuperSocket.ClientEngine.ErrorEventArgs;

#endregion

namespace Bsw.FayeDotNet.Client
{
    /// <summary>
    ///     Implementation with 10 second default timeout
    /// </summary>
    public class FayeClient : IFayeClient
    {
        private const string ONLY_SUPPORTED_CONNECTION_TYPE = "websocket";
        internal const string SUCCESSFUL_FALSE = "Received a succcessful false message from server";
        private readonly IWebSocket _socket;
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
        private readonly FayeJsonConverter _converter;

        public FayeClient(IWebSocket socket)
        {
            _socket = socket;
            HandshakeTimeout = new TimeSpan(0,
                                            0,
                                            10);
            _converter = new FayeJsonConverter();
        }

        public TimeSpan HandshakeTimeout { get; set; }

        public async Task<IFayeConnection> Connect()
        {
            await OpenWebSocket();
            return await Handshake();
        }

        private async Task<IFayeConnection> Handshake()
        {
            var message = new HandshakeRequestMessage(new[] {ONLY_SUPPORTED_CONNECTION_TYPE});
            var result = await ExecuteControlMessage<HandshakeResponseMessage>(message);
            if (result.Successful)
            {
                return new FayeConnection(_socket,
                                          result);
            }
            throw new HandshakeException(SUCCESSFUL_FALSE);
        }

        private async Task<T> ExecuteControlMessage<T>(BaseFayeMessage message) where T : BaseFayeMessage
        {
            var json = _converter.Serialize(message);
            var tcs = new TaskCompletionSource<MessageReceivedEventArgs>();
            EventHandler<MessageReceivedEventArgs> received = (sender,
                                                               args) => tcs.SetResult(args);
            _socket.MessageReceived += received;
            _socket.Send(json);
            var result = await tcs.Task;
            _socket.MessageReceived -= received;
            return _converter.Deserialize<T>(result.Message);
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
    }
}