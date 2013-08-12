#region

using System;
using System.Linq;
using System.Linq.Expressions;

#endregion

namespace Bsw.FayeDotNet.Messages
{
    public class DataMessage : BaseFayeMessage
    {
        public string ClientId { get; set; }
        public object Data { get; set; }

        // for JSON serializer
        public DataMessage() : base()
        {
        }

        public DataMessage(string channel,
                           string clientId,
                           object data) : base(channel)
        {
            ClientId = clientId;
            Data = data;
        }
    }
}