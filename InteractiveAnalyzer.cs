using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using Microsoft.VisualBasic;

namespace GSInteractiveDeviceAnalyzer
{
    public static class InteractiveAnalyzer
    {
        public static void Main()
        {
            var currentPath = Directory.GetCurrentDirectory();
            var lastPath = string.Empty;
            var selectedIndex = 0;
            List<FileSystemInfo> items = new List<FileSystemInfo>();
            Dictionary<string, long> dirSizes = new Dictionary<string, long>();

            while (true)
            {
                if (currentPath != lastPath)
                {
                    items = LoadDirectoryItems(currentPath);

                    if (items.Count == 0)
                    {
                        items.Add(new FileInfo("EMPTY_FOLDER_NO_FILES_HERE"));
                    }
                    lastPath = currentPath;
                }

                if (selectedIndex >= items.Count)
                {
                    selectedIndex = Math.Max(0, items.Count - 1);
                }

                DrawMenu(currentPath, items, selectedIndex, dirSizes);

                var key = Console.ReadKey(true).Key;

                switch (key)
                {
                    case ConsoleKey.DownArrow:
                        selectedIndex = Math.Min(items.Count - 1, selectedIndex + 1);
                        break;
                    case ConsoleKey.UpArrow:
                        selectedIndex = Math.Max(0, selectedIndex - 1);
                        break;

                    case ConsoleKey.Backspace:
                    case ConsoleKey.Escape:
                        var parent = Directory.GetParent(currentPath);
                        if (parent != null)
                        {
                            currentPath = parent.FullName;
                            selectedIndex = 0;
                        }

                        break;

                    case ConsoleKey.Enter:
                        if (items[selectedIndex] is DirectoryInfo dir)
                        {
                            currentPath = dir.FullName;
                            selectedIndex = 0;
                        }
                        break;

                    case ConsoleKey.Delete:
                        ExecuteDelete(items[selectedIndex]);
                        lastPath = string.Empty; // Force reload after delete
                        break;
                }
            }
        }

        public static List<FileSystemInfo> LoadDirectoryItems(string path)
        {
            var items = new List<FileSystemInfo>();
            try
            {
                var dirInfo = new DirectoryInfo(path);

                items.AddRange(dirInfo.GetDirectories().Where(d => !d.Attributes.HasFlag(FileAttributes.Hidden | FileAttributes.System)));
                items.AddRange(dirInfo.GetFiles().Where(f => !f.Attributes.HasFlag((FileAttributes.Hidden | FileAttributes.System))));
            }
            catch (UnauthorizedAccessException )
            {
                
            }

            return items;
        }

        private static void DrawMenu(string currentPath, List<FileSystemInfo> items, int selectedIndex, Dictionary<string, long> dirSizes)
        {
            Console.Clear();

            Console.ForegroundColor = ConsoleColor.Cyan;

            // The @ symbol allows us to do multi-line ASCII art easily
            Console.WriteLine(@"
   _____  ___   ___  ____   _____  ___  _   _ _      _______        __
  / ____|/ _ \ / _ \|  _ \ / ____|/ _ \| | | | |    / ____\ \      / /
 | |  __| | | | | | | | | | (___ | | | | | | | |   | (___  \ \ /\ / / 
 | | |_ | | | | | | | | | |\___ \| | | | | | | |    \___ \  \ V  V /  
 | |__| | |_| | |_| | |_| |____) | |_| | |_| | |________) |  \_/\_/   
  \_____|\___/ \___/|____/|_____/ \___/ \___/|_____|_____/            
        ");

            Console.ResetColor();

            Console.WriteLine("=========================================================================");
            Console.WriteLine($"ROOT: {currentPath}");
            Console.WriteLine("[UP/DOWN] Navigate | [ENTER] Open | [BACKSPACE] Go Back | [DELETE] Nuke");
            Console.WriteLine("=========================================================================");

            var maxItems = Math.Max(5, Console.WindowHeight - 6);
            var startIndex = Math.Max(0, selectedIndex - maxItems / 2);
            var endIndex = Math.Min(items.Count, startIndex + maxItems);

            if (endIndex - startIndex < maxItems)
            {
                startIndex = Math.Max(0, endIndex - maxItems);
            }
            for (var i = startIndex; i < endIndex; i++)
            {
                if (i == selectedIndex)
                {
                    Console.BackgroundColor = ConsoleColor.DarkCyan;
                    Console.ForegroundColor = ConsoleColor.White;
                }
                else
                {
                    Console.ResetColor();
                }

                var item = items[i];

                if (item is DirectoryInfo dir)
                {
                    if (!dirSizes.TryGetValue(dir.FullName, out long size))
                    {
                        size = GetDirectorySize(dir);
                        dirSizes[dir.FullName] = size;
                    }
                    var sizeMb = size / (1024.0 * 1024.0);
                    Console.WriteLine($"[DIR] {item.Name,-40} | {sizeMb:F2} MB");
                }
                else if (item is FileInfo file)
                {
                    var sizeMb = file.Length / (1024.0 * 1024.0);
                    Console.WriteLine($"[FILE] {item.Name,-40} | {sizeMb:F2} MB");
                }
                else
                {
                    Console.WriteLine($"{item.Name,-40}");
                }
            }

            Console.ResetColor();
        }

        public static long GetDirectorySize(DirectoryInfo dir)
        {
            long size = 0;
            try
            {
                foreach (var file in dir.GetFiles())
                {
                    size += file.Length;
                }

                foreach (var subDir in dir.GetDirectories())
                {
                    size += GetDirectorySize(subDir);
                }
            }
            catch (UnauthorizedAccessException) { }
            catch (Exception) { }

            return size;
        }

        public static void ExecuteDelete(FileSystemInfo item)
        {
            if (item.Name == "EMPTY_FOLDER_NO_FILES_HERE") return;

            Console.ResetColor();
            Console.Write($"\n ARE YOU SURE YOU WANT TO DELETE THIS!!!! '");
            Console.ForegroundColor = ConsoleColor.Red;
            Console.Write(item.Name);
            Console.ResetColor();
            Console.Write("'Permanently? (Y/N): ");

            var confirm = Console.ReadKey(true).Key;
            if (confirm == ConsoleKey.Y)
            {
                try
                {
                    if (item is DirectoryInfo dir)
                    {
                        dir.Delete(true);
                    }
                    else if (item is FileInfo file)
                    {
                        file.Delete();
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error deleting item: {ex.Message}");
                    Console.WriteLine("Press any key to continue...");
                    Console.ReadKey(true);
                }
            }
          
        }
    }
}