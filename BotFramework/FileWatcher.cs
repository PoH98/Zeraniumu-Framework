using System;
using System.IO;
using System.Threading;
using System.Windows.Forms;

namespace Zeraniumu
{
    public class FileWatcher
    {
        /// <summary>
        /// Watch if our running script is modified
        /// </summary>
        /// <param name="path"></param>
        /// <param name="file"></param>
        public static void CreateFileWatcher(string file)
        {
            // Create a new FileSystemWatcher and set its properties.
            FileSystemWatcher watcher = new FileSystemWatcher();
            if (file.Contains("\\"))
            {
                watcher.Path = file.Remove(file.LastIndexOf('\\'));
            }
            else
            {
                watcher.Path = Environment.CurrentDirectory;
            }
            /* Watch for changes in LastAccess and LastWrite times, and 
               the renaming of files or directories. */
            watcher.NotifyFilter = NotifyFilters.LastAccess | NotifyFilters.LastWrite
               | NotifyFilters.FileName | NotifyFilters.DirectoryName;
            // Only watch text files.
            if (file.Contains("\\"))
            {
                watcher.Filter = file.Remove(0, file.LastIndexOf('\\'));
            }
            else
            {
                watcher.Filter = file;
            }
            // Add event handlers.
            watcher.Changed += new FileSystemEventHandler(OnChanged);

            // Begin watching.
            watcher.EnableRaisingEvents = true;
        }

        // Define the event handlers.
        private static void OnChanged(object source, FileSystemEventArgs e)
        {
            if(e.ChangeType == WatcherChangeTypes.Changed)
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("Script changed! Restarting process for updating script!");
                Application.Restart();
                Thread.Sleep(3000);
                Environment.Exit(0);
            }
        }
    }
}
