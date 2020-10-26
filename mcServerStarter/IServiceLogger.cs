// <copyright file="IServiceLogger.cs" company="kf corp">
// Licensed under the Apache 2.0 license
// </copyright>

namespace McServerStarter
{
    using Microsoft.Extensions.Logging;

    /// <summary>
    /// The Logger.
    /// </summary>
    internal interface IServiceLogger
    {
        /// <summary>
        /// Gets actual factory.
        /// </summary>
        ILoggerFactory LogFactory { get; }
    }
}