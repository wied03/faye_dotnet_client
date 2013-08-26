#region

using System;
using System.Linq;
using System.Linq.Expressions;
using System.Net.Security;

#endregion

namespace Bsw.WebSocket4NetSslExt.Socket
{
    public class SSLException : Exception
    {
        public SSLException(SslPolicyErrors policyErrors,
                            Exception error)
            : base(string.Format("The server certificate is not trusted{0}!",
                                 policyErrors == SslPolicyErrors.None
                                     ? string.Empty
                                     : " ("+policyErrors+")"),
                   error)
        {
        }
    }
}