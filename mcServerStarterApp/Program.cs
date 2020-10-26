using System;
using System.Threading;
using System.Threading.Tasks;

namespace mcServerStarterApp
{
    internal class Program
    {
        private static CancellationTokenSource _shutDownCts;

        private static Task Main(string[] args)
        {
            return McServerStarter.Starter.StartAsync(interactiveMode: true);
        }
    }
}
