#region

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

    public delegate void ConnectionLost(object sender,
                                        EventArgs args);

    public delegate void MessageReceived(object sender,
                                         MessageReceivedArgs args);

    public interface ITransportConnection
    {
        /// <summary>
        ///     Sends a message (will gracefully handle a disconnect that has happened while trying to send)
        /// </summary>
        /// <param name="message">Message to send</param>
        void Send(string message);

        Task Disconnect();

        event MessageReceived MessageReceived;

        /// <summary>
        ///     If the transport drops the connection, it will reconnect, but if you need to issue other commands when this
        ///     happens,
        ///     then hook into this event
        /// </summary>
        event ConnectionLost ConnectionLost;

        TimeSpan RetryTimeout { get; set; }

        bool Closed { get; }
    }
}