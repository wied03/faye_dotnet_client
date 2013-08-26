#region

using System;
using System.Linq;
using System.Linq.Expressions;
using MsBw.MsBwUtility.Enum;

#endregion

namespace Bsw.FayeDotNet.Client
{
    public enum Reconnect
    {
        [StringValue("retry")]
        Retry,
        [StringValue("handshake")]
        Handshake,
        [StringValue("none")]
        None
    }

    public class Advice
    {
        public Advice(Reconnect reconnect,
                      TimeSpan interval,
                      TimeSpan timeout)
        {
            Reconnect = reconnect;
            Interval = interval;
            Timeout = timeout;
        }

        public Reconnect Reconnect { get; private set; }
        public TimeSpan Interval { get; private set; }
        public TimeSpan Timeout { get; private set; }
    }
}