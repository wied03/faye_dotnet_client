// Copyright 2013 BSW Technology Consulting, released under the BSD license - see LICENSING.txt at the top of this repository for details
 #region

using System;
using System.Collections.Generic;
using SuperSocket.ClientEngine;
using WebSocket4Net;

#endregion

namespace Bsw.WebSocket4Net.Wrapper.Socket
{
    public interface IWebSocket
    {
        void Open();
        void Send(string message);

        void Send(byte[] data,
                  int offset,
                  int length);

        void Send(IList<ArraySegment<byte>> segments);
        void Close();
        void Close(string reason);

        void Close(int statusCode,
                   string reason);

        WebSocketVersion Version { get; }
        DateTime LastActiveTime { get; }
        bool EnableAutoSendPing { get; set; }
        int AutoSendPingInterval { get; set; }
        bool SupportBinary { get; }
        WebSocketState State { get; }
        bool Handshaked { get; }
        IProxyConnector Proxy { get; set; }
        int ReceiveBufferSize { get; set; }
        bool AllowUnstrustedCertificate { get; set; }
        event EventHandler Opened;
        event EventHandler<MessageReceivedEventArgs> MessageReceived;
        event EventHandler<DataReceivedEventArgs> DataReceived;
        event EventHandler Closed;
        event EventHandler<ErrorEventArgs> Error;
    }
}