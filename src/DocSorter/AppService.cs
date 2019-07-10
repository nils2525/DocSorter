using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace DocSorter
{
    public class AppService : IHostedService
    {
        public Task StartAsync(CancellationToken cancellationToken)
        {
            Data.Configuration.ReadConfig();
            SortingService.InitServices();
            return Task.CompletedTask;
        }
        public Task StopAsync(CancellationToken cancellationToken)
        {
            foreach(var instance in SortingService.Instances)
            {
                instance.Stop();
            }
            return Task.CompletedTask;
        }
    }
}
