// Copyright 2013 BSW Technology Consulting, released under the BSD license - see LICENSING.txt at the top of this repository for details
﻿#region

using System;
using System.Linq;
using System.Linq.Expressions;

#endregion

namespace Bsw.FayeDotNet.Messages
{
    public class ConnectRequestMessage : BaseFayeMessage
    {
        public string ClientId { get; set; }
        public string ConnectionType { get; set; }

        // for JSON serializer
        public ConnectRequestMessage()
        {
        }

        public ConnectRequestMessage(string clientId,
                                     string connectionType,
                                     int id) : base(channel: MetaChannels.Connect,
                                                    id: id)
        {
            ClientId = clientId;
            ConnectionType = connectionType;
        }
    }
}