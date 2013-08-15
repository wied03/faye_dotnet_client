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

        public WebsocketTransportClient(IWebSocket webSocket)
            : base(socket: webSocket,
                   logger: Logger)
        {
        }

        public async Task<ITransportConnection> Connect()
        {
            await ConnectWebsocket();
            return new WebsocketTransportConnection(Socket);
        }
    }
}