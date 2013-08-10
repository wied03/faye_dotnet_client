#region

using System;
using System.Linq;
using System.Linq.Expressions;
using Newtonsoft.Json;

#endregion

namespace Bsw.FayeDotNet.Messages
{
    internal abstract class BaseFayeMessage
    {
        protected BaseFayeMessage(string theChannel)
        {
            Channel = theChannel;
        }

        public string Channel { get; private set; }
    }
}