#region

using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

#endregion

namespace Bsw.FayeDotNet.Messages
{
    internal class HandshakeRequestMessage : BaseFayeMessage
    {
        internal const string HANDSHAKE_MESSAGE = "/meta/handshake";
        internal const string BAYEUX_VERSION_1 = "1.0";
        public string version { get; private set; }
        public List<string> supportedConnectionTypes { get; private set; }

        public HandshakeRequestMessage(IEnumerable<string> theSupportedConnectionTypes,
                                       string theVersion = BAYEUX_VERSION_1)
            : base(HANDSHAKE_MESSAGE)
        {
            version = theVersion;
            supportedConnectionTypes = new List<string>(theSupportedConnectionTypes);
        }
    }
}