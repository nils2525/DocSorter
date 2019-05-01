using DocSorter.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace DocSorter
{
    public class SortingService
    {
        private SortingEntry _sortingEntry;
        private FileSystemWatcher _fileWatcher;

        public SortingService(SortingEntry sortingEntry)
        {
            _sortingEntry = sortingEntry;
        }


        public bool Start()
        {
            if (_sortingEntry == null)
            {
                return false;
            }

            _fileWatcher = new FileSystemWatcher(_sortingEntry.SourceFolder);
            _fileWatcher.Created += _fileWatcher_Created;
            _fileWatcher.Changed += _fileWatcher_Changed;
            _fileWatcher.Renamed += _fileWatcher_Renamed;
            _fileWatcher.EnableRaisingEvents = true;
            return true;
        }

        private void _fileWatcher_Renamed(object sender, RenamedEventArgs e)
        {
            HandleSingleFile(e.FullPath);
        }
        private void _fileWatcher_Changed(object sender, FileSystemEventArgs e)
        {
            HandleSingleFile(e.FullPath);
        }
        private void _fileWatcher_Created(object sender, FileSystemEventArgs e)
        {
            HandleSingleFile(e.FullPath);
        }


        private bool HandleSingleFile(string fullPath)
        {
            if (!String.IsNullOrWhiteSpace(fullPath) && File.Exists(fullPath))
            {
                return false;
            }


            //If no file condition or condition matched
            if (String.IsNullOrWhiteSpace(_sortingEntry.FileNameCondition) || Regex.IsMatch(fullPath, _sortingEntry.FileNameCondition))
            {
                if (Path.GetExtension(fullPath) == ".pdf")
                {
                    if (String.IsNullOrWhiteSpace(_sortingEntry.FileContentCondition))
                    {
                        var text = TextReader.ReadText(fullPath, FileTypes.pdf);

                        if (String.IsNullOrWhiteSpace(_sortingEntry.FileContentCondition) || Regex.IsMatch(text, _sortingEntry.FileContentCondition))
                        {
                            if (!String.IsNullOrWhiteSpace(_sortingEntry.DestinationFolder))
                            {
                                if (!Directory.Exists(_sortingEntry.DestinationFolder))
                                {
                                    Directory.CreateDirectory(_sortingEntry.DestinationFolder);
                                }

                                //Move file to new position
                                var fileName = Path.GetFileName(fullPath);
                                var newFilename = UpdateFileName(fileName);

                                var fullDestinationPath = Path.Combine(_sortingEntry.DestinationFolder, newFilename);
                                Console.WriteLine("Moving '" + fullPath + "' to '" + fullDestinationPath + "'");
                                File.Move(fullPath, fullDestinationPath);
                            }
                        }
                    }
                }
            }


            return false;
        }

        private string UpdateFileName(string fileName)
        {
            var newName = fileName;
            foreach (var substitution in _sortingEntry.Substitutions)
            {
                newName = Regex.Replace(newName, substitution.RegexCondition, substitution.Replacement);
            }

            return newName;
        }
    }
}
