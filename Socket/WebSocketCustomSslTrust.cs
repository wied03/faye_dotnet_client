#region

using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Security.Cryptography.X509Certificates;
using WebSocket4Net;

#endregion

namespace Bsw.WebSocket4NetSslExt.Socket
{
    public class WebSocketCustomSslTrust : WebSocket,
                                           IWebSocket
    {
        private readonly IEnumerable<X509Certificate> _trustedCertChain;

        /// <summary>
        ///     Constructs a websocket that verifies the server's certificate is signed by someone we explicitly trust
        ///     before we open the websocket
        /// </summary>
        /// <param name="uri">URI to connect to</param>
        /// <param name="trustedCertChain">
        ///     List of certificates that should be trusted.  If any certificate in the server's chain
        ///     matches these certs, the verification will pass.  Otherwise a ConnectionException is thrown
        ///     when Open() is called.
        /// </param>
        public WebSocketCustomSslTrust(string uri,
                                       IEnumerable<X509Certificate> trustedCertChain) : base(uri)
        {
            _trustedCertChain = trustedCertChain;
        }

        public new void Open()
        {
            base.Open();
        }
    }
}