#region

using System;
using System.Linq;
using System.Linq.Expressions;

#endregion

namespace Bsw.WebSocket4NetSslExt.Socket
{
    public class ConnectionException : Exception
    {
        public ConnectionException(string message) : base(message)
        {
        }

        public ConnectionException(string message,
                                   Exception e) : base(message,
                                                       e)
        {
            
        }
    }
}