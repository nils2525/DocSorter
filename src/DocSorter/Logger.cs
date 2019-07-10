using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;

namespace DocSorter
{
    public class Logger
    {
        private static readonly string LogFile = Path.Combine(Program.BaseDirectory, "Log.txt");
        public static void CreateLog(string message)
        {
            var completeMessage = DateTime.Now.ToString("s") + " - " + message + Environment.NewLine;
            Console.WriteLine(message);
            File.AppendAllText(LogFile, completeMessage);
        }
    }
}
