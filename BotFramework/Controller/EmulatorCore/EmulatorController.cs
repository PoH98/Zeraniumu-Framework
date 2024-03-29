﻿using Zeraniumu.AdbCore;
using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Windows.Forms;
using System.IO;
using System.Reflection;
using System.Linq;
using System.Collections.Generic;
using Zeraniumu.Helper;
using System.Text.RegularExpressions;
using System.Net;
using Emgu.CV.OCR;

namespace Zeraniumu
{
    public interface IEmulatorController: IController
    {
        void StartEmulator(string arguments = null, [CallerLineNumber] int lineNumber = 0, [CallerMemberName] string caller = null);
        void RestartEmulator([CallerLineNumber] int lineNumber = 0, [CallerMemberName] string caller = null);
        void StopEmulator([CallerLineNumber] int lineNumber = 0, [CallerMemberName] string caller = null);
        void ConnectEmulator([CallerLineNumber] int lineNumber = 0, [CallerMemberName] string caller = null);
        void Tap(Point location);
        void Swipe(Point startLocation, Point endLocation, int interval);
        void LongTap(Point location, int interval);
        void StartGame(string packageName, string activityName);
        void KillGame(string packageName);
        bool GameIsForeground(string packageName);
        void SendText(string text);
        void CloseSystemBar();
        void OpenSystemBar();
        void OpenPlayStore(string packageName);
        void ResizeToPreferedSize();
    }
    /// <summary>
    /// Emulator's actions
    /// </summary>
    public class EmulatorController:IEmulatorController
    {
        private IEmulator emulator;
        private Process emulatorProcess;
        private Panel docker, transparentPanel;
        private Point defaultLocation;
        private AdbController adbController;
        private MinitouchController minitouchController;
        private bool docked;
        private string args;
        private IntPtr dockhandler;
        private ILog logger;
        private int StartExecute = 0;
        private int CaptureError = 0;
        private Random rnd = new Random();
        private Rectangle? rect = null;
        /// <summary>
        /// Resize the screenshot to make sure all captured images are the same size
        /// </summary>
        public bool ResizeScreenshot { get; set; } = true;
        /// <summary>
        /// Set the screenshot method
        /// </summary>
        public EmulatorCaptureMethod CaptureMethod { get; set; } = EmulatorCaptureMethod.WinApiCapture;
        /// <summary>
        /// Used to check if we need to kept the bot running on background. Used for multiple purpose like capturing screenshots and etc
        /// </summary>
        public bool KeepBackground { get; set; } = true;
        /// <summary>
        /// Config file settings which can be used for storing settings and etc
        /// </summary>
        public Config Config { get; set; }
        /// <summary>
        /// The path to adb.exe
        /// </summary>
        public string AdbPath { get; set; } = "adb";
        /// <summary>
        /// The path to minitouch
        /// </summary>
        public string MinitouchPath { get; set; } = "adb\\minitouch";
        /// <summary>
        /// Tesseract object, normally won't use this so just ignore it
        /// </summary>
        public object Tesseract { get; set; }

        /// <summary>
        /// Create a new instance of Emulator Controller for controlling emulator actions
        /// </summary>
        public EmulatorController(ILog logging, Panel docker = null, string arguments = null, string profileName = "Bot", [CallerLineNumber] int lineNumber = 0, [CallerMemberName] string caller = null)
        {
            
            if (!Directory.Exists("Emulators"))
            {
                Directory.CreateDirectory("Emulators");
                throw new FileNotFoundException("No emulator dll is found! Please download the emulator's dll or create one before using this script!");
            }
            bool Found = false;
            foreach(var dll in Directory.GetFiles("Emulators", "*.dll"))
            {
                try
                {
                    var assembly = Assembly.LoadFrom(dll);
                    var types = assembly.GetTypes();
                    foreach (var t in types)
                    {
                        if (t.GetInterface("IEmulator") != null)
                        {
                            var type = Activator.CreateInstance(t) as IEmulator;
                            if (type.CheckEmulatorExist(arguments, logger))
                            {
                                emulator = type;
                                Found = true;
                                break;
                            }
                        }
                    }
                    if (Found)
                    {
                        break;
                    }
                }
                catch
                {

                }
            }
            Config = Config.GetInstance(profileName);
            Config.ReadConfig();
            logging.SetLogPath(Path.Combine(Config.ConfigPath, "Log") + "\\");
            if(emulator == null || !Found)
            {
                //No installed emulator found
                throw new Exception("No installed emulator found!");
            }
            this.docker = docker;
            logger = logging;
            logger.WritePrivateLog("Emulator Controller created", lineNumber, caller);
            if(ComputerHelper.getScalingFactor() != 1)
            {
                logger.WriteLog("Please set your scaling factor to 100% before using this program to ensure everything works well!", Color.Red);
            }
        }

        /// <summary>
        /// Start the emulator, if emulator is already started then do nothing
        /// </summary>
        public void StartEmulator(string arguments = null, [CallerLineNumber] int lineNumber = 0, [CallerMemberName] string caller = null)
        {
            int retryCount = 0;
            try
            {
                if (emulatorProcess != null)
                    if (!emulatorProcess.HasExited)
                    {
                        logger.WriteLog("Emulator already exist", Color.AliceBlue, lineNumber, caller);
                        return;
                    }
            }
            catch
            {

            }
            StartExecute++;
            if(StartExecute > 2)
            {
                StopEmulator();
                StartExecute = 0;
            }
            args = arguments;
            Loop:
            retryCount++;
            var processlist = new List<Process>();
            try
            {
                if(retryCount > 5)
                {
                    throw new Exception("Unable to fetch process");
                }
                foreach (var proc in Process.GetProcesses())
                {
                    try
                    {
                        if (emulator.EmulatorName().Split('|').Any(x => x == proc.ProcessName || x == proc.MainWindowTitle))
                        {
                            processlist.Add(proc);
                        }
                    }
                    catch
                    {
                        //Some process might conduct null process name
                    }
                }
                if(processlist.Count < 1)
                {
                    if (retryCount > 1)
                    {
                        StopEmulator();
                    }
                }
            }
            catch
            {
                //Some times the antivirus will block us for getting processes
                logger.WriteLog("Warning! Unable to fetch processes as blocked by antivirus! The process might unable to get emulator binded! Please to it manually!", Color.Red);
                SelectProcess select = new SelectProcess();
                if(select.ShowDialog() == DialogResult.OK)
                {
                    emulatorProcess = Process.GetProcessById(select.id);
                    logger.WriteLog("Emulator started", Color.Lime);
                    return;
                }
                else
                {
                    goto Loop;
                }
            }
            if (processlist.Count() > 0)
            {
                if(arguments == null)
                {
                    //Use default emulator arguments or just ignore it
                    foreach(var proc in processlist)
                    {
                        var cmd = Imports.GetCommandLineOfProcess(proc);
                        if (string.IsNullOrEmpty(cmd))
                        {
                            emulatorProcess = proc;
                            logger.WriteLog("Emulator started", Color.Lime);
                            return;
                        }
                        else
                        {
                            if(cmd.EndsWith(emulator.DefaultArguments()))
                            {
                                emulatorProcess = proc;
                                logger.WriteLog("Emulator started", Color.Lime);
                                return;
                            }
                        }
                    }
                }
                else
                {
                    foreach (var proc in processlist)
                    {
                        var cmd = Imports.GetCommandLineOfProcess(proc);
                        if(cmd.EndsWith(arguments))
                        {
                            emulatorProcess = proc;
                            logger.WriteLog("Emulator started", Color.Lime);
                            return;
                        }
                    }
                }
            }
            logger.WriteLog("Starting Emulator", Color.Lime);
            emulator.StartEmulator(arguments);
            Thread.Sleep(5000);
            goto Loop;
        }
        /// <summary>
        /// Restart the emulator. If emulator not running then start it and skip closing it
        /// </summary>
        public void RestartEmulator([CallerLineNumber] int lineNumber = 0, [CallerMemberName] string caller = null)
        {
            try
            {
                if (emulatorProcess != null)
                    if (!emulatorProcess.HasExited)
                    {
                        StopEmulator(lineNumber, caller);
                    }
            }
            catch
            {

            }
            StartEmulator(args, lineNumber, caller);
        }
        /// <summary>
        /// Stopping the emulator
        /// </summary>
        public void StopEmulator([CallerLineNumber] int lineNumber = 0, [CallerMemberName] string caller = null)
        {
            logger.WriteLog("Closing emulator", Color.Red, lineNumber, caller);
            emulator.StopEmulator(emulatorProcess, args);
            Thread.Sleep(500);
            try
            {
                emulatorProcess.CloseMainWindow();
                Thread.Sleep(1000);
                emulatorProcess.Kill();
            }
            catch
            {

            }
            Thread.Sleep(5000);
            emulatorProcess = null;
        }

        private IntPtr getCurrentAndroidHWnd()
        {
            if(emulatorProcess == null)
            {
                StartEmulator();
                ConnectEmulator();
            }
            if (!docked)
            {
                return emulatorProcess.MainWindowHandle;
            }
            else
            {
                Rectangle rect = new Rectangle();
                var handle = emulatorProcess.MainWindowHandle;
                if (rect.X != -1 || rect.Y != -30)
                {
                    Imports.MoveWindow(handle, -1, -30, emulator.DefaultSize().Width, emulator.DefaultSize().Height, false);
                }
                return emulatorProcess.MainWindowHandle;
            }
        }
        /// <summary>
        /// Dock the emulator into a <see cref="Panel"/> for screenshots and many more
        /// </summary>
        public void Dock([CallerLineNumber] int lineNumber = 0, [CallerMemberName] string caller = null)
        {
            docker.Invoke((MethodInvoker)delegate { docker.Visible = true; dockhandler = docker.Handle; });
            var handle = getCurrentAndroidHWnd();
            if(handle == dockhandler) //Already docked
            {
                return;
            }
            Thread.Sleep(3000); //wait 3 Seconds for window to get accessible
            Rectangle rect = new Rectangle();
            Imports.GetWindowRect(handle, ref rect);
            defaultLocation = rect.Location;
            Control parent = null;
            docker.Invoke((MethodInvoker)delegate
            {
                parent = docker.Parent;
                Imports.SetParent(handle, docker.Handle);
            });
            if(parent != null)
            {
                transparentPanel = new TransparentPanel();
                parent.Invoke((MethodInvoker)delegate
                {
                    transparentPanel.Size = docker.Size;
                    transparentPanel.Location = docker.Location;
                    parent.Controls.Add(transparentPanel);
                    transparentPanel.BringToFront();
                });
            }
            if (rect.X != -1 || rect.Y != -30)
            {
                Imports.MoveWindow(handle, -1, -30, emulator.DefaultSize().Width, emulator.DefaultSize().Height, false);
            }
            docked = true;
            logger.WritePrivateLog("Docking emulator success with ptr " + handle.ToInt64(), lineNumber, caller);
        }
        /// <summary>
        /// Undock the emulator from <see cref="Panel"/>.
        /// </summary>
        public void UnDock([CallerLineNumber] int lineNumber = 0, [CallerMemberName] string caller = null)
        {
            if (!docked)
            {
                return;
            }
            docker.Invoke((MethodInvoker)delegate { docker.Visible = false; });
            Imports.SetParent(emulatorProcess.MainWindowHandle, IntPtr.Zero);
            Imports.MoveWindow(emulatorProcess.MainWindowHandle, defaultLocation.X, defaultLocation.Y, emulator.DefaultSize().Width, emulator.DefaultSize().Height, true);
            Control parent = null;
            docker.Invoke((MethodInvoker)delegate
            {
                parent = docker.Parent;
            });
            parent.Invoke((MethodInvoker)delegate
            {
                parent.Controls.Remove(transparentPanel);
            });
            transparentPanel.Dispose();
            docked = false;
            logger.WritePrivateLog("Undocking emulator success", lineNumber, caller);
        }
        /// <summary>
        /// Screenshot the emulator screen using PrintWindow. Careful here as we might get blank image if exception found
        /// </summary>
        /// <returns></returns>
        public virtual IImageData Screenshot(bool Bgr_Rgb = true, [CallerLineNumber] int lineNumber = 0, [CallerMemberName] string caller = null)
        {
            Stopwatch s = Stopwatch.StartNew();
            Retry:
            try
            {
                if (!KeepBackground)
                {
                    CaptureMethod = EmulatorCaptureMethod.ForegroundCapture;
                }
                if (CaptureError >= 5)
                {
                    if (KeepBackground)
                    {
                        KeepBackground = !KeepBackground;
                    }
                    CaptureMethod = EmulatorCaptureMethod.AdbCapture;
                }
                IImageData result;
                switch (CaptureMethod)
                {
                    case EmulatorCaptureMethod.AdbCapture:
                        result = adbController.Screenshot().Result;
                        break;
                    case EmulatorCaptureMethod.ForegroundCapture:
                        result = ForegroundCapture(lineNumber, caller);
                        break;
                    case EmulatorCaptureMethod.WinApiCapture:
                        result = WinApiCapture(lineNumber, caller);
                        break;
                    default:
                        throw new ArgumentException("How the hell can you input something like this to me??");
                }
                CaptureError = 0;
                if (ResizeScreenshot)
                {
                    if (rect.HasValue)
                    {
                        return result.Crop(rect.Value);
                    }
                    else
                    {
                        return result.Crop(emulator.ActualSize());
                    }
                }
                else
                {
                    return result;
                }
            }
            catch(Exception ex)
            {
                if(ex is InvalidDataException)
                {
                    goto Retry;
                }
                Bitmap bmp;
                if (rect.HasValue)
                {
                    bmp = new Bitmap(emulator.DefaultSize().Width, emulator.DefaultSize().Height);
                }
                else
                {
                    bmp = new Bitmap(emulator.DefaultSize().Width, emulator.DefaultSize().Height);
                }
                //            logger.WritePrivateLog("Screenshot saved to memory used " + s.ElapsedMilliseconds + " ms", lineNumber, caller);
                logger.WritePrivateLog("Screenshot found exception: " + ex.Message  + " at line "+ new StackTrace(ex, true).GetFrame(0).GetFileLineNumber() + " used " + s.ElapsedMilliseconds + " ms", lineNumber, caller);
                return new ScreenShot(bmp, logger, false);
            }
        }

        private IImageData ForegroundCapture(int lineNumber, string caller)
        {
            var handle = getCurrentAndroidHWnd();
            Rectangle rc = new Rectangle();
            Imports.GetWindowRect(handle, ref rc);
            var bmp = new Bitmap(rc.Width, rc.Height, PixelFormat.Format32bppArgb);
            using (Graphics graphics = Graphics.FromImage(bmp))
            {
                Imports.SetForegroundWindow(handle);
                graphics.CopyFromScreen(rc.X, rc.Y, 0, 0, new Size(rc.Width, rc.Height), CopyPixelOperation.SourceCopy);
            }
            if (bmp.AllBlack())
            {
                CaptureError++;
                bmp.Dispose();
                logger.WritePrivateLog("Screenshot get black image, retrying now!", lineNumber, caller);
                throw new InvalidDataException();
            }
            return new ScreenShot(bmp, logger, false);
        }

        private IImageData WinApiCapture(int lineNumber, string caller)
        {
            var handle = getCurrentAndroidHWnd();
            Rectangle rc = new Rectangle();
            Imports.GetWindowRect(handle, ref rc);
            Bitmap bmp = new Bitmap(rc.Width, rc.Height, PixelFormat.Format32bppArgb);
            Imports.RedrawWindow(handle, rc, IntPtr.Zero, 0x85);
            Imports.UpdateWindow(handle);
            Graphics gfxBmp = Graphics.FromImage(bmp);
            IntPtr hdcBitmap = gfxBmp.GetHdc();
            Imports.PrintWindow(handle, hdcBitmap, 0);
            gfxBmp.ReleaseHdc(hdcBitmap);
            gfxBmp.Dispose();
            if (bmp.AllBlack())
            {
                CaptureError++;
                bmp.Dispose();
                logger.WritePrivateLog("Screenshot get black image, retrying now!", lineNumber, caller);
                throw new InvalidDataException();
            }
            return new ScreenShot(bmp, logger, false);
        }
        
        /// <summary>
        /// Attach connection to emulator
        /// </summary>
        /// <param name="lineNumber"></param>
        /// <param name="caller"></param>
        public void ConnectEmulator([CallerLineNumber] int lineNumber = 0, [CallerMemberName] string caller = null)
        {
            var errorCount = 0;
            Loop:
            try
            {
                adbController = new AdbController(AdbPath, logger);
                adbController.SelectDevice(emulator.AdbIpPort());
                adbController.SetShellOptions(emulator.AdbShellOptions);
                adbController.WaitDeviceStart();
                minitouchController = new MinitouchController(adbController, MinitouchPath, emulator, logger);
                minitouchController.Install();
                adbController.Execute("input keyevent 3");
                Thread.Sleep(3000);
                adbController.Execute("settings put system font_scale 1.0");
                emulator.UnUnbotify(this);
                logger.WriteLog("Emulator Connection Success!", Color.Lime);
            }
            catch(Exception ex)
            {
                errorCount++;
                if(errorCount > 15)
                {
                    logger.WriteLog(ex.Message, Color.Red);
                    StopEmulator();
                    Thread.Sleep(3000);
                    errorCount = 0;
                }
                StartEmulator();
                Thread.Sleep(1000);
                goto Loop;
            }

        }
        /// <summary>
        /// Send tap to emulator
        /// </summary>
        /// <param name="location"></param>
        public void Tap(Point location)
        {
            if(minitouchController == null)
            {
                //no connect is executed, we connect ourselves
                ConnectEmulator();
            }
            minitouchController.Tap(location);
        }
        /// <summary>
        /// Send swipe to emulator
        /// </summary>
        /// <param name="startLocation"></param>
        /// <param name="endLocation"></param>
        public void Swipe(Point startLocation, Point endLocation, int interval)
        {
            if (adbController == null)
            {
                //no connect is executed, we connect ourselves
                ConnectEmulator();
            }
            int sx = startLocation.X;
            int sy = startLocation.Y;
            int x = sx;
            int y = sy;
            int ex = endLocation.X;
            int ey = endLocation.Y;
            var steps = (emulator.DefaultSize().Width * 10) / emulator.DefaultSize().Width;
            int loops = (Math.Max(Math.Abs(ex - x), Math.Abs(ey - y)) / steps) + 1;
            int x_steps = Math.Abs((ex - x) / loops);
            int y_steps = Math.Abs((ey - y) / loops);
            int sleepMove = Math.Abs(interval / loops);
            int sleepStart = sleepMove * 2;
            int sleepEnd = interval;
            int sleep = sleepStart;
            int botSleep = 0;
            minitouchController.Send("d 0 " + rnd.Next(x - 5, x + 5) + " " + rnd.Next(y - 5, y + 5) + " " + rnd.Next(50, 200) + "\nc\nw " + sleep + "\n");
            sleep += sleepMove;
            for(int i = 1; i < loops; i++)
            {
                x += x_steps;
                y += y_steps;
                if((ex >= sx && x >= ex) || (ex < sx) && (x <= ex))
                {
                    x = ex;
                }
                if((ey >= sy && y >= ey) || (ey < sy && y <= ey))
                {
                    y = ey;
                }
                if(x == ex && y == ey)
                {
                    i = loops;
                    sleep = sleepEnd;
                }
                minitouchController.Send("m 0 " + rnd.Next(x - 5, x + 5) + " " + rnd.Next(y - 5, y + 5) + " " + rnd.Next(50, 200) + "\nc\nw " + sleep + "\n");
                botSleep += sleep;
            }
            sleep = sleepMove;
            minitouchController.Send("u 0\nc\nw " + sleep + "\n");
            botSleep += sleep;
            Thread.Sleep(botSleep);
        }
        /// <summary>
        /// Send a long tap to emulator
        /// </summary>
        /// <param name="location"></param>
        /// <param name="interval"></param>
        public void LongTap(Point location, int interval)
        {
            if (minitouchController == null)
            {
                //no connect is executed, we connect ourselves
                ConnectEmulator();
            }
            minitouchController.LongTap(location, interval);
        }
        /// <summary>
        /// Start the game according to package name and activity name
        /// </summary>
        /// <param name="packageName"></param>
        /// <param name="activityName"></param>
        public void StartGame(string packageName, string activityName)
        {
            adbController.Execute("am start -n " + packageName + "/" + activityName);
            Thread.Sleep(3000);
        }
        /// <summary>
        /// Check the game is foreground?
        /// </summary>
        /// <param name="packageName"></param>
        /// <returns></returns>
        public bool GameIsForeground(string packageName)
        {
            try
            {
                var result = adbController.Execute("dumpsys window windows | grep -E 'mCurrentFocus'");
                if (result.ToLower().Contains(packageName.ToLower()) && !result.ToLower().Contains("error"))
                {
                    return true;
                }
                else if (result.ToLower().Contains("error"))
                {
                    adbController.Execute("am force-stop " + result);
                }
            }
            catch
            {

            }
            return false;
        }
        /// <summary>
        /// Kill the game
        /// </summary>
        /// <param name="packageName"></param>
        public void KillGame(string packageName)
        {
            adbController.Execute("input keyevent KEYCODE_HOME");
            adbController.Execute("am force-stop " + packageName);
            Thread.Sleep(3000);
        }
        /// <summary>
        /// Execute Adb Command
        /// </summary>
        /// <param name="command"></param>
        public void ExecuteAdbCommand(string command)
        {
            adbController.Execute(command);
        }
        /// <summary>
        /// Send a text or sentance to emulator
        /// </summary>
        /// <param name="text"></param>
        public void SendText(string text)
        {
            var words = text.Split(' ');
            foreach(var word in words)
            {
                var newWord = Regex.Escape(word);
                adbController.Execute("input text " + newWord);
                Thread.Sleep(rnd.Next(800, 2000));
            }
        }
        /// <summary>
        /// Close the system bar of android
        /// </summary>
        public void CloseSystemBar()
        {
            adbController.Execute("service call activity 42 s16 com.android.systemui");
        }
        /// <summary>
        /// Start the system bar of android
        /// </summary>
        public void OpenSystemBar()
        {
            adbController.Execute("setprop ctl.restart zygote");
            adbController.Execute("am startservice -n com.android.systemui/.SystemUIService");
        }
        /// <summary>
        /// Open Google Play Store and show the specific application download page
        /// </summary>
        /// <param name="packageName"></param>
        public void OpenPlayStore(string packageName)
        {
            adbController.Execute("am start -a android.intent.action.VIEW -d 'market://details?id=" + packageName + "'");
        }
        /// <summary>
        /// Prepare and download missing tesseract ocr traineddata. More info about language: https://github.com/tesseract-ocr/tessdata
        /// </summary>
        /// <param name="language"></param>
        /// <param name="lineNumber"></param>
        /// <param name="caller"></param>
        public virtual async void PrepairOCR(string language, string whiteList = null, [CallerLineNumber] int lineNumber = 0, [CallerMemberName] string caller = null)
        {
            if (!Directory.Exists("tessdata"))
            {
                Directory.CreateDirectory("tessdata");
            }
            if (!File.Exists("tessdata\\" + language + ".traineddata"))
            {
                WebClient wc = new WebClient();
                wc.DownloadProgressChanged += Wc_DownloadProgressChanged;
                await wc.DownloadFileTaskAsync(new Uri("https://github.com/tesseract-ocr/tessdata/raw/main/"+language+".traineddata"), "tessdata\\" + language + ".traineddata");
                wc.Dispose();
            }
            Tesseract = new Tesseract("tessdata", language, OcrEngineMode.TesseractLstmCombined, whiteList);
        }

        private void Wc_DownloadProgressChanged(object sender, DownloadProgressChangedEventArgs e)
        {
            if (e.TotalBytesToReceive == -1)
            {
                logger.WriteLog("Download in Progress", Color.Cyan);
            }
            else
            {
                logger.WriteLog("Downloading...(" + e.ProgressPercentage + ")", Color.Cyan);
            }
        }
        /// <summary>
        /// Resize the emulator to prefered size set in Emulator's dll
        /// </summary>
        /// <param name="rect"></param>
        public void ResizeToPreferedSize()
        {
            Rectangle rec = Rectangle.Empty;
            Imports.GetWindowRect(emulatorProcess.Handle, ref rec);
            if (emulator.ActualSize().Width != rec.Width && emulator.ActualSize().Height != rec.Height)
            {
                StopEmulator();
                logger.WriteLog("Resolution not supported, resizing...", Color.Red);
                emulator.SetResolution(emulator.ActualSize().Width, emulator.ActualSize().Height, 190);
                StartEmulator();
                ConnectEmulator();
            }
        }
        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        /// <param name="rect"></param>
        public void SetImageRect(Rectangle rect)
        {
            this.rect = rect;
            logger.WritePrivateLog("Overrided capture rect as " + this.rect.Value.X + " " + this.rect.Value.Y + " " + this.rect.Value.Width + " " + this.rect.Value.Height);
        }
    }
    public enum EmulatorCaptureMethod
    {
        AdbCapture,
        WinApiCapture,
        ForegroundCapture
    }
}
