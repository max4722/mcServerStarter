using System;
using System.ServiceProcess;
using System.Threading;
using System.Threading.Tasks;

namespace McServerStarterService
{
    public partial class McServerStarterService : ServiceBase
    {
        private CancellationTokenSource _shutDownCts;
        private Task _runTask;

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
            try
            {
                using (_shutDownCts = new CancellationTokenSource())
                {
                    //while (!_shutDownCts.IsCancellationRequested)
                    {
                        try
                        {
                            _runTask = McServerStarter.Starter.StartAsync(interactiveMode: false);
                            await _runTask;
                        }
                        catch (Exception e)
                        {
                            EventLog.WriteEntry(e.ToString(), System.Diagnostics.EventLogEntryType.Error);
                        }

                        //await Task.Delay(1000);
                    }
                }
            }
            finally
            {
                _shutDownCts = null;
                _runTask = null;
            }
        }

        protected async override void OnStop()
        {
            _shutDownCts?.Cancel();
            McServerStarter.Starter.Shutdown();
            while (_runTask != null)
            {
                RequestAdditionalTime(1000);
                await Task.Delay(1000, _shutDownCts.Token);
            }
        }
    }
}
