#region

using System;
using System.Linq;
using System.Linq.Expressions;

#endregion

namespace Bsw.FayeDotNet.Messages
{
    public class UnsubscribeResponseMessage : UnsubscribeRequestMessage
    {
        public bool Successful { get; set; }
        public string Error { get; set; }

        public UnsubscribeResponseMessage(string clientId,
                                          string subscriptionChannel) : base(clientId,
                                                                             subscriptionChannel)
        {
        }
    }
}