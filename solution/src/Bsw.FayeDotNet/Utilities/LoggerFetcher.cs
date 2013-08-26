// Copyright 2013 BSW Technology Consulting, released under the BSD license - see LICENSING.txt at the top of this repository for details
﻿#region

using System;
using System.Linq;
using System.Linq.Expressions;
using NLog;

#endregion

namespace Bsw.FayeDotNet.Utilities
{
    public static class LoggerFetcher
    {
        private const string LOGGER_NAME_FORMAT = "{0} Logger for connection '{1}': ";

        public static Logger GetLogger(string connectionId,
                                       object forObject)
        {
            return LogManager.GetLogger(string.Format(LOGGER_NAME_FORMAT,
                                                      forObject.GetType(),
                                                      connectionId));
        }
    }
}