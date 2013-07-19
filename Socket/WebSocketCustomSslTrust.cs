#region

using System;
using System.Linq;
using System.Linq.Expressions;
using WebSocket4Net;

#endregion

namespace Bsw.WebSocket4NetSslExt.Socket
{
    public class WebSocketCustomSslTrust : WebSocket
    {
        public WebSocketCustomSslTrust(string uri,
            string subProtocol,
            WebSocketVersion version) : base(uri,
                subProtocol,
                version)
        {
        }
    }
}