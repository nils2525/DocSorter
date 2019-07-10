using DocSorter.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;

namespace DocSorter
{
    public class SortingService
    {
        public static List<SortingService> Instances { get; set; } = new List<SortingService>();

        public static void InitServices()
        {
            foreach (var entry in Data.Configuration.SortingEntries)
            {
                var instance = new SortingService(entry);
                if (instance.Start())
                {
                    Instances.Add(instance);
                }
            }
        }

        private SortingEntry _sortingEntry;
        private FileSystemWatcher _fileWatcher;

        public SortingService(SortingEntry sortingEntry)
        {
            _sortingEntry = sortingEntry;
        }


        public bool Start()
        {
            if (_sortingEntry == null || String.IsNullOrWhiteSpace(_sortingEntry.SourceFolder) || _sortingEntry.SortingConditions.Any(c => String.IsNullOrWhiteSpace(c.DestinationFolder)))
            {
                return false;
            }

            _fileWatcher = new FileSystemWatcher(_sortingEntry.SourceFolder);
            _fileWatcher.Created += _fileWatcher_Created;
            _fileWatcher.Changed += _fileWatcher_Changed;
            _fileWatcher.Renamed += _fileWatcher_Renamed;
            _fileWatcher.IncludeSubdirectories = _sortingEntry.IncludeSubfolders;
            _fileWatcher.EnableRaisingEvents = true;

            Logger.CreateLog("Start service for folder " + _sortingEntry.SourceFolder);
            return true;
        }

        public void Stop()
        {
            _fileWatcher.EnableRaisingEvents = false;
            _fileWatcher.Dispose();
            _fileWatcher = null;
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


        private void HandleSingleFile(string fullPath)
        {
            Thread.Sleep(1000);

            if (String.IsNullOrWhiteSpace(fullPath) || !File.Exists(fullPath))
            {
                //File not exist
                return;
            }

            bool conditionMatched = false;

            foreach (var condition in _sortingEntry.SortingConditions)
            {
                if (!String.IsNullOrWhiteSpace(condition.FileNameCondition) && !Regex.IsMatch(fullPath, condition.FileNameCondition))
                {
                    //Regex does not match
                    continue;
                }

                if (FileContentMatches(fullPath, condition))
                {
                    //Content matches, move file
                    MoveFile(fullPath, condition);
                    conditionMatched = true;
                }
            }

            if (!conditionMatched)
            {
                MoveFile(fullPath, new SortingCondition() { DestinationFolder = _sortingEntry.SourceFolder });
            }
        }

        /// <summary>
        /// True = file content matches regex (or no condition defined)
        /// </summary>
        /// <param name="fullPath"></param>
        /// <returns></returns>
        private bool FileContentMatches(string fullPath, SortingCondition condition)
        {
            if (!string.IsNullOrWhiteSpace(condition.FileContentCondition))
            {
                //Read content
                string text = "";
                if (Path.GetExtension(fullPath) == ".pdf")
                {
                    text = TextReader.ReadText(fullPath, FileTypes.pdf);
                }

                return Regex.IsMatch(text, condition.FileContentCondition);
            }
            else
            {
                return true;
            }
        }

        /// <summary>
        /// Move file to new destination (configure in _sortingEntry)
        /// </summary>
        /// <param name="fullPath"></param>
        private void MoveFile(string fullPath, SortingCondition condition)
        {
            //Create destination folder if not exist
            if (!Directory.Exists(condition.DestinationFolder))
            {
                Directory.CreateDirectory(condition.DestinationFolder);
            }

            //Get new file name
            var fileName = Path.GetFileName(fullPath);
            var newFilename = UpdateFileName(fileName, condition);

            //Move file to new position
            var fullDestinationPath = Path.Combine(condition.DestinationFolder, newFilename);

            if (fullPath != fullDestinationPath)
            {
                Logger.CreateLog("Moving '" + fullPath + "' to '" + fullDestinationPath + "'");

                try
                {
                    File.Move(fullPath, fullDestinationPath);
                }
                catch (Exception ex)
                {
                    Logger.CreateLog(ex.Message);
                }               
            }
        }

        /// <summary>
        /// Get updated filename (replace regex matches in _sortingEntry.Substitutions)
        /// </summary>
        /// <param name="fileName"></param>
        /// <returns></returns>
        private string UpdateFileName(string fileName, SortingCondition condition)
        {
            var newName = fileName;
            foreach (var substitution in _sortingEntry.Substitutions)
            {
                newName = Regex.Replace(newName, substitution.RegexCondition, substitution.Replacement);
            }

            foreach (var substitution in condition.Substitutions)
            {
                newName = Regex.Replace(newName, substitution.RegexCondition, substitution.Replacement);
            }

            return newName;
        }
    }
}
