using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace SeekAndArchive
{
    class Program
    {
        static List<FileInfo> FoundFiles;
        static List<FileSystemWatcher> watchers;

        static void Main(string[] args)
        {
            string fileName = args[0];
            string directoryName = args[1];
            FoundFiles = new List<FileInfo>();
            watchers = new List<FileSystemWatcher>();

            DirectoryInfo rootDir = new DirectoryInfo(directoryName);
            if (!rootDir.Exists)
            {
                Console.WriteLine("The specified directory does not exist.");
                return;
            }
            Console.WriteLine($"Searching for {fileName} in {directoryName} and its subdirectories.");
            RecursiveSearch(FoundFiles, fileName, rootDir);
            Console.WriteLine("Found {0} files.", FoundFiles.Count);
            foreach (FileInfo fil in FoundFiles)
            {
                Console.WriteLine("{0}", fil.FullName);
            }
            foreach (FileInfo fil in FoundFiles)
            {
                FileSystemWatcher newWatcher = new FileSystemWatcher(fil.DirectoryName, fil.Name);
                newWatcher.Changed += new FileSystemEventHandler(WatcherModified);
                newWatcher.Renamed += new RenamedEventHandler(WatcherRenamed);
                newWatcher.Deleted += new FileSystemEventHandler(WatcherModified);
                newWatcher.EnableRaisingEvents = true;
                watchers.Add(newWatcher);
            }
            Console.ReadKey();
        }

        static void RecursiveSearch(List<FileInfo> foundFiles, string fileName, DirectoryInfo currentDirectory)
        {
            string fileExtension = fileName.Substring(fileName.IndexOf('.'));
            string fileNamePattern = BuildSearchPattern(fileName);

            foreach (FileInfo fil in currentDirectory.GetFiles())
            {
                string filShortName = fil.Name.Split('.')[0];

                if (fil.Extension.Equals(fileExtension) && new Regex(fileNamePattern).IsMatch(filShortName))
                {
                    foundFiles.Add(fil);
                }
            }
            foreach (DirectoryInfo dir in currentDirectory.GetDirectories())
            {
                RecursiveSearch(foundFiles, fileName, dir);
            }
        }

        static string BuildSearchPattern(string fileName)
        {
            string fileShortName = fileName.Split('.')[0];
            string fileNamePattern = fileShortName;

            if (fileShortName.Contains("*"))
            {
                if (fileShortName.Count(c => c == '*') == 2)
                {
                    fileNamePattern = @"[A-Za-z0-9_\-]*" + fileShortName.Trim('*') + @"[A-Za-z0-9_\-]*";
                }
                else if (fileShortName.IndexOf('*') == 0)
                {
                    fileNamePattern = @"[A-Za-z0-9_\-]*" + fileShortName.TrimStart('*') + @"$";
                }
                else
                {
                    fileNamePattern = @"^" + fileShortName.TrimEnd('*') + @"[A-Za-z0-9_\-]*";
                }
            }
            else if (fileShortName.Contains("?"))
            {
                int wildCardIndex = fileShortName.IndexOf('?');
                string firstNamePart = fileShortName.Substring(0, wildCardIndex);
                string secondNamePart = fileShortName.Substring(wildCardIndex + 1);
                fileNamePattern = @"^" + firstNamePart + @"[A-Za-z0-9_\-]" + secondNamePart + @"$";
            }
            return fileNamePattern;
        }

        static void WatcherModified(object sender, FileSystemEventArgs e)
        {
            Console.WriteLine($"{e.FullPath} has been {e.ChangeType.ToString().ToLower()}.");
        }

        static void WatcherRenamed(object sender, RenamedEventArgs e)
        {
            Console.WriteLine($"{e.OldFullPath} has been {e.ChangeType.ToString().ToLower()} to {e.FullPath}");
        }
    }
}