#region

using System;
using System.Linq;
using System.Linq.Expressions;

#endregion

namespace Bsw.FayeDotNet.Messages
{
    public class DataMessageRequest : BaseFayeMessage
    {
        public string ClientId { get; set; }
        public object Data { get; set; }

        public DataMessageRequest(string channel,
                                  string clientId,
                                  object data) : base(channel)
        {
            ClientId = clientId;
            Data = data;
        }
    }
}