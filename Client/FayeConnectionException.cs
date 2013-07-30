#region

using System;
using System.Linq;
using System.Linq.Expressions;

#endregion

namespace Bsw.FayeDotNet.Client
{
    public class FayeConnectionException : Exception
    {
        public FayeConnectionException(string message) : base(message)
        {
        }
    }
}