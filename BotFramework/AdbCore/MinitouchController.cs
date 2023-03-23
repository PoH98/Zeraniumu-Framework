using SharpAdbClient;
using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Zeraniumu.AdbCore
{
    internal class MinitouchController
    {
        private TcpSocket minitouchSocket;
        private int minitouchPort = 1111;
        private AdbController controller;
        private string minitouchpath;
        private IEmulator emulator;
        private Random rnd = new Random();
        private ILog logger;
        private int MinitouchMode = 0;
        private ConsoleOutputReceiver receiver = new ConsoleOutputReceiver();
        private Process MinitouchSTD;
        internal MinitouchController(AdbController controller, string minitouchpath, IEmulator emulator, ILog logger)
        {
            this.minitouchpath = minitouchpath;
            this.controller = controller;
            var path = Path.GetTempPath() + "minitouch";
            if (File.Exists(path))
            {
                var ports = File.ReadAllLines(path);
                foreach (var port in ports)
                {
                    try
                    {
                        if (minitouchPort == Convert.ToInt32(port))
                        {
                            minitouchPort++;
                        }
                        else
                        {
                            break;
                        }
                    }
                    catch
                    {

                    }
                }
            }
            using (var stream = File.AppendText(path))
            {
                stream.WriteLine(minitouchPort); //Let other instance know that this socket is in use!
            }
            this.emulator = emulator;
            this.logger = logger;
        }

        internal void Install()
        {
            string rndMiniTouch = GetSecureName(emulator.EmulatorName() + ":" + emulator.AdbIpPort() + controller.Execute("getprop ro.build.version.sdk"));
            SharedFolder useSharedFolder = null;
            try
            {
                useSharedFolder = emulator.GetSharedFolder;
            }
            catch
            {

            }
            try
            {
                if(useSharedFolder == null)
                {
                    controller.Execute("find /data/local/tmp/ -maxdepth 1 -type f -delete");
                    controller.Push(minitouchpath, "/data/local/tmp/" + rndMiniTouch, 777);
                }
                else
                {
                    File.Copy(minitouchpath, Path.Combine(useSharedFolder.PCPath, rndMiniTouch));
                }
            }
            catch(Exception ex)
            {
                if(ex.Message.Contains("Device Offline"))
                {
                    throw new Exception("Device Offline");
                }
            }
            try
            {
                if (emulator.MinitouchMode != Zeraniumu.MinitouchMode.UDP)
                {
                    MinitouchMode = 1;
                }
            }
            catch
            {

            }
            bool successConnect = false;
            do
            {
                if (MinitouchMode == 0)
                {
                    controller.CreateForward($"tcp:{minitouchPort}", "localabstract:minitouch");
                    minitouchSocket = new TcpSocket();
                    minitouchSocket.Connect(new IPEndPoint(IPAddress.Parse("127.0.0.1"), minitouchPort));
                    if (minitouchSocket.Connected)
                    {
                        try
                        {
                            string cmd = "d 0 0 0 100\nc\nu 0\nc\n";
                            byte[] bytes = AdbClient.Encoding.GetBytes(cmd);
                            minitouchSocket.Send(bytes, 0, bytes.Length, SocketFlags.None);
                            successConnect = true;
                        }
                        catch
                        {
                            continue;
                        }
                    }
                    else
                    {
                        logger.WriteLog("Minitouch unable to connect, use STDIN instead", Color.Red);
                        MinitouchMode = 1;
                    }
                }
                else
                {
                    if (useSharedFolder == null)
                    {
                        MinitouchSTD = controller.STDINMinitouch(receiver, "/data/local/tmp/" + rndMiniTouch);
                    }
                    else
                    {
                        MinitouchSTD = controller.STDINMinitouch(receiver, useSharedFolder.AndroidPath + rndMiniTouch);
                    }
                    Delay.Wait(500);
                    if (!receiver.ToString().Contains("not found") && !receiver.ToString().Contains("No such file or directory"))
                        successConnect = true;
                    else
                    {
                        logger.WriteLog("Seems serious error, STDIN mode is not working too!", Color.Red);
                        throw new InvalidProgramException("STDIN mode is not working too! Bot have to stop!");
                    }
                }
            }
            while (!successConnect);
            Console.Out.WriteLine("Minitouch install Success");
        }

        internal void Tap(Point location)
        {
            if (MinitouchSTD == null)
            {
                if (minitouchSocket == null || !minitouchSocket.Connected)
                {
                    Install();
                }
            }
            int x = (int)Math.Round((double)rnd.Next(location.X - 10, location.X + 10));
            int y = (int)Math.Round((double)rnd.Next(location.Y - 10, location.Y - 10));
            int pressure = (int)Math.Round((double)rnd.Next(50, 200));
            var p = emulator.GetAccurateClickPoint(new Point(x, y));
            string cmd = $"d 0 {p.X} {p.Y} {pressure}\nc\nu 0\nc\n";
            Send(cmd);
        }

        internal void LongTap(Point location, int interval)
        {
            if (MinitouchSTD == null)
            {
                if (minitouchSocket == null || !minitouchSocket.Connected)
                {
                    Install();
                }
            }
            int x = (int)Math.Round((double)rnd.Next(location.X - 10, location.X + 10));
            int y = (int)Math.Round((double)rnd.Next(location.Y - 10, location.Y - 10));
            int pressure = (int)Math.Round((double)rnd.Next(50, 200));
            var p = emulator.GetAccurateClickPoint(new Point(x, y));
            string cmd = $"d 0 {p.X} {p.Y} {pressure}\nc";
            Send(cmd);
            Thread.Sleep(interval);
            cmd = "u 0\nc\n";
            Send(cmd);
        }

        internal void Send(string cmd)
        {
            if (MinitouchSTD == null)
            {
                byte[] bytes = AdbClient.Encoding.GetBytes(cmd);
                try
                {
                    minitouchSocket.Send(bytes, 0, bytes.Length, SocketFlags.None);
                }
                catch
                {
                    Install();
                    Thread.Sleep(3000);
                    Send(cmd);
                }
            }
            else
            {
                foreach(var l in cmd.Split('\n'))
                {
                    MinitouchSTD.StandardInput.WriteLine(l);
                    logger.WritePrivateLog("Send minitouch command with " + l);
                }
            }
        }

        private string GetSecureName(string rawData)
        {
            using (SHA256 sha256Hash = SHA256.Create())
            {
                // ComputeHash - returns byte array  
                byte[] bytes = sha256Hash.ComputeHash(Encoding.UTF8.GetBytes(rawData));

                // Convert byte array to a string   
                StringBuilder builder = new StringBuilder();
                for (int i = 0; i < bytes.Length; i++)
                {
                    builder.Append(bytes[i].ToString("x2"));
                }
                return builder.ToString();
            }
        }
    }
}
