#region

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
                                           string subscriptionChannel) : base(clientId,
                                                                              subscriptionChannel)
        {
        }
    }
}