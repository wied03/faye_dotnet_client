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
        public string Version { get; private set; }
        public List<string> SupportedConnectionTypes { get; private set; }

        // for JSON deserializer
        public HandshakeRequestMessage() : base(HANDSHAKE_MESSAGE) { }

        public HandshakeRequestMessage(IEnumerable<string> supportedConnectionTypes,
                                       string version = BAYEUX_VERSION_1)
            : base(HANDSHAKE_MESSAGE)
        {
            Version = version;
            SupportedConnectionTypes = new List<string>(supportedConnectionTypes);
        }
    }
}