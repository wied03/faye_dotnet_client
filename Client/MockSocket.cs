#region

using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Bsw.WebSocket4NetSslExt.Socket;
using SuperSocket.ClientEngine;
using WebSocket4Net;

#endregion

namespace Bsw.FayeDotNet.Test.Client
{
    public class MockSocket : IWebSocket
    {
        public Action<EventHandler> OpenedAction;
        public Action<string> MessageSentAction;
        public Func<string,string> MessageReceiveAction;

        public void Open()
        {
            OpenedAction(Opened);
        }

        public void Send(string message)
        {
            if (MessageSentAction != null)
            {
                MessageSentAction(message);
            }
            if (MessageReceiveAction != null)
            {
                Task.Factory.StartNew(() => MessageReceived(this,
                                                            new MessageReceivedEventArgs(MessageReceiveAction.Invoke(message))));
            }
        }

        public void Send(byte[] data,
                         int offset,
                         int length)
        {
            throw new NotImplementedException();
        }

        public void Send(IList<ArraySegment<byte>> segments)
        {
            throw new NotImplementedException();
        }

        public void Close()
        {
            throw new NotImplementedException();
        }

        public void Close(string reason)
        {
            throw new NotImplementedException();
        }

        public void Close(int statusCode,
                          string reason)
        {
            throw new NotImplementedException();
        }

        public WebSocketVersion Version { get; private set; }
        public DateTime LastActiveTime { get; private set; }
        public bool EnableAutoSendPing { get; set; }
        public int AutoSendPingInterval { get; set; }
        public bool SupportBinary { get; private set; }
        public WebSocketState State { get; private set; }
        public bool Handshaked { get; private set; }
        public IProxyConnector Proxy { get; set; }
        public int ReceiveBufferSize { get; set; }
        public bool AllowUnstrustedCertificate { get; set; }
        public event EventHandler Opened;
        public event EventHandler<MessageReceivedEventArgs> MessageReceived;
        public event EventHandler<DataReceivedEventArgs> DataReceived;
        public event EventHandler Closed;
        public event EventHandler<ErrorEventArgs> Error;
    }
}