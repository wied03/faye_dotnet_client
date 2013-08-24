#region

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