#region

using System;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Bsw.WebSocket4NetSslExt.Socket;

#endregion

namespace Bsw.FayeDotNet.Client
{
    /// <summary>
    /// Implementation with 10 second default timeout
    /// </summary>
    public class FayeClient : IFayeClient
    {
        public FayeClient(IWebSocket socket)
        {
            HandshakeTimeout = new TimeSpan(0,
                                            0,
                                            10);
        }

        public TimeSpan HandshakeTimeout { get; set; }

        public async Task<IFayeConnection> Connect()
        {
            throw new NotImplementedException();
        }
    }
}