#region

using System;
using System.Threading.Tasks;
using Bsw.FayeDotNet.Client;
using Bsw.WebSocket4NetSslExt.Socket;
using MsBw.MsBwUtility.Tasks;
using NLog;
using SuperSocket.ClientEngine;

#endregion

namespace Bsw.FayeDotNet.Transports
{
    public abstract class BaseWebsocket
    {
        protected IWebSocket Socket { get; private set; }

        private readonly Logger _logger;
        public abstract TimeSpan ConnectionOpenTimeout { get; set; }

        protected BaseWebsocket(IWebSocket socket,
                                Logger logger)
        {
            _logger = logger;
            Socket = socket;
        }

        protected async Task ConnectWebsocket()
        {
            _logger.Debug("Connecting to websocket");
            var tcs = new TaskCompletionSource<bool>();
            EventHandler socketOnOpened = (sender,
                                           args) => tcs.SetResult(true);
            Socket.Opened += socketOnOpened;
            Exception exception = null;
            EventHandler<ErrorEventArgs> socketOnError = (sender,
                                                          args) =>
                                                         {
                                                             exception = args.Exception;
                                                             tcs.SetResult(false);
                                                         };
            Socket.Error += socketOnError;
            Socket.Open();
            var task = tcs.Task;
            var result = await task.Timeout(ConnectionOpenTimeout);
            try
            {
                if (result == Result.Timeout)
                {
                    var error = String.Format("Timed out, waited {0} milliseconds to connect via websockets",
                                              ConnectionOpenTimeout.TotalMilliseconds);
                    throw new FayeConnectionException(error);
                }
                if (!task.Result)
                {
                    throw exception;
                }
            }
            finally
            {
                Socket.Error -= socketOnError;
                Socket.Opened -= socketOnOpened;
            }
            _logger.Debug("Connected to websocket");
        }
    }
}