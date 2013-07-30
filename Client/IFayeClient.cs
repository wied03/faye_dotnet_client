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

        /// <summary>
        /// Opens a connection and handshakes with the server
        /// </summary>
        /// <returns>A connection that can be used for publishing/subscribing</returns>
        /// <exception cref="HandshakeException">Problem in the handshaking process</exception>
        Task<IFayeConnection> Connect();
    }
}