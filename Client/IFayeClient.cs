#region

using System;
using System.Threading.Tasks;

#endregion

namespace Bsw.FayeDotNet.Client
{
    public interface IFayeClient
    {
        /// <summary>
        /// After this span has passed, an exception will be thrown when trying to connect/handshake with the server
        /// </summary>
        TimeSpan HandshakeTimeout { get; set; }
        Task Connect();
        Task Disconnect();

        Task Subscribe(string channel,
                       Action<object> messageReceived);

        Task Unsubscribe(string channel);

        Task Publish(string channel,
                     object message);
    }
}