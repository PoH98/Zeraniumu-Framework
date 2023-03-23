using System.Diagnostics;
using System.Drawing;
using System;

namespace Zeraniumu
{
    public interface IEmulator
    {
        /// <summary>
        /// The name of Emulator
        /// </summary>
        /// <returns>return as string with | if the emulator have multiple names</returns>
        string EmulatorName();
        /// <summary>
        /// The AdbIpPort to connect to the emulator. Use the format of 127.0.0.1:1000 for example
        /// </summary>
        /// <returns>return as string of ip:port format</returns>
        string AdbIpPort();
        /// <summary>
        /// The size of emulator which will used to run script
        /// </summary>
        /// <returns></returns>
        Rectangle DefaultSize();
        /// <summary>
        /// The rectangle that needs to remove the title, toolbar and etc
        /// </summary>
        /// <returns></returns>
        Rectangle ActualSize();
        /// <summary>
        /// Load the emulator settings, such as adb port, shared path and etc and check if the emulator is installed in the PC
        /// </summary>
        /// <returns><see cref="true"/> if the emulator is found</returns>
        bool CheckEmulatorExist(string arguments, ILog logger);
        /// <summary>
        /// Set the resolution of Emulator
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="dpi"></param>
        void SetResolution(int x, int y, int dpi);
        /// <summary>
        /// The function used to start the emulator
        /// </summary>
        /// <returns>the process of the emulator which is started. Be aware if you using emulator's controller. DON'T GIVE ME CONTROLLER!!</returns>
        void StartEmulator(string arguments);
        /// <summary>
        /// The function to stop emulator. Try not using Process.Kill if you can. But if can't close it then go ahead
        /// </summary>
        void StopEmulator(Process emulator, string arguments);
        /// <summary>
        /// Remove the files which will used by unbotify to detect bots. Currently will only aware on MEmu
        /// </summary>
        void UnUnbotify(EmulatorController controller);
        /// <summary>
        /// The arguments for staring emulator, used to detect if the emulator is running now
        /// </summary>
        /// <returns></returns>
        string DefaultArguments();
        /// <summary>
        /// Some emulators need extra options on adb execute
        /// </summary>
        string AdbShellOptions { get; }
        /// <summary>
        /// Get accurate click points for each emulators
        /// </summary>
        /// <param name="point"></param>
        /// <returns></returns>
        Point GetAccurateClickPoint(Point point);
        /// <summary>
        /// Set default minitouch should use which mode, keeps original not implemented exception will use UDP
        /// </summary>
        MinitouchMode MinitouchMode { get; }
        /// <summary>
        /// Used to set minitouch and screenshot images to, for better method than remounting temp folder frequently.
        /// <seealso cref="NotImplementedException"/> or <seealso cref="null"/> will use mount android temp folder
        /// </summary>
        SharedFolder GetSharedFolder { get; }

    }

    public enum MinitouchMode
    {
        UDP = 0,
        STD = 1
    }
}
