// <copyright file="Starter.cs" company="kf corp">
// Licensed under the Apache 2.0 license
// </copyright>

namespace McServerStarter
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.Configuration.Json;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Logging.Console;

    public static class Starter
    {
        private static IServiceProvider _serviceProvider;
        private static CancellationTokenSource _shutDownCts;
        private static ManualResetEventSlim _done;

        public static async Task StartAsync(bool interactiveMode)
        {
            _serviceProvider = ConfigureServices();

            try
            {
                using (_shutDownCts = new CancellationTokenSource())
                {
                    using (_done = new ManualResetEventSlim(false))
                    {
                        try
                        {
                            AttachCtrlcSigtermShutdown(interactiveMode);
                            var monitorService = _serviceProvider.GetService<IMonitorService>();
                            await monitorService.RunAsync(_shutDownCts.Token, interactiveMode);
                        }
                        finally
                        {
                            _done.Set();
                        }
                    }
                }
            }
            finally
            {
                _done = null;
                _shutDownCts = null;
                await ((IAsyncDisposable)_serviceProvider).DisposeAsync();
                _serviceProvider = null;
            }
        }

        public static void Shutdown()
        {
            try
            {
                _shutDownCts?.Cancel();
            }
            catch (ObjectDisposedException)
            {
            }

            _done?.Wait();
        }

        private static IServiceProvider ConfigureServices()
        {
            var services = new ServiceCollection();

            ConfigureOptions(services)
                .AddSingleton<IServiceLogger, ServiceLogger>()
                .AddSingleton<IMonitorService, MonitorService>()
                .AddTransient(typeof(ILog<>), typeof(SimpleLogger<>));

            return services.BuildServiceProvider();
        }

        private static IServiceCollection ConfigureOptions(IServiceCollection services)
        {
            var configSource = new JsonConfigurationSource { Path = "./options.json" };
            var configuration = new ConfigurationBuilder().Add(configSource).Build();

            return services.Configure<MonitorServiceOptions>(options => configuration.GetSection("MonitorServiceOptions").Bind(options))
                           .Configure<ConsoleLoggerOptions>(options => configuration.GetSection("ConsoleLoggerOptions").Bind(options));
        }

        private static void AttachCtrlcSigtermShutdown(bool interactiveMod)
        {
            if (interactiveMod)
            {
                Console.TreatControlCAsInput = true;
                Console.CancelKeyPress += OnCancelKeyPressed;

                void OnCancelKeyPressed(object sender, ConsoleCancelEventArgs eventArgs)
                {
                    Console.CancelKeyPress -= OnCancelKeyPressed;

                    Shutdown();

                    // Don't terminate the process immediately, wait for the Main thread to exit gracefully.
                    eventArgs.Cancel = true;
                }
            }

            AppDomain.CurrentDomain.ProcessExit += OnAppDomainCurrentDomainProcessExit;
            void OnAppDomainCurrentDomainProcessExit(object sender, EventArgs eventArgs)
            {
                AppDomain.CurrentDomain.ProcessExit += OnAppDomainCurrentDomainProcessExit;
                Shutdown();
            }
        }
    }
}
