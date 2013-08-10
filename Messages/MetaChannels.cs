using MsBw.MsBwUtility.Enum;

namespace Bsw.FayeDotNet.Messages
{
    public enum MetaChannels
    {
        [StringValue("/meta/subscribe")]
        Subscribe,
        [StringValue("/meta/handshake")]
        Handshake
    }
}