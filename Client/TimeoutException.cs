#region

using System;
using System.Linq;
using System.Linq.Expressions;

#endregion

namespace Bsw.FayeDotNet.Client
{
    public class TimeoutException : Exception
    {
        private const string ERROR_MESSAGE = "Timed out at '{0}' milliseconds waiting for response to the following message: {1}";

        public TimeoutException(TimeSpan timeoutValue,
                                string waitingFor)
            : base(string.Format(ERROR_MESSAGE,
                                 timeoutValue.TotalMilliseconds,
                                 waitingFor))
        {
        }
    }
}