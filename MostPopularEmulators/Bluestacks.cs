using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Text.RegularExpressions;
using Zeraniumu;

namespace MostPopularEmulators
{
    public class Bluestacks 
    {
        private string BlueStackPath, BootParameters, VBoxManager, AdbShellOptions;
        public Rectangle ActualSize()
        {
            return new Rectangle(1, 31, 640, 360);
        }

        public string AdbIpPort()
        {
            var port = Registry.LocalMachine.OpenSubKey(@"\SOFTWARE\BlueStacks\Guests\Android\Guests\Android\Config").GetValue("BstAdbPort").ToString();
            return "127.0.0.1:" + port;
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
            if (result.ToString().ToLower() == "plus")
            {
                plusMode = true;
            }
            if (plusMode)
            {
                frontendexe = new string[] { "HD-Plus-Frontend.exe" };
            }
            var files = new List<string> { "HD-Quit.exe" };
            files.AddRange(frontendexe);
            frontendexe = files.ToArray();
            BlueStackPath = key.GetValue("InstallDir").ToString();
            if (!Directory.Exists(BlueStackPath))
            {
                string programFiles = Environment.ExpandEnvironmentVariables("%ProgramW6432%");
                string programFilesX86 = Environment.ExpandEnvironmentVariables("%ProgramFiles(x86)%");
                if (Directory.Exists(Path.Combine(programFiles, "BlueStacks")))
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
            foreach(var file in frontendexe)
            {
                if (File.Exists(Path.Combine(BlueStackPath,file)))
                {
                    VBoxManager = BlueStackPath + "BstkVMMgr.exe";
                    AdbShellOptions = "/data/anr/../../system/xbin/bstk/su root";
                    BlueStackPath = Path.Combine(BlueStackPath, file);
                    BootParameters = key.OpenSubKey(@"\SOFTWARE\BlueStacks\Guests\Android").GetValue("BootParameters").ToString();
                    
                    return true;
                }
            }
            return false;
        }

        public string DefaultArguments()
        {
            return BootParameters;
        }

        public Rectangle DefaultSize()
        {
            return new Rectangle(0, 0, 640, 360);
        }

        public string EmulatorName()
        {
            return "BlueStacks";
        }

        public void SetResolution(int x, int y, int dpi)
        {
            var key = Registry.LocalMachine.OpenSubKey(@"\SOFTWARE\BlueStacks\Guests\Android\FrameBuffer\0");
            key.SetValue("FullScreen", 0, RegistryValueKind.DWord);
            key.SetValue("GuestHeight", y, RegistryValueKind.DWord);
            key.SetValue("GuestWidth", x, RegistryValueKind.DWord);
            key.SetValue("WindowHeight", y, RegistryValueKind.DWord);
            key.SetValue("GuestWidth", x, RegistryValueKind.DWord);
            BootParameters = Regex.Replace(BootParameters, "DPI=\\d+", "DPI=" + dpi);
            Registry.LocalMachine.OpenSubKey(@"\SOFTWARE\BlueStacks\Guests\Android").SetValue("BootParameters", BootParameters , RegistryValueKind.String);
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
            
        }
    }
}
