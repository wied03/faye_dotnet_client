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
        internal const string BAYEUX_VERSION_1 = "1.0";
        public string Version { get; set; }
        public List<string> SupportedConnectionTypes { get; set; }

        // for JSON deserializer
        public HandshakeRequestMessage() : base(MetaChannels.Handshake)
        {
        }

        public HandshakeRequestMessage(IEnumerable<string> supportedConnectionTypes,
                                       string version = BAYEUX_VERSION_1)
            : this()
        {
            Version = version;
            SupportedConnectionTypes = new List<string>(supportedConnectionTypes);
        }
    }
}