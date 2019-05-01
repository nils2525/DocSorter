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

            var instances = new List<SortingService>();

            foreach (var entry in Data.Configuration.SortingEntries)
            {
                var instance = new SortingService(entry);
                instance.Start();
                instances.Add(instance);
            }

            //Prevents closing console
            new Task(() => { Task.Delay(-1); }).Wait(-1);
        }
    }
}
