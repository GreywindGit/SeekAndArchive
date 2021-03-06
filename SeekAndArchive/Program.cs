﻿using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text.RegularExpressions;

namespace SeekAndArchive
{
    class Program
    {
        static List<FileInfo> FoundFiles;
        static List<FileSystemWatcher> watchers;
        static List<DirectoryInfo> archiveDirs;

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
                newWatcher.Changed += new FileSystemEventHandler(OnChanged);
                newWatcher.Renamed += new RenamedEventHandler(OnRenamed);
                newWatcher.Deleted += new FileSystemEventHandler(OnChanged);
                newWatcher.EnableRaisingEvents = true;
                watchers.Add(newWatcher);
            }

            archiveDirs = new List<DirectoryInfo>();
            for (int i = 0; i < FoundFiles.Count; i++)
            {
                archiveDirs.Add(Directory.CreateDirectory("archive" + i.ToString()));
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

        static void OnChanged(object sender, FileSystemEventArgs e)
        {
            Console.WriteLine($"{e.FullPath} has been {e.ChangeType.ToString().ToLower()}.");
            FileSystemWatcher senderWatcher = (FileSystemWatcher)sender;
            int index = watchers.IndexOf(senderWatcher, 0);
            System.Threading.Thread.Sleep(1000);
            ArchiveFile(archiveDirs[index], FoundFiles[index]);
        }

        static void OnRenamed(object sender, RenamedEventArgs e)
        {
            Console.WriteLine($"{e.OldFullPath} has been {e.ChangeType.ToString().ToLower()} to {e.FullPath}");
        }

        static void ArchiveFile(DirectoryInfo archiveDir, FileInfo fileToArchive)
        {
            string timeStamp = DateTime.Now.ToString("yyyyMMdd-HHmm");
            FileStream input = fileToArchive.OpenRead();
            FileStream output = File.Create(archiveDir.FullName + @"\"  + fileToArchive.Name + timeStamp + ".gz");
            GZipStream Compressor = new GZipStream(output, CompressionMode.Compress);
            int b = input.ReadByte();
            while (b != -1)
            {
                Compressor.WriteByte((byte)b);

                b = input.ReadByte();
            }
            Compressor.Close();
            input.Close();
            output.Close();
        }
    }
}