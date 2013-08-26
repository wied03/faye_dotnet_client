// Copyright 2013 BSW Technology Consulting, released under the BSD license - see LICENSING.txt at the top of this repository for details
﻿#region

using System;
using System.Linq;
using System.Linq.Expressions;
using Newtonsoft.Json.Linq;

#endregion

namespace Bsw.FayeDotNet.Messages
{
    public class DataMessage : BaseFayeMessage
    {
        public string ClientId { get; set; }
        public JRaw Data { get; set; }

        // for JSON serializer
        public DataMessage()
        {
        }

        public DataMessage(string channel,
                           string clientId,
                           string data,
                           int id)
            : base(channel: channel,
                   id: id)
        {
            ClientId = clientId;
            Data = new JRaw(data);
        }
    }
}