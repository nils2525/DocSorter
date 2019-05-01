using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace DocSorter.Data
{
    internal static class Configuration
    {
        private const string ConfigPath = "config.json";
        internal static List<Models.SortingEntry> SortingEntries { get; set; }

        internal static void ReadConfig()
        {
            if (File.Exists(ConfigPath))
            {
                var configJson = File.ReadAllText(ConfigPath);
                var sortingEntries = JsonConvert.DeserializeObject<List<Models.SortingEntry>>(configJson);
                SortingEntries = sortingEntries;
            }
            else
            {
                //Create empty config
                File.WriteAllText(ConfigPath, JsonConvert.SerializeObject(new List<Models.SortingEntry> { new Models.SortingEntry() { Substitutions = new List<Models.RegexSubstitution>() { new Models.RegexSubstitution() } } }, Formatting.Indented));
            }
        }
    }
}
