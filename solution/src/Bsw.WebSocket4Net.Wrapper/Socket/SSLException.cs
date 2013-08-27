// Copyright 2013 BSW Technology Consulting, released under the BSD license - see LICENSING.txt at the top of this repository for details
 #region

using System;
using System.Linq;
using System.Linq.Expressions;
using System.Net.Security;

#endregion

namespace Bsw.WebSocket4Net.Wrapper.Socket
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