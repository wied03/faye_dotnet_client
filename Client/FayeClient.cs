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

#endregion

namespace Bsw.FayeDotNet.Client
{
    /// <summary>
    ///     Implementation with 10 second default timeout
    /// </summary>
    public class FayeClient : FayeClientBase,
                              IFayeClient
    {
        private const string ONLY_SUPPORTED_CONNECTION_TYPE = "websocket";
        private readonly IWebSocket _socket;
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        public FayeClient(IWebSocket socket) : base(socket)
        {
            _socket = socket;
            HandshakeTimeout = new TimeSpan(0,
                                            0,
                                            10);
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
            HandshakeResponseMessage result;
            try
            {
                result = await ExecuteControlMessage<HandshakeResponseMessage>(message,
                                                                               HandshakeTimeout);
            }
            catch (TimeoutException)
            {
                throw new HandshakeException(HandshakeTimeout);
            }
            if (result.Successful)
            {
                return new FayeConnection(_socket,
                                          result);
            }
            throw new HandshakeException(result.Error);
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