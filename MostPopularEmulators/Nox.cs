using Microsoft.Win32;
using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Net.Sockets;
using System.Text.RegularExpressions;
using System.Threading;
using Zeraniumu;

namespace MostPopularEmulators
{
    public class Nox : IEmulator
    {
        private string NoxFile, VBoxManagerPath, arguments;
        private ILog logger;

        public string AdbShellOptions => null;

        public MinitouchMode MinitouchMode => throw new NotImplementedException();

        public SharedFolder GetSharedFolder => throw new NotImplementedException();

        public Rectangle ActualSize()
        {
            return new Rectangle(1,1,640, 360);
        }

        public string AdbIpPort()
        {
            ProcessStartInfo info = new ProcessStartInfo();
            info.FileName = VBoxManagerPath;
            if (arguments.Length > 0)
            {
                info.Arguments = "showvminfo " + arguments;
            }
            else
            {
                info.Arguments = "showvminfo nox";
            }
            info.RedirectStandardOutput = true;
            info.UseShellExecute = false;
            info.CreateNoWindow = true;
            var p = Process.Start(info);
            var result = p.StandardOutput.ReadToEnd();
            Regex host = new Regex(".*host ip = ([^,]+), .* guest port = 5555");
            var regexresult = host.Match(result).Value;
            string ip = "127.0.0.1", port = "62001";
            if (!string.IsNullOrEmpty(regexresult))
            {
                ip = regexresult.Substring(regexresult.IndexOf("host ip = ") + 10);
                ip = ip.Remove(ip.IndexOf("host port =") - 2);
                port = regexresult.Substring(regexresult.IndexOf("host port = ") + 12, 5);
            }
            return ip + ":" + port;//Adb Port Get
        }

        public bool CheckEmulatorExist(string arguments, ILog logger)
        {
            this.arguments = arguments;
            RegistryKey reg = null;
            if (Environment.Is64BitProcess)
            {
                reg = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64);
            }
            else
            {
                reg = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry32);
            }
            string path;
            var r = reg.OpenSubKey(@"SOFTWARE\BigNox\VirtualBox\");
            if (r != null)
            {
                try
                {
                    var result = r.GetValue("InstallDir");
                    if (result != null)
                    {
                        path = result.ToString();
                        if (Directory.Exists(path))
                        {
                            NoxFile = path;
                            VBoxManagerPath = GetRTPath() + "BigNoxVMMgr.exe";
                            return true;
                        }
                    }
                }
                catch (Exception ex)
                {
                    if(ex is FileNotFoundException)
                    {
                        return false;
                    }
                    logger.WritePrivateLog(ex.ToString());
                }
            }
            if (Environment.Is64BitProcess)
            {
                r = reg.OpenSubKey("SOFTWARE\\Wow6432Node\\DuoDianOnline\\SetupInfo\\");
                if (r != null)
                {
                    try
                    {
                        var result = r.GetValue("InstallPath");
                        if (result != null)
                        {
                            path = result.ToString();
                            if (Directory.Exists(path))
                            {
                                NoxFile = path;
                                VBoxManagerPath = GetRTPath() + "BigNoxVMMgr.exe";
                                return true;
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        if (ex is FileNotFoundException)
                        {
                            return false;
                        }
                        logger.WritePrivateLog(ex.ToString());
                    }
                }
            }
            else
            {
                r = reg.OpenSubKey("SOFTWARE\\DuoDianOnline\\SetupInfo\\");
                if (r != null)
                {
                    try
                    {
                        var result = r.GetValue("InstallPath");
                        if (result != null)
                        {
                            path = result.ToString();
                            if (Directory.Exists(path))
                            {
                                NoxFile = path + "\\bin\\Nox.exe";
                                VBoxManagerPath = GetRTPath() + "BigNoxVMMgr.exe";
                                return true;
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        if (ex is FileNotFoundException)
                        {
                            return false;
                        }
                        logger.WritePrivateLog(ex.ToString());
                    }
                }
            }
            return false;
        }
        private string GetRTPath()
        {
            var path = Environment.ExpandEnvironmentVariables("%ProgramW6432%") + "\\BigNox\\BigNoxVM\\RT\\";
            if (!Directory.Exists(path))
            {
                path = Environment.ExpandEnvironmentVariables("%ProgramFiles(x86)%") + "\\BigNox\\BigNoxVM\\RT\\";
            }
            if (!Directory.Exists(path))
            {
                path = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86) + "\\BigNox\\BigNoxVM\\RT\\";
            }
            if (!Directory.Exists(path))
            {
                RegistryKey reg = null;
                if (Environment.Is64BitProcess)
                {
                    reg = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64);
                }
                else
                {
                    reg = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry32);
                }
                RegistryKey key = reg.OpenSubKey(@"SOFTWARE\BigNox\VirtualBox\");
                var value = key.GetValue("InstallDir");
                if (value != null)
                {
                    path = value.ToString();
                }
                else
                {
                    logger.WritePrivateLog("Nox RT Path not found!");
                    throw new FileNotFoundException();
                }
            }
            return path;
        }

        public string DefaultArguments()
        {
            return "Nox";
        }

        public Rectangle DefaultSize()
        {
            return new Rectangle(0, 0, 640, 360);
        }

        public string EmulatorName()
        {
            return "Nox|夜神模拟器";
        }

        public void SetResolution(int x, int y, int dpi)
        {
            string path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData).Replace("\\Roaming", "\\Local\\"+arguments+"\\conf.ini"));
            if (File.Exists(path))
            {
                string[] lines = File.ReadAllLines(path);
                for (int a = 0; a < lines.Length; a++)
                {
                    if (lines[a].Contains("h_resolution"))
                    {
                        lines[a] = "h_resolution=" + x + "x" + y;
                    }
                    else if (lines[a].Contains("h_dpi"))
                    {
                        lines[a] = "h_dpi=" + dpi;
                    }
                }
                File.WriteAllLines(path, lines);
            }
            else
            {
                throw new FileNotFoundException("Unable to find Nox conf.ini file");
            }
        }

        public void StartEmulator(string arguments)
        {
            try
            {
                ProcessStartInfo info = new ProcessStartInfo();
                info.FileName = NoxFile + "\\bin\\Nox.exe";
                if (arguments != null)
                {
                    info.Arguments = "-clone:" + arguments;
                }
                else
                {
                    info.Arguments = "-clone:Nox_0";
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
            
        }

        public Point GetAccurateClickPoint(Point point)
        {
            return point;
        }
    }
}
