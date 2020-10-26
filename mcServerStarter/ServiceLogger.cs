// <copyright file="ServiceLogger.cs" company="kf corp">
// Licensed under the Apache 2.0 license
// </copyright>

namespace McServerStarter
{
    using System;
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Logging.Console;
    using Microsoft.Extensions.Options;
    using NLog.Web;

    internal class ServiceLogger : IServiceLogger
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ServiceLogger"/> class.
        /// </summary>
        /// <param name="settings">settings.</param>
        public ServiceLogger(IOptions<ConsoleLoggerOptions> settings)
        {
            LogFactory = LoggerFactory.Create((loggerBuilder) =>
            {
                loggerBuilder.AddFilter(level => level >= LogLevel.Trace)
                             .AddConsole(options =>
                             {
                                 options.DisableColors = settings.Value.DisableColors;
                                 options.Format = settings.Value.Format;
                                 options.IncludeScopes = settings.Value.IncludeScopes;
                                 options.LogToStandardErrorThreshold = settings.Value.LogToStandardErrorThreshold;
                                 options.TimestampFormat = settings.Value.TimestampFormat ?? options.TimestampFormat;
                             });
            });
        }

        public ILoggerFactory LogFactory { get; private set; }
    }
}