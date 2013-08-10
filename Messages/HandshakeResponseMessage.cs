#region

using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

#endregion

namespace Bsw.FayeDotNet.Messages
{
    internal class HandshakeResponseMessage : HandshakeRequestMessage
    {
        public bool Successful { get; set; }
        public string ClientId { get; set; }

        // for JSON deserializer
        public HandshakeResponseMessage() : base() { }

        public HandshakeResponseMessage(IEnumerable<string> supportedConnectionTypes,
                                        bool successful,
                                        string clientId,
                                        string version = BAYEUX_VERSION_1)
            : base(supportedConnectionTypes,
                   version)
        {
            Successful = successful;
            ClientId = clientId;
        }
    }
}