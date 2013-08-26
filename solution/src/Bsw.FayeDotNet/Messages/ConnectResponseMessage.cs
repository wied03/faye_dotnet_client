// Copyright 2013 BSW Technology Consulting, released under the BSD license - see LICENSING.txt at the top of this repository for details
﻿#region

using System;
using System.Linq;
using System.Linq.Expressions;

#endregion

namespace Bsw.FayeDotNet.Messages
{
    public class ConnectResponseMessage : ConnectRequestMessage
    {
        public bool Successful { get; set; }
        public string Error { get; set; }

        // for JSON serializer
        public ConnectResponseMessage()
        {
        }

        public ConnectResponseMessage(string clientId,
                                      string connectionType,
                                      int id) : base(clientId,
                                                     connectionType,
                                                     id)
        {
        }
    }
}