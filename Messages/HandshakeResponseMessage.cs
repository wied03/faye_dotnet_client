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
        public MetaMessageResult Result { get; private set; }
        public string ClientId { get; private set; }

        public HandshakeResponseMessage(IEnumerable<string> theSupportedConnectionTypes,
                                        MetaMessageResult result,
                                        string clientId,
                                        string theVersion = BAYEUX_VERSION_1)
            : base(theSupportedConnectionTypes,
                   theVersion)
        {
            Result = result;
            ClientId = clientId;
        }
    }
}