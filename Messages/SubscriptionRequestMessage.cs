#region

using System;
using System.Linq;
using System.Linq.Expressions;

#endregion

namespace Bsw.FayeDotNet.Messages
{
    internal class SubscriptionRequestMessage : BaseFayeMessage
    {
        public string ClientId { get; set; }
        public string Subscription { get; set; }

        public SubscriptionRequestMessage(string clientId,
                                          string subscriptionChannel,
                                          int id)
            : base(channel: MetaChannels.Subscribe,
                   id: id)
        {
            ClientId = clientId;
            Subscription = subscriptionChannel;
        }
    }
}