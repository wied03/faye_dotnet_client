#region

using System;
using System.Linq;
using System.Linq.Expressions;
using MsBw.MsBwUtility.Enum;

#endregion

namespace Bsw.FayeDotNet.Messages
{
    public abstract class BaseFayeMessage
    {
        protected BaseFayeMessage(MetaChannels channel)
            : this(channel.StringValue())
        {
        }

        protected BaseFayeMessage(string channel)
        {
            Channel = channel;
        }

        // for JSON serializer
        protected BaseFayeMessage() {}

        public string Channel { get; set; }
    }
}