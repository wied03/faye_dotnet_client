#region

using System;
using System.Linq;
using System.Linq.Expressions;

#endregion

namespace Bsw.FayeDotNet.Messages
{
    public class UnsubscribeRequestMessage : BaseFayeMessage
    {
        public string ClientId { get; set; }
        public string Subscription { get; set; }

        public UnsubscribeRequestMessage(string clientId,
                                         string subscriptionChannel) : base(MetaChannels.Unsubscribe)
        {
            ClientId = clientId;
            Subscription = subscriptionChannel;
        }
    }
}