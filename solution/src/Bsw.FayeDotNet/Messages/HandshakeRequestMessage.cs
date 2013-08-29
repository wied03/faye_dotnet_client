// Copyright 2013 BSW Technology Consulting, released under the BSD license - see LICENSING.txt at the top of this repository for details
﻿#region

using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

#endregion

namespace Bsw.FayeDotNet.Messages
{
    public class HandshakeRequestMessage : BaseFayeMessage
    {
        internal const string BAYEUX_VERSION_1 = "1.0";
        public string Version { get; set; }
        public List<string> SupportedConnectionTypes { get; set; }

        // for JSON deserializer
        public HandshakeRequestMessage()
        {
        }

        public HandshakeRequestMessage(IEnumerable<string> supportedConnectionTypes,
                                       int id,
                                       string version = BAYEUX_VERSION_1)
            : base(channel: MetaChannels.Handshake,
                   id: id)
        {
            Version = version;
            SupportedConnectionTypes = new List<string>(supportedConnectionTypes);
        }
    }
}