#region

using System;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Bsw.WebSocket4NetSslExt.Socket;
using NLog;

#endregion

namespace Bsw.FayeDotNet.Transports
{
    public class WebsocketTransportClient : BaseWebsocket,
                                            ITransportClient
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
        private TimeSpan _connectionOpenTimeout;

        public static readonly TimeSpan DefaultConnectionOpenTimeout = new TimeSpan(0,
                                                                                    0,
                                                                                    10);

        public WebsocketTransportClient(IWebSocket webSocket)
            : base(socket: webSocket,
                   logger: Logger)
        {
            _connectionOpenTimeout = DefaultConnectionOpenTimeout;
        }

        public async Task<ITransportConnection> Connect()
        {
            await ConnectWebsocket();
            return new WebsocketTransportConnection(Socket,
                                                    // share the same timeout setting
                                                    () => _connectionOpenTimeout,
                                                    t => _connectionOpenTimeout = t);
        }

        public override TimeSpan ConnectionOpenTimeout
        {
            get { return _connectionOpenTimeout; }
            set { _connectionOpenTimeout = value; }
        }
    }
}