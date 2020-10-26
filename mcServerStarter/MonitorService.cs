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

        public Task RunAsync(CancellationToken shutdownToken, bool interativeMode)
        {
            _logger.LogInformation("Starting ...");
            _tcs = new TaskCompletionSource<EventArgs>();
            _process = new Process
            {
                EnableRaisingEvents = true,
            };
            _process.ErrorDataReceived += (object sender, DataReceivedEventArgs e) =>
            {
                _logger.LogError($"> {e.Data}");
            };

            _process.OutputDataReceived += (object sender, DataReceivedEventArgs e) =>
            {
                _logger.LogInformation($"> {e.Data}");
            };

            _process.Exited += (object sender, EventArgs e) =>
            {
                _logger.LogDebug("process exit");
                _tcs.TrySetResult(e);
            };

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                _process.StartInfo = new ProcessStartInfo
                {
                    //FileName = "powershell",
                    //Arguments = "\"& \"npx serve\"",
                    FileName = _options.ProcessPath,
                    Arguments = _options.Args,
                    //RedirectStandardError = true,
                    //RedirectStandardOutput = true,
                    RedirectStandardInput = true,
                    UseShellExecute = false,
                };
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                _process.StartInfo = new ProcessStartInfo
                {
                    //FileName = "npx",
                    //Arguments = "serve",
                    FileName = _options.ProcessPath,
                    Arguments = _options.Args,
                    RedirectStandardError = true,
                    RedirectStandardOutput = true,
                    RedirectStandardInput = true,
                    UseShellExecute = false,
                };
            }
            else
            {
                throw new NotImplementedException($"Not implmented for this OS. {RuntimeInformation.OSDescription}");
            }

            Directory.SetCurrentDirectory(_options.RootPath);

            _process.Start();

            //_process.BeginOutputReadLine();
            //_process.BeginErrorReadLine();

            shutdownToken.Register(ShutDown);
            if (interativeMode)
            {
                ReadInputAsync(shutdownToken);
            }

            return _tcs.Task;
        }

        public ValueTask DisposeAsync()
        {
            _logger.LogDebug("Disposing ...");
            return new ValueTask(Task.CompletedTask);
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

                using (var writer = _process.StandardInput)
                {
                    writer.WriteLine("/stop");
                }

                _logger.LogInformation($"Stop command sent. Waiting for response");
                await _tcs.Task;
                _logger.LogInformation($"process stopped");
                _process = null;
            }
            catch (Exception e)
            {
                _logger.LogError(e.ToString());
            }
        }
    }
}