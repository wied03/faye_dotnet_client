#region

using System;
using System.Linq;
using System.Linq.Expressions;

#endregion

namespace Bsw.FayeDotNet.Messages
{
    public class DisconnectRequestMessage : BaseFayeMessage
    {
        public string ClientId { get; set; }

        // for JSON serializer
        protected DisconnectRequestMessage() : base(MetaChannels.Disconnect) {}

        public DisconnectRequestMessage(string clientId) : this()
        {
            ClientId = clientId;
        }
    }
}