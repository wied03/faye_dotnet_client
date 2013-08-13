using System;
using MsBw.MsBwUtility.Enum;

namespace Bsw.FayeDotNet.Messages
{
    public enum MetaChannels
    {
        [StringValue("/meta/subscribe")]
        Subscribe,
        [StringValue("/meta/unsubscribe")]
        Unsubscribe,
        [StringValue("/meta/handshake")]
        Handshake,
        [StringValue("/meta/disconnect")]
        Disconnect
    }
}