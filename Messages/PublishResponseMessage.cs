#region

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
        public PublishResponseMessage() : base()
        {
        }

        public PublishResponseMessage(string channel,
                                      string clientId,
                                      string data,
                                      bool successful) : base(channel,
                                                              clientId,
                                                              data)
        {
            Successful = successful;
        }
    }
}