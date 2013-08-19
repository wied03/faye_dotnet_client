#region

using System;
using System.Threading.Tasks;

#endregion

namespace Bsw.FayeDotNet.Transports
{
    public interface ITransportClient
    {
        /// <summary>
        ///     Connects to the underlying transport
        /// </summary>
        /// <returns>A connection that will reconnect on its own when a connection is lost</returns>
        Task<ITransportConnection> Connect();

        TimeSpan ConnectionOpenTimeout { get; set; }
    }
}