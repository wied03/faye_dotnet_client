#region

using System;
using System.Threading.Tasks;

#endregion

namespace Bsw.FayeDotNet.Client
{
    public interface IFayeClient
    {
        Task Connect();
        Task Disconnect();

        Task Subscribe(string channel,
                       Action<object> messageReceived);

        Task Unsubscribe(string channel);

        Task Publish(string channel,
                     object message);
    }
}