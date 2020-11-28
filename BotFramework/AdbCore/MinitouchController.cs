using SharpAdbClient;
using System;
using System.Drawing;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace Zeraniumu.AdbCore
{
    internal class MinitouchController
    {
        private TcpSocket minitouchSocket;
        private int minitouchPort = 1111;
        private AdbController controller;
        private string minitouchpath;
        private double Scale = 1;
        private Random rnd = new Random();
        internal MinitouchController(AdbController controller, string minitouchpath, double Scale = 1)
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
            this.Scale = Scale;
        }

        internal void Install()
        {
            string rndMiniTouch = Path.GetRandomFileName();
            try
            {
                controller.Execute("find /data/local/tmp/ -maxdepth 1 -type f -delete");
                controller.Push(minitouchpath, "/data/local/tmp/" + rndMiniTouch, 777);
            }
            catch(Exception ex)
            {
                if(ex.Message.Contains("Device Offline"))
                {
                    throw new Exception("Device Offline");
                }
            }

            bool successConnect = false;
            do
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

                    }
                }
            }
            while (!successConnect);
            Console.Out.WriteLine("Minitouch install Success");
        }

        internal void Tap(Point location)
        {
            if(minitouchSocket == null || !minitouchSocket.Connected)
            {
                Install();
            }
            int x = (int)Math.Round(rnd.Next(location.X - 10, location.X + 10) * Scale);
            int y = (int)Math.Round(rnd.Next(location.Y - 10, location.Y - 10) * Scale);
            int pressure = (int)Math.Round(rnd.Next(50, 200) * Scale);
            string cmd = $"d 0 {x} {y} {pressure}\nc\nu 0\nc\n";
            byte[] bytes = AdbClient.Encoding.GetBytes(cmd);
            try
            {
                minitouchSocket.Send(bytes, 0, bytes.Length, SocketFlags.None);
            }
            catch
            {
                Install();
                Thread.Sleep(3000);
                Tap(location);
            }

        }

        internal void LongTap(Point location, int interval)
        {
            if (minitouchSocket == null)
            {
                Install();
            }
            int x = (int)Math.Round(rnd.Next(location.X - 10, location.X + 10) * Scale);
            int y = (int)Math.Round(rnd.Next(location.Y - 10, location.Y - 10) * Scale);
            int pressure = (int)Math.Round(rnd.Next(50, 200) * Scale);
            string cmd = $"d 0 {x} {y} {pressure}\nc";
            byte[] bytes = AdbClient.Encoding.GetBytes(cmd);
            minitouchSocket.Send(bytes, 0, bytes.Length, SocketFlags.None);
            Thread.Sleep(interval);
            cmd = "u 0\nc\n";
            bytes = AdbClient.Encoding.GetBytes(cmd);
            minitouchSocket.Send(bytes, 0, bytes.Length, SocketFlags.None);
        }

        internal void Send(string command)
        {
            byte[] bytes = AdbClient.Encoding.GetBytes(command);
            minitouchSocket.Send(bytes, 0, bytes.Length, SocketFlags.None);
        }
    }
}
