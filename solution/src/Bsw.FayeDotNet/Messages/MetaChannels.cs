// Copyright 2013 BSW Technology Consulting, released under the BSD license - see LICENSING.txt at the top of this repository for details
﻿using System;
using MsBw.MsBwUtility.Enum;

namespace Bsw.FayeDotNet.Messages
{
    public enum MetaChannels
    {
        [StringValue("/meta/connect")]
        Connect,
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