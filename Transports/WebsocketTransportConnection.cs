#region

using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using Bsw.WebSocket4NetSslExt.Socket;
using NLog;
using WebSocket4Net;

#endregion

namespace Bsw.FayeDotNet.Transports
{
    public class WebsocketTransportConnection : BaseWebsocket,
                                                ITransportConnection
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
        private readonly Queue<string> _outgoingMessageQueue;
        private readonly object _connectionStateMutex;

        // 5 seconds
        private static readonly TimeSpan DefaultRetryTimeout = new TimeSpan(0,
                                                                            0,
                                                                            5);

        public WebsocketTransportConnection(IWebSocket webSocket) : base(socket: webSocket,
                                                                         logger: Logger)
        {
            Socket.MessageReceived += WebSocketMessageReceived;
            Socket.Closed += WebSocketClosedWithRetry;
            RetryTimeout = DefaultRetryTimeout;
            ConnectionState = ConnectionState.Connected;
            _outgoingMessageQueue = new Queue<string>();
            _connectionStateMutex = new object();
            RetryEnabled = true;
        }

        private void WebSocketClosedOnPurpose(object sender,
                                              EventArgs e)
        {
            Logger.Info("Connection close complete");
            lock (_connectionStateMutex)
            {
                ConnectionState = ConnectionState.Disconnected;
            }
            if (ConnectionClosed != null)
            {
                ConnectionClosed(this,
                                 new EventArgs());
            }
        }

        private void WebSocketClosedWithRetry(object sender,
                                              EventArgs e)
        {
            lock (_connectionStateMutex)
            {
                ConnectionState = ConnectionState.Lost;
            }
            if (RetryEnabled)
            {
                Logger.Info("Lost connection, retrying in {0} milliseconds",
                            RetryTimeout.TotalMilliseconds);
                Task.Factory.StartNew(() => ReestablishConnection().Wait());
            }
            else
            {
                Logger.Info("Lost connection and not retrying");
            }
            if (ConnectionLost != null)
            {
                ConnectionLost(this,
                               new EventArgs());
            }
        }

        private async Task ReestablishConnection()
        {
            Thread.Sleep(RetryTimeout);
            Logger.Info("Retrying connection");
            lock (_connectionStateMutex)
            {
                ConnectionState = ConnectionState.Reconnecting;
            }
            await ConnectWebsocket();
            lock (_connectionStateMutex)
            {
                ConnectionState = ConnectionState.Connected;
            }
            if (ConnectionReestablished != null)
            {
                ConnectionReestablished(this,
                                        new EventArgs());
            }
            SendQueuedMessages();
        }

        private void SendQueuedMessages()
        {
            Logger.Info("Sending queued messages from when connection was lost");
            while (true)
            {
                string message;
                try
                {
                    // wait to dequeue until we successfully send the message
                    message = _outgoingMessageQueue.Peek();
                }
                catch (InvalidOperationException)
                {
                    break;
                }
                Send(message);
                _outgoingMessageQueue.Dequeue();
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
            lock (_connectionStateMutex)
            {
                if (ConnectionState == ConnectionState.Lost || ConnectionState == ConnectionState.Reconnecting)
                {
                    Logger.Debug("Connection was lost, queuing message");
                    _outgoingMessageQueue.Enqueue(message);
                }
                else
                {
                    Socket.Send(message);
                }
            }
        }

        public async Task Disconnect()
        {
            var considerDisconnected = (ConnectionState == ConnectionState.Disconnected) ||
                                       (ConnectionState == ConnectionState.Lost && !RetryEnabled);
            if (considerDisconnected)
            {
                Logger.Info("Already disconnected!");
                return;
            }
            Logger.Info("Disconnecting from websocket server");
            // We don't need the retry handler anymore
            DisableRetryHandler();
            var tcs = new TaskCompletionSource<bool>();
            EventHandler closed = (sender,
                                   args) => tcs.SetResult(true);
            Socket.Closed += closed;
            Socket.Close("Disconnection Requested");
            await tcs.Task;
        }

        public event MessageReceived MessageReceived;
        public event ConnectionEvent ConnectionClosed;
        public event ConnectionEvent ConnectionLost;
        public event ConnectionEvent ConnectionReestablished;
        public TimeSpan RetryTimeout { get; set; }
        public bool RetryEnabled { get; set; }
        public ConnectionState ConnectionState { get; private set; }

        private void DisableRetryHandler()
        {
            Socket.Closed -= WebSocketClosedWithRetry;
            Socket.Closed += WebSocketClosedOnPurpose;
        }

        public void NotifyOfPendingServerDisconnection()
        {
            DisableRetryHandler();
        }
    }
}