#region

using System;
using System.Linq;
using System.Linq.Expressions;

#endregion

namespace Bsw.FayeDotNet.Client
{
    public class SubscriptionException : Exception
    {
        public SubscriptionException(string serverError) : base(serverError)
        {
        }
    }
}