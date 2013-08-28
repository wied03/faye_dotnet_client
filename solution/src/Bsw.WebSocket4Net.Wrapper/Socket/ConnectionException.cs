// Copyright 2013 BSW Technology Consulting, released under the BSD license - see LICENSING.txt at the top of this repository for details
 #region

using System;
using System.Linq;
using System.Linq.Expressions;

#endregion

namespace Bsw.WebSocket4Net.Wrapper.Socket
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