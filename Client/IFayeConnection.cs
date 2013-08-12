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
                       Action<string> messageReceived);

        Task Unsubscribe(string channel);

        Task Publish(string channel,
                     string message);
    }
}