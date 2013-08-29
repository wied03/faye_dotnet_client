#region

// Copyright 2013 BSW Technology Consulting, released under the BSD license - see LICENSING.txt at the top of this repository for details
using System;
using System.Threading.Tasks;
using Bsw.FayeDotNet.Messages;

#endregion

namespace Bsw.FayeDotNet.Client
{
    public delegate void ConnectionEvent(object sender,
                                         EventArgs args);

    public delegate SubscriptionRequestMessage CustomizeSubscriptionRequest(SubscriptionRequestMessage defaultRequest,
                                                                            HandshakeResponseMessage
                                                                                initialHandshakeResponse);


    public interface IFayeConnection
    {
        /// <summary>
        ///     The client ID assigned by the server
        /// </summary>
        string ClientId { get; }

        Task Disconnect();

        Task Subscribe(string channel,
                       Action<string> messageReceivedAction);

        Task Unsubscribe(string channel);

        Task Publish(string channel,
                     string message);

        event ConnectionEvent ConnectionLost;

        event ConnectionEvent ConnectionReestablished;

        /// <summary>
        ///     If you wish to customize subscription requests to the server, attach to this event handler as it will be invoked
        ///     before sending the message to the server
        /// </summary>
        event CustomizeSubscriptionRequest PresubscriptionHandler;
    }
}