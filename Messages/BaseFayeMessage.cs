#region

using System;
using System.Linq;
using System.Linq.Expressions;

#endregion

namespace Bsw.FayeDotNet.Messages
{
    internal abstract class BaseFayeMessage
    {
        protected BaseFayeMessage(string theChannel)
        {
            channel = theChannel;
        }

        public string channel { get; private set; }
    }
}