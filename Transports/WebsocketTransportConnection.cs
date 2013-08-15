#region

using System;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using Bsw.FayeDotNet.Client;
using Bsw.WebSocket4NetSslExt.Socket;
using NLog;
using WebSocket4Net;

#endregion

namespace Bsw.FayeDotNet.Transports
{
    public class WebsocketTransportConnection : BaseWebsocket,
                                                ITransportConnection
    {
        private const string ALREADY_DISCONNECTED = "Already disconnected";
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        // 5 seconds
        private static readonly TimeSpan DefaultRetryTimeout = new TimeSpan(0,
                                                                            0,
                                                                            5);

        public WebsocketTransportConnection(IWebSocket webSocket) : base(socket: webSocket,
                                                                         logger: Logger)
        {
            Socket.MessageReceived += WebSocketMessageReceived;
            Socket.Closed += WebSocketClosed;
            RetryTimeout = DefaultRetryTimeout;
            Closed = false;
        }

        private void WebSocketClosed(object sender,
                                     EventArgs e)
        {
            Logger.Info("Lost connection, retrying in {0} milliseconds",
                        RetryTimeout.TotalMilliseconds);
            if (ConnectionLost != null)
            {
                ConnectionLost(this,
                               new EventArgs());
            }
            Closed = true;
            Task.Factory.StartNew(() => ReestablishConnection().Wait());
        }

        private async Task ReestablishConnection()
        {
            Thread.Sleep(RetryTimeout);
            Logger.Info("Retrying connection");
            await ConnectWebsocket();
            Closed = false;
            if (ConnectionReestablished != null)
            {
                ConnectionReestablished(this,
                                        new EventArgs());
            }
        }

        private void WebSocketMessageReceived(object sender,
                                              MessageReceivedEventArgs e)
        {
            Logger.Debug("Received raw message '{0}'",
                         e.Message);
            if (MessageReceived != null)
            {
                MessageReceived(sender,
                                new MessageReceivedArgs(e.Message));
            }
        }

        public void Send(string message)
        {
            Logger.Debug("Sending message '{0}'",
                         message);
            Socket.Send(message);
        }

        public async Task Disconnect()
        {
            if (Socket.State == WebSocketState.Closed)
            {
                throw new FayeConnectionException(ALREADY_DISCONNECTED);
            }
            Logger.Info("Disconnecting from websocket server");
            // We don't need the retry handler anymore
            Socket.Closed -= WebSocketClosed;
            var tcs = new TaskCompletionSource<bool>();
            EventHandler closed = (sender,
                                   args) => tcs.SetResult(true);
            Socket.Closed += closed;
            Socket.Close("Disconnection Requested");
            await tcs.Task;
            Closed = true;
        }

        public event MessageReceived MessageReceived;
        public event ConnectionEvent ConnectionLost;
        public event ConnectionEvent ConnectionReestablished;
        public TimeSpan RetryTimeout { get; set; }
        public bool Closed { get; private set; }
    }
}