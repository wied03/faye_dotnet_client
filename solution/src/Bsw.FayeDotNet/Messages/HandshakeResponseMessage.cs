// Copyright 2013 BSW Technology Consulting, released under the BSD license - see LICENSING.txt at the top of this repository for details
﻿#region

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
        public string Error { get; set; }
        public string ClientId { get; set; }

        // for JSON deserializer
        public HandshakeResponseMessage()
        {
        }

        public HandshakeResponseMessage(IEnumerable<string> supportedConnectionTypes,
                                        bool successful,
                                        string clientId,
                                        int id,
                                        string version = BAYEUX_VERSION_1)
            : base(supportedConnectionTypes,
                   id,
                   version)
        {
            Successful = successful;
            ClientId = clientId;
        }
    }
}