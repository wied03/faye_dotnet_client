// Copyright 2013 BSW Technology Consulting, released under the BSD license - see LICENSING.txt at the top of this repository for details
 #region

using System;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Bsw.WebSocket4Net.Wrapper.Socket;

#endregion

namespace Bsw.FayeDotNet.Transports
{
    public class WebsocketTransportClient : BaseWebsocket,
                                            ITransportClient
    {
        private TimeSpan _connectionOpenTimeout;

        public static readonly TimeSpan DefaultConnectionOpenTimeout = new TimeSpan(0,
                                                                                    0,
                                                                                    10);

        private readonly string _connectionId;

        /// <summary>
        ///     Build a new transport client
        /// </summary>
        /// <param name="webSocket">Websocket to use</param>
        /// <param name="connectionId">Optional connection ID to include in log files</param>
        public WebsocketTransportClient(IWebSocket webSocket,
                                        string connectionId = "standard")
            : base(socket: webSocket,
                   connectionId: connectionId)
        {
            _connectionId = connectionId;
            _connectionOpenTimeout = DefaultConnectionOpenTimeout;
        }

        public async Task<ITransportConnection> Connect()
        {
            await ConnectWebsocket();
            return new WebsocketTransportConnection(webSocket: Socket,
                                                    // share the same timeout setting
                                                    connectionOpenTimeoutFetch: () => _connectionOpenTimeout,
                                                    connectionOpenTimeoutSetter: t => _connectionOpenTimeout = t,
                                                    connectionId: _connectionId);
        }

        public override TimeSpan ConnectionOpenTimeout
        {
            get { return _connectionOpenTimeout; }
            set { _connectionOpenTimeout = value; }
        }
    }
}