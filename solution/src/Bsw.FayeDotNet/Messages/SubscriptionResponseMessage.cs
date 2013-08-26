// Copyright 2013 BSW Technology Consulting, released under the BSD license - see LICENSING.txt at the top of this repository for details
﻿#region

using System;
using System.Linq;
using System.Linq.Expressions;

#endregion

namespace Bsw.FayeDotNet.Messages
{
    internal class SubscriptionResponseMessage : SubscriptionRequestMessage
    {
        public bool Successful { get; set; }
        public string Error { get; set; }

        public SubscriptionResponseMessage(string clientId,
                                           string subscriptionChannel,
                                           int id) : base(clientId,
                                                          subscriptionChannel,
                                                          id)
        {
        }
    }
}