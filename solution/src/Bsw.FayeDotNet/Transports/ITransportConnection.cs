// Copyright 2013 BSW Technology Consulting, released under the BSD license - see LICENSING.txt at the top of this repository for details
﻿#region

using System;
using System.Threading.Tasks;

#endregion

namespace Bsw.FayeDotNet.Transports
{
    public class MessageReceivedArgs : EventArgs
    {
        public MessageReceivedArgs(string message)
        {
            Message = message;
        }

        public string Message { get; private set; }
    }

    public delegate void ConnectionEvent(object sender,
                                        EventArgs args);

    public delegate void MessageReceived(object sender,
                                         MessageReceivedArgs args);

    public enum ConnectionState
    {
        Connected,
        Disconnected,
        Lost,
        Reconnecting
    }

    public interface ITransportConnection
    {
        /// <summary>
        ///     Sends a message (will gracefully handle a disconnect that has happened while trying to send)
        /// </summary>
        /// <param name="message">Message to send</param>
        void Send(string message);

        Task Disconnect();

        event MessageReceived MessageReceived;
        event ConnectionEvent ConnectionClosed;
        event ConnectionEvent ConnectionLost;
        /// <summary>
        ///     If the transport drops the connection, it will reconnect, but if you need to issue other commands when this
        ///     happens,
        ///     then hook into this event
        /// </summary>
        event ConnectionEvent ConnectionReestablished;
        
        TimeSpan RetryTimeout { get; set; }

        /// <summary>
        /// Enabled by default
        /// </summary>
        bool RetryEnabled { get; set; }

        ConnectionState ConnectionState { get; }

        /// <summary>
        /// Allows clients to notify the connection that the other side may disconnect in an expected manner (so this
        /// transport connection should not try and reconnect)
        /// </summary>
        void NotifyOfPendingServerDisconnection();
    }
}