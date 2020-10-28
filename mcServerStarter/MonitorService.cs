// <copyright file="MonitorService.cs" company="kf corp">
// Licensed under the Apache 2.0 license
// </copyright>

namespace McServerStarter
{
    using System;
    using System.Diagnostics;
    using System.IO;
    using System.Runtime.InteropServices;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Extensions.Options;

    internal class MonitorService : IMonitorService
    {
        private readonly MonitorServiceOptions _options;
        private readonly ILog<MonitorService> _logger;
        private Process _process;
        private TaskCompletionSource<EventArgs> _tcs;

        public MonitorService(IOptions<MonitorServiceOptions> options, ILog<MonitorService> logger)
        {
            _options = options.Value;
            _logger = logger;
            _logger.LogDebug($"{nameof(MonitorService)} created");
        }

        ~MonitorService()
        {
            _logger.LogDebug($"{nameof(MonitorService)} destroyed");
        }

        public async Task RunAsync(CancellationToken shutdownToken, bool interativeMode)
        {
            if (_tcs != null)
            {
                await _tcs.Task;
                return;
            }

            _logger.LogInformation("Starting ...");
            try
            {
                using (var process = new Process())
                {
                    process.EnableRaisingEvents = true;
                    process.ErrorDataReceived += (object sender, DataReceivedEventArgs e) =>
                    {
                        _logger.LogError($"> {e.Data}");
                    };

                    process.OutputDataReceived += (object sender, DataReceivedEventArgs e) =>
                    {
                        _logger.LogInformation($"> {e.Data}");
                    };

                    process.Exited += (object sender, EventArgs e) =>
                    {
                        _logger.LogDebug("worker process exit");
                        _tcs.TrySetResult(e);
                    };

                    if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                    {
                        process.StartInfo = new ProcessStartInfo
                        {
                            FileName = _options.ProcessPath,
                            Arguments = _options.Args,
                            RedirectStandardError = interativeMode,
                            RedirectStandardOutput = interativeMode,
                            RedirectStandardInput = true,
                            UseShellExecute = false,
                        };
                    }
                    else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                    {
                        process.StartInfo = new ProcessStartInfo
                        {
                            FileName = _options.ProcessPath,
                            Arguments = _options.Args,
                            RedirectStandardError = interativeMode,
                            RedirectStandardOutput = interativeMode,
                            RedirectStandardInput = true,
                            UseShellExecute = false,
                        };
                    }
                    else
                    {
                        throw new NotImplementedException($"Not implmented for this OS. {RuntimeInformation.OSDescription}");
                    }

                    Directory.SetCurrentDirectory(_options.RootPath);

                    if (!process.Start())
                    {
                        return;
                    }

                    _process = process;
                    _tcs = new TaskCompletionSource<EventArgs>();
                    if (interativeMode)
                    {
                        _process.BeginOutputReadLine();
                        _process.BeginErrorReadLine();
                        ReadInputAsync(shutdownToken);
                    }

                    using (var shutdownCtr = shutdownToken.Register(ShutDown))
                    {
                        await _tcs.Task;
                    }
                }
            }
            finally
            {
                _tcs = null;
                _process = null;
            }
        }

        public async ValueTask DisposeAsync()
        {
            _logger.LogDebug("Disposing ...");
            if (_tcs != null)
            {
                _tcs.TrySetResult(EventArgs.Empty);
                await _tcs.Task;
            }
        }

        private async void ReadInputAsync(CancellationToken cancellationToken)
        {
            var buffer = new char[1024];
            while (!cancellationToken.IsCancellationRequested)
            {
                var len = await Console.In.ReadAsync(buffer, 0, 1024);
                if (cancellationToken.IsCancellationRequested)
                {
                    return;
                }

                if (len <= 0)
                {
                    continue;
                }

                await _process.StandardInput.WriteAsync(buffer, 0, len);
            }
        }

        private async void ShutDown()
        {
            _logger.LogInformation("Shuting down...");
            try
            {
                if (_tcs.Task.Status == TaskStatus.RanToCompletion
                || _tcs.Task.Status == TaskStatus.Faulted
                || _tcs.Task.Status == TaskStatus.Canceled)
                {
                    _logger.LogWarning($"process is already not running. Task status: {_tcs.Task.Status}");
                    return;
                }

                _process.StandardInput.WriteLine("/stop");

                _logger.LogInformation($"Stop command sent. Waiting for worker response");
                await Task.WhenAny(Task.Delay(_options.ShutDownTimeout), _tcs.Task);
                if (_process != null)
                {
                    _logger.LogWarning("Shutdown timeout reached. Killing worker process.");
                    _process.Kill();
                    _process.WaitForExit();
                    _tcs.TrySetResult(EventArgs.Empty);
                }

                _logger.LogInformation($"worker process stopped");
            }
            catch (Exception e)
            {
                _logger.LogError(e.ToString());
                _tcs?.TrySetException(e);
            }
        }
    }
}