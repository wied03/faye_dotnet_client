using System;
using System.Threading.Tasks;

namespace Bsw.FayeDotNet.Client
{
    public interface IFayeConnection
    {
        /// <summary>
        /// The client ID assigned by the server
        /// </summary>
        string ClientId { get; }

        Task Disconnect();

        Task Subscribe(string channel,
                       Action<object> messageReceived);

        Task Unsubscribe(string channel);

        void Publish(string channel,
                     object message);
    }
}