using System.Threading.Tasks;
using Bsw.FayeDotNet.Messages;

namespace Bsw.FayeDotNet.Transports
{
    public interface ITransportClient
    {
        /// <summary>
        /// Connects to the underlying transport
        /// </summary>
        /// <returns>A connection that will reconnect on its own when a connection is lost</returns>
        Task<ITransportConnection> Connect();
    }
}