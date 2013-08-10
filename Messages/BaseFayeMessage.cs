#region

using System;
using System.Linq;
using System.Linq.Expressions;
using MsBw.MsBwUtility.Enum;
using Newtonsoft.Json;

#endregion

namespace Bsw.FayeDotNet.Messages
{
    public abstract class BaseFayeMessage
    {
        protected BaseFayeMessage(MetaChannels channel)
        {
            Channel = channel.StringValue();
        }

        public string Channel { get; set; }
    }
}