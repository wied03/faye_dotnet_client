// Copyright 2013 BSW Technology Consulting, released under the BSD license - see LICENSING.txt at the top of this repository for details
﻿#region

using System;
using System.Linq;
using System.Linq.Expressions;

#endregion

namespace Bsw.FayeDotNet.Messages
{
    public class PublishResponseMessage : DataMessage
    {
        public bool Successful { get; set; }
        public string Error { get; set; }

        // for JSON serializer
        public PublishResponseMessage()
        {
        }

        public PublishResponseMessage(string channel,
                                      string clientId,
                                      string data,
                                      bool successful,
                                      int id) : base(channel,
                                                     clientId,
                                                     data,
                                                     id)
        {
            Successful = successful;
        }
    }
}