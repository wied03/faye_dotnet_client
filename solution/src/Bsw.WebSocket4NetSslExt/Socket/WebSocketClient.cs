#region

using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using WebSocket4Net;

#endregion

namespace Bsw.WebSocket4NetSslExt.Socket
{
    public class WebSocketClient : WebSocket,
                                   IWebSocket
    {
        public WebSocketClient(string uri,
                               string subProtocol,
                               WebSocketVersion version) : base(uri,
                                                                subProtocol,
                                                                version)
        {
        }

        public WebSocketClient(string uri,
                               string subProtocol = "",
                               List<KeyValuePair<string, string>> cookies = null,
                               List<KeyValuePair<string, string>> customHeaderItems = null,
                               string userAgent = "",
                               string origin = "",
                               WebSocketVersion version = WebSocketVersion.None) : base(uri,
                                                                                        subProtocol,
                                                                                        cookies,
                                                                                        customHeaderItems,
                                                                                        userAgent,
                                                                                        origin,
                                                                                        version)
        {
        }
    }
}