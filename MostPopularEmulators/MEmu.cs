using Microsoft.Win32;
using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Threading;
using Zeraniumu;

namespace MostPopularEmulators
{
    public class MEmu : IEmulator
    {
        private string location, arguments, VBoxManagerPath;
        private ILog logger;
        public Rectangle ActualSize()
        {
            return new Rectangle(1, 31, 640, 360);
        }
        public string AdbShellOptions => null;
        public string AdbIpPort()
        {
            var path = location.ToString().Replace("\0", "");
            if (Directory.Exists(path))
            {
                VBoxManagerPath = path + @"\MEmuHyperv\MEmuManage.exe";
                ProcessStartInfo fetch = new ProcessStartInfo(VBoxManagerPath);
                if (arguments != null)
                {
                    fetch.Arguments = "showvminfo " + arguments;
                }
                else
                {
                    fetch.Arguments = "showvminfo MEmu";
                }
                fetch.CreateNoWindow = true;
                fetch.RedirectStandardOutput = true;
                fetch.UseShellExecute = false;
                int retryCount = 0;
                do
                {
                    Process fetching = Process.Start(fetch);
                    string result = fetching.StandardOutput.ReadToEnd();
                    string[] splitted = result.Split('\n');
                    foreach (var s in splitted)
                    {
                        if (s.Contains("name = ADB"))
                        {
                            var port = s.Substring(s.IndexOf("port = ") + 7, 5).Replace(" ", "");
                            return "127.0.0.1:" + port;
                        }
                    }
                    retryCount++;
                }
                while (retryCount < 20);
                throw new ArgumentException("Retried 20 times fetching required data but failed! MEmu init not success!");
            }
            throw new FileNotFoundException("MEmu is not found at installation path: " + path);
        }

        public bool CheckEmulatorExist(string arguments, ILog logger)
        {
            this.logger = logger;
            RegistryKey reg = null;
            if (Environment.Is64BitProcess)
            {
                reg = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64);
            }
            else
            {
                reg = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry32);
            }
            try
            {
                object location = null;
                var drives = DriveInfo.GetDrives();
                foreach (var d in drives)
                {
                    if (File.Exists(d.Name + @"Program Files\Microvirt\MEmu\MEmu.exe"))
                    {
                        location = d.Name + @"Program Files\Microvirt";
                    }
                }
                if (location == null)
                {
                    //We will try getting running processes as user might helped us opened it
                    foreach (var process in Process.GetProcesses().Where(x => x.ProcessName.Contains("MEmu")))
                    {
                        if (File.Exists(process.MainModule.FileName) && process.MainModule.FileName.EndsWith("MEmu.exe"))
                        {
                            location = process.MainModule.FileName.Replace(@"\MEmu\MEmu.exe", "");
                            RegistryKey r = reg.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Uninstall\\MEmu");
                            if (r == null)
                            {
                                //MEmu didnt have this registered, lets do it for next load we will able to get file easily
                                r = reg.CreateSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Uninstall\\MEmu");
                                r.SetValue("InstallLocation", location);
                            }
                            break;
                        }
                    }
                }
                if (location == null)
                {
                    RegistryKey r = reg.OpenSubKey("SOFTWARE\\WOW6432Node\\Microsoft\\Windows\\CurrentVersion\\Uninstall\\MEmu");
                    if (r == null)
                    {
                        r = reg.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Uninstall\\MEmu");
                    }
                    if (r == null)
                    {
                        return false;
                    }
                    location = r.GetValue("InstallLocation");
                    if (location == null)
                    {
                        location = r.GetValue("DisplayIcon");
                        if (location != null)
                        {
                            location = location.ToString().Remove(location.ToString().LastIndexOf("\\"));
                        }
                    }
                }
                //Found all the path of MEmu
                if (location != null)
                {
                    this.arguments = arguments;
                    this.location = location.ToString();
                    return true;
                }
            }
            catch (Exception ex)
            {
                logger.WritePrivateLog(ex.ToString());
            }
            return false;
        }

        public string DefaultArguments()
        {
            return "MEmu";
        }

        public Rectangle DefaultSize()
        {
            return new Rectangle(0,0,640, 360);
        }

        public string EmulatorName()
        {
            return "MEmu|逍遥";
        }

        public void SetResolution(int x, int y, int dpi)
        {
            ProcessStartInfo s = new ProcessStartInfo(VBoxManagerPath);
            s.Arguments = "guestproperty set MEmu resolution_height " + y;
            Process.Start(s);
            s.Arguments = "guestproperty set MEmu resolution_width " + x;
            Process.Start(s);
            s.Arguments = "guestproperty set MEmu vbox_dpi " + dpi;
            Process.Start(s);
        }

        public void StartEmulator(string arguments)
        {
            try
            {
                ProcessStartInfo info = new ProcessStartInfo();
                info.FileName = VBoxManagerPath.Replace(@"\MEmuHyperv\MEmuManage.exe", @"\MEmu\MEmuConsole.exe");
                if (arguments != null)
                {
                    info.Arguments = arguments;
                }
                else
                {
                    info.Arguments = "MEmu";
                }
                info.RedirectStandardOutput = true;
                info.UseShellExecute = false;
                info.CreateNoWindow = true;
                Process.Start(info);
            }
            catch (SocketException)
            {

            }
            catch (Exception ex)
            {
                logger.WritePrivateLog(ex.ToString());
            }
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
            try
            {
                //Remove 4 files which detect by Unbotify
                controller.ExecuteAdbCommand("rm -f /system/bin/microvirtd");
                controller.ExecuteAdbCommand("rm -f /system/etc/init.microvirt.sh");
                controller.ExecuteAdbCommand("rm -f /system/bin/memud");
                controller.ExecuteAdbCommand("rm -f /system/lib/memuguest.ko");
            }
            catch
            {

            }
        }

        public Point GetAccurateClickPoint(Point point)
        {
            return point;
        }
    }
}
