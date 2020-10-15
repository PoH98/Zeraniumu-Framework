using SharpAdbClient;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Net;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace BotFramework
{
    /// <summary>
    /// The adb system controller
    /// </summary>
    internal class AdbController
    {
        private AdbServer server = new AdbServer();
        private AdbClient client = new AdbClient();
        private List<DeviceData> devices;
        private DeviceData selectedDevice;
        private DeviceMonitor monitor;
        private string adbipport;
        private Framebuffer framebuffer;
        private ILog logger;
        /// <summary>
        /// Create new adb controller
        /// </summary>
        /// <param name="adbPath"></param>
        internal AdbController(string adbPath, ILog logger)
        {
            server.StartServer(Path.Combine(adbPath, "adb.exe"), false);
            this.logger = logger;
            monitor = new DeviceMonitor(new AdbSocket(new IPEndPoint(IPAddress.Loopback, AdbClient.AdbServerPort)));
            monitor.DeviceConnected += OnDeviceConnected;
            monitor.DeviceDisconnected += OnDeviceDisconnected;
            devices = client.GetDevices();
        }

        /// <summary>
        /// Create new adb controller and try get the device
        /// </summary>
        /// <param name="adbPath"></param>
        internal AdbController(string adbPath, string adbipsocket, ILog logger)
        {
            server.StartServer(Path.Combine(adbPath, "adb.exe"), false);
            this.logger = logger;
            monitor = new DeviceMonitor(new AdbSocket(new IPEndPoint(IPAddress.Loopback, AdbClient.AdbServerPort)));
            monitor.DeviceConnected += OnDeviceConnected;
            monitor.DeviceDisconnected += OnDeviceDisconnected;
            devices = client.GetDevices();
            SelectDevice(adbipsocket);
        }

        /// <summary>
        /// Select a device based on adb ip:port format string
        /// </summary>
        /// <param name="adbipsocket">ip:port format string</param>
        internal bool SelectDevice(string adbipsocket)
        {
            adbipport = adbipsocket;
            logger.WritePrivateLog("Finding " + adbipport);
            client.Connect(new DnsEndPoint(adbipsocket.Split(':')[0], Convert.ToInt32(adbipsocket.Split(':')[1])));
            foreach (var device in client.GetDevices())
            {
                if (device.ToString() == adbipsocket)
                {
                    selectedDevice = device;
                    Console.Out.WriteLine("SelectDevice Success");
                    return true;
                }
            }
            logger.WritePrivateLog("No such device found");
            return false;
        }

        /// <summary>
        /// Await for device startup complete
        /// </summary>
        internal async Task<bool> WaitDeviceStart()
        {
            string output;
            do
            {
                output = Execute("getprop sys.boot_completed");
                await Task.Delay(1000);
            }
            while (output == null || !output.Contains("1"));
            logger.WritePrivateLog("device wait completed");
            return true;
        }
        /// <summary>
        /// Execute adb command to selected device
        /// </summary>
        /// <param name="command"></param>
        /// <returns></returns>
        internal string Execute(string command, [CallerLineNumber] int lineNumber = 0, [CallerMemberName] string caller = null)
        {
            if (selectedDevice == null)
            {
                if (string.IsNullOrEmpty(adbipport))
                {
                    Console.Error.WriteLine("No adbipport registered. Please check you called SelectDevice(string adbipsocket)");
                    return string.Empty;
                }
                if (!SelectDevice(adbipport))
                {
                    Console.Error.WriteLine("No such device found");
                    return string.Empty;
                }
            }
            ConsoleOutputReceiver receiver = new ConsoleOutputReceiver();
            try
            {
                client.ExecuteRemoteCommand(command, selectedDevice, receiver);
            }
            catch(Exception ex)
            {
                if(ex.Message.Contains("device offline"))
                {
                    throw new Exception("device offline");
                }
                receiver.AddOutput(ex.ToString());
            }
            logger.WritePrivateLog("Execute " + command + " Result:"+ receiver.ToString(), lineNumber, caller);
            return receiver.ToString();
        }

        /// <summary>
        /// Push file from PC to emulator
        /// </summary>
        /// <param name="from">path of file on PC</param>
        /// <param name="to">path of file in android</param>
        /// <param name="permission">Permission of file</param>
        public void Push(string from, string to, int permission)
        {
            try
            {
                to = to.Replace("\\", "/");
                Execute("mount -o remount rw " + to.Remove(to.LastIndexOf('/')));
                using (SyncService service = new SyncService(new AdbSocket(new IPEndPoint(IPAddress.Loopback, AdbClient.AdbServerPort)), selectedDevice))
                {
                    using (Stream stream = File.OpenRead(from))
                    {
                        service.Push(stream, to, permission, DateTime.Now, null, CancellationToken.None);
                    }
                }
                Console.Out.WriteLine("Push Success");
            }
            catch (Exception ex)
            {
                if (ex.Message == "Device Offline")
                {
                    throw new Exception("Device Offline");
                }
            }

        }

        /// <summary>
        /// Pull file from emulator to PC
        /// </summary>
        /// <param name="from">path of file in android</param>
        /// <param name="to">path of file on PC</param>
        /// <returns></returns>
        public bool Pull(string from, string to)
        {
            try
            {
                to = to.Replace("\\", "/");
                Execute("mount -o remount rw " + to.Remove(to.LastIndexOf('/')));
                using (SyncService service = new SyncService(new AdbSocket(new IPEndPoint(IPAddress.Loopback, AdbClient.AdbServerPort)), selectedDevice))
                {
                    using (Stream stream = File.OpenRead(from))
                    {
                        service.Pull(from, stream, null, CancellationToken.None);
                    }
                }
                if (File.Exists(to))
                {
                    Console.Out.WriteLine("Pull Success");
                    return true;
                }


            }
            catch(Exception ex)
            {
                if(ex.Message == "Device Offline")
                {
                    throw new Exception("Device Offline");
                }
            }
            Console.Out.WriteLine("Pull Failed");
            return false;
        }
        /// <summary>
        /// Create foward port
        /// </summary>
        /// <param name="minitouchport"></param>
        public void CreateForward(string localport, string remoteport)
        {
            try
            {
                client.CreateForward(selectedDevice, ForwardSpec.Parse(localport), ForwardSpec.Parse(remoteport), true);
                Console.Out.WriteLine("Forward Success");
            }
            catch
            {

            }

        }

        public async Task<IImageData> Screenshot()
        {
            if(framebuffer == null)
            {
                framebuffer = new Framebuffer(selectedDevice, client);
            }
            await framebuffer.RefreshAsync(new CancellationToken());
            var image = framebuffer.ToImage();
            return new ScreenShot((Bitmap)image, logger);
        }

        #region EventHandler
        private void OnDeviceDisconnected(object sender, DeviceDataEventArgs e)
        {
            try
            {
                devices.Remove(e.Device);
                Console.Out.WriteLine("Device disconnected " + e.Device.ToString());
                if(e.Device == this.selectedDevice)
                {
                    //Restart

                }
            }
            catch
            {

            }
        }

        private void OnDeviceConnected(object sender, DeviceDataEventArgs e)
        {
            try
            {
                devices.Add(e.Device);
                Console.Out.WriteLine("Device connected " + e.Device.ToString());
            }
            catch
            {

            }
        }
        #endregion
    }
}
