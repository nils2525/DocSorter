using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace DocSorter
{
    class Program
    {
        internal static bool LogInformation { get; private set; }
        internal static string BaseDirectory { get; private set; }

        // In program.cs
        static async Task Main(string[] args)
        {            
            BaseDirectory = Path.GetDirectoryName(Assembly.GetAssembly(typeof(Program)).Location);
            Console.WriteLine(BaseDirectory);

            LogInformation = args.Contains("--debug");
                        

            var builder = new HostBuilder().ConfigureServices((hostContext, services) =>
            {
                services.AddHostedService<AppService>();
            });

            if (!(Debugger.IsAttached || args.Contains("--console")))
            {
                //Run as service
                await builder
                    .ConfigureServices((hostContext, services) => services.AddSingleton<IHostLifetime, ServiceBaseLifeTime>())
                    .Build().RunAsync();
            }
            else
            {
                //Run in console mode
                await builder.RunConsoleAsync();
            }


        }
    }
}
