using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using System.Threading;

namespace DocSorter.Data
{
    internal static class Configuration
    {
        private static readonly string ConfigPath = Path.Combine(Program.BaseDirectory, "config.json");
        internal static List<Models.SortingEntry> SortingEntries { get; set; }

        private static FileSystemWatcher _fileWatcher;

        private static void InitFileWatcher()
        {
            if (_fileWatcher == null)
            {
                _fileWatcher = new FileSystemWatcher(Program.BaseDirectory, "config.json");
                _fileWatcher.Changed += _fileWatcher_Changed;
                _fileWatcher.EnableRaisingEvents = true;
            }
        }

        private static void _fileWatcher_Changed(object sender, FileSystemEventArgs e)
        {
            _fileWatcher.EnableRaisingEvents = false;

            Logger.CreateLog("Config changed. Reinit...");
            foreach (var entry in SortingService.Instances)
            {
                entry.Stop();
            }

            SortingService.Instances = new List<SortingService>();

            ReadConfig();
            SortingService.InitServices();

            _fileWatcher.EnableRaisingEvents = true;
        }

        internal static void ReadConfig()
        {
            InitFileWatcher();

            if (File.Exists(ConfigPath))
            {
                Thread.Sleep(500);
                try
                {
                    var configJson = File.ReadAllText(ConfigPath);
                    var sortingEntries = JsonConvert.DeserializeObject<List<Models.SortingEntry>>(configJson);
                    SortingEntries = sortingEntries;
                }
                catch (Exception ex)
                {
                    Logger.CreateLog(ex.Message);
                }
            }
            else
            {
                try
                {
                    //Create empty config
                    File.WriteAllText(ConfigPath, JsonConvert.SerializeObject(new List<Models.SortingEntry>
                    {
                        new Models.SortingEntry()
                        {
                            Substitutions = new List<Models.RegexSubstitution>()
                            {
                                new Models.RegexSubstitution()
                            },
                            SortingConditions = new List<Models.SortingCondition>()
                            {
                                new Models.SortingCondition()
                                {
                                Substitutions = new List<Models.RegexSubstitution>()
                                }
                            }
                        }
                    }, Formatting.Indented));

                    SortingEntries = new List<Models.SortingEntry>();
                }
                catch (Exception ex)
                {
                    Logger.CreateLog(ex.Message);
                }
            }
        }
    }
}
