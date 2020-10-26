// <copyright file="IMonitorService.cs" company="kf corp">
// Licensed under the Apache 2.0 license
// </copyright>

using System;
using System.Threading;
using System.Threading.Tasks;

namespace McServerStarter
{
    internal interface IMonitorService : IAsyncDisposable
    {
        Task RunAsync(CancellationToken shutdownToken, bool interativeMode);
    }
}