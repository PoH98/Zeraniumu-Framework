using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using Zeraniumu;

namespace MostPopularEmulators
{
    public class Bluestacks 
    {
        private string BlueStackPath;
        public Rectangle ActualSize()
        {
            throw new NotImplementedException();
        }

        public string AdbIpPort()
        {
            throw new NotImplementedException();
        }

        public bool CheckEmulatorExist(string arguments, ILog logger)
        {
            var plusMode = false;
            var frontendexe = new string[] { "HD-Frontend.exe", "HD-Player.exe" };
            RegistryKey key = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Default);
            key = key.OpenSubKey(@"\SOFTWARE\BlueStacks");
            if(key == null)
            {
                return false;
            }
            var result = key.GetValue("Engine");
            if(result.ToString().ToLower() == "plus")
            {
                plusMode = true;
            }
            if (plusMode)
            {
                frontendexe = new string[] { "HD-Plus-Frontend.exe" };
            }
            var files = new List<string> { "HD-Quit.exe" };
            files.AddRange(frontendexe);
            BlueStackPath = key.GetValue("InstallDir").ToString();
            string programFiles = Environment.ExpandEnvironmentVariables("%ProgramW6432%");
            string programFilesX86 = Environment.ExpandEnvironmentVariables("%ProgramFiles(x86)%");
            if (!Directory.Exists(BlueStackPath))
            {
                if(Directory.Exists(Path.Combine(programFiles, "BlueStacks")))
                {
                    BlueStackPath = Path.Combine(programFiles, "BlueStacks");
                }
                else if(Directory.Exists(Path.Combine(programFilesX86, "BlueStacks")))
                {
                    BlueStackPath = Path.Combine(programFilesX86, "BlueStacks");
                }
                else
                {
                    return false;
                }
            }
            foreach(var file in files)
            {

            }
            return false;
        }

        public string DefaultArguments()
        {
            throw new NotImplementedException();
        }

        public Rectangle DefaultSize()
        {
            throw new NotImplementedException();
        }

        public string EmulatorName()
        {
            throw new NotImplementedException();
        }

        public void SetResolution(int x, int y, int dpi)
        {
            throw new NotImplementedException();
        }

        public void StartEmulator(string arguments)
        {
            throw new NotImplementedException();
        }

        public void StopEmulator(Process emulator, string arguments)
        {
            throw new NotImplementedException();
        }

        public void UnUnbotify(EmulatorController controller)
        {
            throw new NotImplementedException();
        }
    }
}
