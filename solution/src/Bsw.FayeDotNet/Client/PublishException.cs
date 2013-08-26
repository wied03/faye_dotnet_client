#region

using System;
using System.Linq;
using System.Linq.Expressions;

#endregion

namespace Bsw.FayeDotNet.Client
{
    public class PublishException : Exception
    {
        public PublishException(string error) : base(error)
        {
        }
    }
}