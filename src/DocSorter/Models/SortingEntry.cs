using System;
using System.Collections.Generic;
using System.Text;

namespace DocSorter.Models
{
    public class SortingEntry
    {
        public string FileNameCondition { get; set; }
        public string FileContentCondition { get; set; }

        public string SourceFolder { get; set; }
        public string DestinationFolder { get; set; }

        public bool IncludeSubfolders { get; set; }

        public List<RegexSubstitution> Substitutions { get; set; } = new List<RegexSubstitution>();
    }

    public class RegexSubstitution
    {
        public string RegexCondition { get; set; }
        public string Replacement { get; set; }
    }
}
