using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading;
using Zeraniumu;

namespace MostPopularEmulators
{
    public class Bluestacks:IEmulator
    {
        private string BlueStackPath, BootParameters, VBoxManagerPath, _adbShellOptions;
        private Process bluestacks;
        public Rectangle ActualSize()
        {
            return new Rectangle(1, 31, 960, 540);
        }
        public string AdbShellOptions => _adbShellOptions;
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
                    VBoxManagerPath = BlueStackPath + "BstkVMMgr.exe";
                    _adbShellOptions = "/data/anr/../../system/xbin/bstk/su root";
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
            return new Rectangle(0, 0, 960, 540);
        }

        public string EmulatorName()
        {
            return "BlueStacks|藍疊";
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
            ProcessStartInfo info = new ProcessStartInfo();
            info.FileName = BlueStackPath;
            info.Arguments = arguments;
            bluestacks = Process.Start(info);
            Thread.Sleep(5000);
        }

        public void StopEmulator(Process emulator, string arguments)
        {
            ProcessStartInfo close = new ProcessStartInfo();
            close.FileName = VBoxManagerPath;
            if (arguments.Length > 0)
            {
                close.Arguments = "controlvm " + arguments + " poweroff";
            }
            else
            {
                close.Arguments = "controlvm MEmu poweroff";
            }
            close.CreateNoWindow = true;
            close.WindowStyle = ProcessWindowStyle.Hidden;
            try
            {
                emulator.Kill();
            }
            catch
            {

            }
            Process p = Process.Start(close);
            Thread.Sleep(5000);
            if (!p.HasExited)
            {
                try
                {
                    p.Kill();
                }
                catch
                {

                }
            }
        }

        public void UnUnbotify(EmulatorController controller)
        {
            controller.ExecuteAdbCommand("rm -f /system/bin/microvirtd");
            controller.ExecuteAdbCommand("rm -f /system/etc/init.microvirt.sh");
            controller.ExecuteAdbCommand("rm -f /system/bin/memud");
            controller.ExecuteAdbCommand("rm -f /system/lib/memuguest.ko");
        }

        public Point GetAccurateClickPoint(Point point)
        {
            return new Point(32767 / ActualSize().Width * point.X, 32767 / ActualSize().Height * point.Y);
        }
    }
}
