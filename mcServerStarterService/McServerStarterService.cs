using System;
using System.ServiceProcess;
using System.Threading;
using System.Threading.Tasks;

namespace McServerStarterService
{
    public partial class McServerStarterService : ServiceBase
    {
        private CancellationTokenSource _shutDownCts;

        public McServerStarterService()
        {
            InitializeComponent();
        }

        protected override void OnStart(string[] args)
        {
            RunServiceAsync();
        }

        private async void RunServiceAsync()
        {
            using (_shutDownCts = new CancellationTokenSource())
            {
                while (!_shutDownCts.IsCancellationRequested)
                {
                    try
                    {
                        await McServerStarter.Starter.StartAsync(interactiveMode: false);
                    }
                    catch (Exception e)
                    {
                        EventLog.WriteEntry(e.ToString(), System.Diagnostics.EventLogEntryType.Error);
                    }

                    await Task.Delay(1000);
                }
            }

            _shutDownCts = null;
        }

        protected override void OnStop()
        {
            _shutDownCts?.Cancel();
            McServerStarter.Starter.Shutdown();
        }
    }
}
