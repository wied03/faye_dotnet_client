#region

using System;
using System.Linq;
using System.Linq.Expressions;

#endregion

namespace Bsw.FayeDotNet.Messages
{
    public class ConnectResponseMessage : ConnectRequestMessage
    {
        public bool Successful { get; set; }
        public string Error { get; set; }

        // for JSON serializer
        public ConnectResponseMessage() : base()
        {
        }

        public ConnectResponseMessage(string clientId,
                                      string connectionType) : base(clientId,
                                                                    connectionType)
        {
        }
    }
}