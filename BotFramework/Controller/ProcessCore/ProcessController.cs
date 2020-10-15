using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Windows.Forms;
using BotFramework.MouseKeyboardCore;
using Emgu.CV.OCR;

namespace BotFramework
{
    public interface IProcessController:IDisposable, IController
    {
        void StartProcess([CallerLineNumber] int lineNumber = 0, [CallerMemberName] string caller = null);
        void KillProcess([CallerLineNumber] int lineNumber = 0, [CallerMemberName] string caller = null);
        bool ProcessAlive([CallerLineNumber] int lineNumber = 0, [CallerMemberName] string caller = null);
        IntPtr GetIntPtr();
        void LeftClick(Point location, [CallerLineNumber] int lineNumber = 0, [CallerMemberName] string caller = null);
        void RightClick(Point location, [CallerLineNumber] int lineNumber = 0, [CallerMemberName] string caller = null);
        void DoubleClick(Point location, [CallerLineNumber] int lineNumber = 0, [CallerMemberName] string caller = null);
        void MoveMouse(Point location, [CallerLineNumber] int lineNumber = 0, [CallerMemberName] string caller = null);
        void HoldLeft();
        void ReleaseLeft();
        void HoldRight();
        void ReleaseRight();
        void KeyboardPress(VirtualKeyCode code);
        void KeyboardRelease(VirtualKeyCode code);
        void KeyboardType(string text);
        void BlockInput();
    }
    public class ProcessController : IProcessController
    {
        private ILog logger;
        private string processPath;
        private string processName;
        private Process proc;
        private string arguments;
        private InputSimulator inputSimulator;
        private Random rnd = new Random();
        /// <summary>
        /// Tesseract object
        /// </summary>
        public object Tesseract { get; set; }
        /// <summary>
        /// Scale the click location
        /// </summary>
        public double ClickScale { get; set; } = 1;
        /// <summary>
        /// Definee a new ProcessController
        /// </summary>
        /// <param name="logger"></param>
        /// <param name="processPath"></param>
        /// <param name="processName"></param>
        /// <param name="arguments"></param>
        public ProcessController(ILog logger, string processPath, string processName, string arguments = null)
        {
            this.logger = logger;
            this.processName = processName;
            this.processPath = processPath;
            this.arguments = arguments;
            inputSimulator = new InputSimulator();
        }
        /// <summary>
        /// Get screenshot image
        /// </summary>
        /// <param name="lineNumber"></param>
        /// <param name="caller"></param>
        /// <returns></returns>
        public IImageData Screenshot(bool Bgr_Rgb = true, [CallerLineNumber] int lineNumber = 0, [CallerMemberName] string caller = null)
        {
            Stopwatch s = Stopwatch.StartNew();
            var handle = GetIntPtr();
            Rectangle rc = new Rectangle();
            Imports.GetWindowRect(handle, ref rc);
            Bitmap bmp = new Bitmap(rc.Width, rc.Height, PixelFormat.Format32bppArgb);
            Graphics gfxBmp = Graphics.FromImage(bmp);
            IntPtr hdcBitmap = gfxBmp.GetHdc();
            Imports.PrintWindow(handle, hdcBitmap, 0);
            gfxBmp.ReleaseHdc(hdcBitmap);
            gfxBmp.Dispose();
            logger.WritePrivateLog("Screenshot saved to memory used " + s.ElapsedMilliseconds + " ms", lineNumber, caller);
            s.Stop();
            return new ScreenShot(bmp, logger, Bgr_Rgb);
        }
        /// <summary>
        /// Get IntPtr of the process
        /// </summary>
        /// <returns></returns>
        public IntPtr GetIntPtr()
        {
            if(proc == null)
            {
                StartProcess();
            }
            return proc.MainWindowHandle;
        }
        /// <summary>
        /// Kill the process
        /// </summary>
        /// <param name="lineNumber"></param>
        /// <param name="caller"></param>
        public void KillProcess([CallerLineNumber] int lineNumber = 0, [CallerMemberName] string caller = null)
        {
            if(proc == null)
            {
                return;
            }
            proc.Kill();
            proc = null;
        }
        /// <summary>
        /// Check process is alive
        /// </summary>
        /// <param name="lineNumber"></param>
        /// <param name="caller"></param>
        /// <returns></returns>
        public bool ProcessAlive([CallerLineNumber] int lineNumber = 0, [CallerMemberName] string caller = null)
        {
            if(proc == null)
            {
                return false;
            }
            return !proc.HasExited && proc.Responding;
        }
        /// <summary>
        /// Start process
        /// </summary>
        /// <param name="lineNumber"></param>
        /// <param name="caller"></param>
        public void StartProcess([CallerLineNumber] int lineNumber = 0, [CallerMemberName] string caller = null)
        {
            logger.WriteLog("Starting process...");
            int error = 0;
            do
            {
                var proclist = Process.GetProcessesByName(processName);
                if (proclist.Count() > 0)
                {
                    proc = proclist.First();
                    break;
                }
                else
                {
                    if (!File.Exists(processPath))
                    {
                        throw new FileNotFoundException("Process not exist, the script unable to continue execute!");
                    }
                    if (!string.IsNullOrEmpty(arguments))
                    {
                        proc = Process.Start(processPath, arguments);
                    }
                    else
                    {
                        proc = Process.Start(processPath);
                    }
                    Thread.Sleep(3000);//Wait for process to startup
                    if (ProcessAlive())
                    {
                        logger.WriteLog($"Process {processName}(id:{proc.Id}) is started successfully");
                        break;
                    }
                    else
                    {
                        logger.WriteLog($"Process {processName}(id:{proc.Id}) looks like unable to start! Retrying in 3 seconds!");
                        Thread.Sleep(3000);
                        error++;
                    }
                }
            }
            while (error < 10);
            if(error >= 10)
            {
                throw new Exception("Process refused to start! Please check your settings!");
            }
        }
        /// <summary>
        /// Send left click
        /// </summary>
        /// <param name="location"></param>
        public void LeftClick(Point location, [CallerLineNumber] int lineNumber = 0, [CallerMemberName] string caller = null)
        {
            MoveMouse(location, lineNumber, caller);
            HoldLeft();
            ReleaseLeft();
        }
        /// <summary>
        /// Send right click
        /// </summary>
        /// <param name="location"></param>
        public void RightClick(Point location, [CallerLineNumber] int lineNumber = 0, [CallerMemberName] string caller = null)
        {
            MoveMouse(location, lineNumber, caller);
            HoldRight();
            ReleaseRight();
        }
        /// <summary>
        /// Send double click
        /// </summary>
        /// <param name="location"></param>
        public void DoubleClick(Point location, [CallerLineNumber] int lineNumber = 0, [CallerMemberName] string caller = null)
        {
            MoveMouse(location, lineNumber, caller);
            HoldLeft();
            ReleaseLeft();
            HoldLeft();
            ReleaseLeft();
        }
        /// <summary>
        /// Move the mouse to location
        /// </summary>
        /// <param name="location"></param>
        public void MoveMouse(Point location, [CallerLineNumber] int lineNumber = 0, [CallerMemberName] string caller = null)
        {
            //max number is 65535, so we should calculate here
            var rect = Screen.PrimaryScreen.Bounds;
            var xscale = (double)(65535 / rect.Width)+1;
            var yscale = (double)(65535 / rect.Height)+1;
            int x = (int)(rnd.Next(location.X - 10, location.X + 10) * xscale * ClickScale);
            int y = (int)(rnd.Next(location.Y - 10, location.Y + 10) * yscale * ClickScale);
            inputSimulator.Mouse.MoveMouseTo(x, y);
            logger.WritePrivateLog("Mouse moved to location "+ location.X +":"+ location.Y, lineNumber, caller);
            Imports.BringToFront(proc);
        }
        /// <summary>
        /// Hold left button
        /// </summary>
        public void HoldLeft()
        {
            inputSimulator.Mouse.LeftButtonDown();
        }
        /// <summary>
        /// Release left button
        /// </summary>
        public void ReleaseLeft()
        {
            inputSimulator.Mouse.LeftButtonUp();
        }
        /// <summary>
        /// Hold right button
        /// </summary>
        public void HoldRight()
        {
            inputSimulator.Mouse.RightButtonDown();
        }
        /// <summary>
        /// Release right button
        /// </summary>
        public void ReleaseRight()
        {
            inputSimulator.Mouse.RightButtonUp();
        }
        /// <summary>
        /// Keyboard hold pressing the button
        /// </summary>
        /// <param name="code"></param>
        public void KeyboardPress(VirtualKeyCode code)
        {
            inputSimulator.Keyboard.KeyDown(code);
        }
        /// <summary>
        /// Keyboard release pressing specific button
        /// </summary>
        /// <param name="code"></param>
        public void KeyboardRelease(VirtualKeyCode code)
        {
            inputSimulator.Keyboard.KeyUp(code);
        }
        /// <summary>
        /// Type text
        /// </summary>
        /// <param name="text"></param>
        public void KeyboardType(string text)
        {
            inputSimulator.Keyboard.TextEntry(text);
        }
        /// <summary>
        /// Unlock mouse and keyboard
        /// </summary>
        public void Dispose()
        {
            Imports.BlockInput(false);
        }
        /// <summary>
        /// Lock mouse and keyboard
        /// </summary>
        public void BlockInput()
        {
            Imports.BlockInput(true);
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
                await wc.DownloadFileTaskAsync(new Uri("https://github.com/tesseract-ocr/tessdata/raw/master/" + language + ".traineddata"), "tessdata\\" + language + ".traineddata");
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
    }
}
