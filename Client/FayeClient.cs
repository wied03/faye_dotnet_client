#region

using System;
using System.Linq;
using System.Linq.Expressions;
using Bsw.WebSocket4NetSslExt.Socket;

#endregion

namespace Bsw.FayeDotNet.Client
{
    public class FayeClient : IFayeClient
    {
        public FayeClient(IWebSocket socket)
        {
            
        }

        public void Disconnect()
        {
            
        }

        public void Subscribe(string channel,
                              Action<object> messageReceived)
        {
            
        }

        public void Unsubscribe(string channel)
        {
            
        }

        public void Publish(string channel,
                            object message)
        {
            
        }
    }
}