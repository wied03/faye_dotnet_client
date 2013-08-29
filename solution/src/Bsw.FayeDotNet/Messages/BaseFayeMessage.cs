// Copyright 2013 BSW Technology Consulting, released under the BSD license - see LICENSING.txt at the top of this repository for details
﻿#region

using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using MsBw.MsBwUtility.Enum;

#endregion

namespace Bsw.FayeDotNet.Messages
{
    public class BaseFayeMessage
    {
        protected BaseFayeMessage(MetaChannels channel,
                                  int id)
            : this(channel: channel.StringValue(),
                   id: id)
        {
        }

        protected BaseFayeMessage(string channel,
                                  int id)
        {
            Channel = channel;
            Id = id;
            Ext = new Dictionary<object, object>();
        }

        // for JSON serializer
        public BaseFayeMessage()
        {
        }

        public string Channel { get; set; }
        public int Id { get; set; }
        public IDictionary<object, object> Ext { get; set; } 
    }
}