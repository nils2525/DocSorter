using DocSorter.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

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


            foreach (var file in Directory.GetFiles(_sortingEntry.SourceFolder, String.Empty, _sortingEntry.IncludeSubfolders ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly))
            {
                HandleSingleFile(file);
            }


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


                if (!WaitForFileAccess(fullPath))
                {
                    return;
                }

                if (FileContentMatches(fullPath, condition))
                {
                    //Content matches, move file
                    MoveFile(fullPath, condition);
                    conditionMatched = true;
                    break;
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

            //Get new file name
            var fileName = Path.GetFileName(fullPath);
            var newFilename = UpdateFileName(fileName, condition);

            //Move file to new position
            var fullDestinationPath = FillCustomParameters(FillDateParameters(Path.Combine(condition.DestinationFolder, newFilename)));
            var destinationFolder = Path.GetDirectoryName(fullDestinationPath);

            //Create destination folder if not exist
            if (!Directory.Exists(destinationFolder))
            {
                Directory.CreateDirectory(destinationFolder);
            }


            if (fullPath != fullDestinationPath)
            {
                Logger.CreateLog("Moving '" + fullPath + "' to '" + fullDestinationPath + "'");

                try
                {
                    while (File.Exists(fullDestinationPath))
                    {
                        fullDestinationPath = IncrementFileNumber(fullDestinationPath);
                    }

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

        /// <summary>
        /// Fill datetime to pathe where parameter is specified with $$x$$
        /// </summary>
        /// <param name="filePath"></param>
        /// <returns></returns>
        private string FillDateParameters(string filePath)
        {
            var result = filePath;

            var matches = Regex.Matches(filePath, @"(?<=\$\$).*(?=\$\$)");
            foreach (Match dateParameter in matches)
            {
                var formattedDate = DateTime.Now.ToString(dateParameter.Value);
                result = result.Replace("$$" + dateParameter.Value + "$$", formattedDate);
            }

            return result;
        }

        /// <summary>
        /// Fille custom regex parameters §§x§§
        /// </summary>
        /// <param name="filePath"></param>
        /// <returns></returns>
        private string FillCustomParameters(string filePath)
        {
            var result = filePath;

            var matches = Regex.Matches(filePath, @"(?<=§§).*(?=§§)");
            foreach (Match match in matches)
            {
                var replaceValue = Regex.Match(filePath, match.Value);
                result = result.Replace("§§" + match.Value + "§§", replaceValue.Value);
            }

            return result;
        }

        /// <summary>
        /// Increment file number
        /// </summary>
        /// <param name="filePath"></param>
        /// <returns></returns>
        private string IncrementFileNumber(string filePath)
        {
            var searchPattern = @"(?<=_).(?=\..{2,4}$)";
            var newNumberPattern = @"\..{2,4}$";

            var result = filePath;

            var existingNumber = Regex.Match(filePath, searchPattern);
            if (existingNumber.Success && Int32.TryParse(existingNumber.Value, out int currentNumber))
            {
                result = Regex.Replace(filePath, searchPattern, (++currentNumber).ToString());
            }
            else
            {
                var fileEnd = Regex.Match(filePath, newNumberPattern);
                result = filePath.Replace(fileEnd.Value, "_2" + fileEnd);
            }

            if (result == filePath)
            {
                // Incrementing number failed. Add current ticks as an alternative to filepath
                result += DateTime.Now.Ticks;
            }

            return result;
        }


        private bool WaitForFileAccess(string filePath)
        {
            var isUsed = true;

            var currentTime = DateTime.Now.AddHours(1);
            while (isUsed)
            {
                try
                {
                    var file = new FileInfo(filePath);
                    using (FileStream stream = file.Open(FileMode.Open, FileAccess.Read, FileShare.None))
                    {
                        stream.Close();
                        isUsed = false;
                    }
                }
                catch (IOException)
                {
                    if (currentTime <= DateTime.Now)
                    {
                        return false;
                    }

                    Thread.Sleep(1000);
                    //the file is unavailable because it is:
                    //still being written to
                    //or being processed by another thread
                    //or does not exist (has already been processed)
                }
            }

            return true;
        }
    }
}
