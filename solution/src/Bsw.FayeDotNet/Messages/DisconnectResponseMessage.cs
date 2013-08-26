#region

using System;
using System.Linq;
using System.Linq.Expressions;

#endregion

namespace Bsw.FayeDotNet.Messages
{
    public class DisconnectResponseMessage : DisconnectRequestMessage
    {
        public bool Successful { get; set; }

         // for JSON deserializer
        public DisconnectResponseMessage() : base() { }

        public DisconnectResponseMessage(bool successful) : base()
        {
            Successful = successful;
        }
    }
}