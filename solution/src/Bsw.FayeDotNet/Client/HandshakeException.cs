// Copyright 2013 BSW Technology Consulting, released under the BSD license - see LICENSING.txt at the top of this repository for details
﻿#region

using System;
using System.Linq;
using System.Linq.Expressions;

#endregion

namespace Bsw.FayeDotNet.Client
{
    public class HandshakeException : Exception
    {
        public HandshakeException(string responseDetails)
            : base(string.Format("Handshaking with server failed. Reason: {0}",
                                 responseDetails))
        {
        }

        public HandshakeException(TimeSpan timeoutValue)
            : base(string.Format("Timed out at {0} milliseconds waiting for server to respond to handshake request.",
                                 timeoutValue.TotalMilliseconds))
        {
        }
    }
}