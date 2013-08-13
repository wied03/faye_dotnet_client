#region

using System;
using System.Linq;
using System.Linq.Expressions;
using MsBw.MsBwUtility.Enum;

#endregion

namespace Bsw.FayeDotNet.Messages
{
    public class BaseFayeMessage
    {
        protected BaseFayeMessage(MetaChannels channel,
                                  int id)
            : this(channel: channel.StringValue(),
                   id: id)
        {
        }

        protected BaseFayeMessage(string channel,
                                  int id)
        {
            Channel = channel;
            Id = id;
        }

        // for JSON serializer
        public BaseFayeMessage()
        {
        }

        public string Channel { get; set; }
        public int Id { get; set; }
    }
}