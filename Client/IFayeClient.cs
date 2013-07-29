using System;

namespace Bsw.FayeDotNet.Client
{
    public interface IFayeClient
    {
        void Disconnect();

        void Subscribe(string channel,
                                       Action<object> messageReceived);

        void Unsubscribe(string channel);

        void Publish(string channel,
                                     object message);
    }
}