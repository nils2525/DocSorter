using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DocSorter
{
    class Program
    {
        internal static bool LogInformation { get; private set; }

        static void Main(string[] args)
        {
            LogInformation = args.Contains("--debug");

            Data.Configuration.ReadConfig();
            SortingService.InitServices();

            //Prevents closing console
            new Task(() => { Task.Delay(-1); }).Wait(-1);
        }
    }
}
