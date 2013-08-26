// Copyright 2013 BSW Technology Consulting, released under the BSD license - see LICENSING.txt at the top of this repository for details
﻿#region

using System;
using System.Threading.Tasks;

#endregion

namespace Bsw.FayeDotNet.Transports
{
    public interface ITransportClient
    {
        /// <summary>
        ///     Connects to the underlying transport
        /// </summary>
        /// <returns>A connection that will reconnect on its own when a connection is lost</returns>
        Task<ITransportConnection> Connect();

        TimeSpan ConnectionOpenTimeout { get; set; }
    }
}