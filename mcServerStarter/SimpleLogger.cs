// <copyright file="SimpleLogger.cs" company="kf corp">
// Licensed under the Apache 2.0 license
// </copyright>

namespace McServerStarter
{
    using System;
    using Microsoft.Extensions.Logging;
    using NLog.Web;

    internal class SimpleLogger<T> : ILog<T>
    {
        private ILogger _logger;
        private NLog.Logger _nlog;

        public SimpleLogger(IServiceLogger serviceLogger)
        {
            _logger = serviceLogger.LogFactory.CreateLogger($"{typeof(T)}");
            _nlog = NLogBuilder.ConfigureNLog("nlog.config").GetLogger($"{typeof(T)}");
        }

        public void LogInformation(string message, params object[] args)
        {
            _logger.LogInformation(message, args);
            _nlog.Log(NLog.LogLevel.Info, message, args);
            _nlog.Log(NLog.LogLevel.Info, message, args);
        }

        public void LogDebug(string message, params object[] args)
        {
            _logger.LogDebug(message, args);
            _nlog.Log(NLog.LogLevel.Debug, message, args);
        }

        public void LogWarning(string message, params object[] args)
        {
            _logger.LogWarning(message, args);
            _nlog.Log(NLog.LogLevel.Warn, message, args);
        }

        public void LogError(string message, params object[] args)
        {
            _logger.LogError(message, args);
            _nlog.Log(NLog.LogLevel.Error, message, args);
        }
    }
}