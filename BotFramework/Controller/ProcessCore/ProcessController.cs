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
using Zeraniumu.MouseKeyboardCore;
using Emgu.CV.OCR;
using Zeraniumu.Helper;
using System.ComponentModel;
using System.Collections.Generic;

namespace Zeraniumu
{
    public interface IProcessController:IDisposable, IController
    {
        void StartProcess([CallerLineNumber] int lineNumber = 0, [CallerMemberName] string caller = null);
        void KillProcess([CallerLineNumber] int lineNumber = 0, [CallerMemberName] string caller = null);
        bool ProcessAlive([CallerLineNumber] int lineNumber = 0, [CallerMemberName] string caller = null);
        IntPtr GetIntPtr();
        IntPtr GetIntPtr(string className, string windowTitle, IntPtr parent);
        IEnumerable<IntPtr> GetChildrenPtrs(IntPtr parent);
        bool SetIntPtr(IntPtr hWnd, [CallerLineNumber] int lineNumber = 0, [CallerMemberName] string caller = null);
        void LeftClick(Point location, [CallerLineNumber] int lineNumber = 0, [CallerMemberName] string caller = null);
        void RightClick(Point location, [CallerLineNumber] int lineNumber = 0, [CallerMemberName] string caller = null);
        void RightDoubleClick(Point location, [CallerLineNumber] int lineNumber = 0, [CallerMemberName] string caller = null);
        void LeftDoubleClick(Point location, [CallerLineNumber] int lineNumber = 0, [CallerMemberName] string caller = null);
        void MoveMouse(Point location, [CallerLineNumber] int lineNumber = 0, [CallerMemberName] string caller = null);
        void LeftClick(int x, int y, [CallerLineNumber] int lineNumber = 0, [CallerMemberName] string caller = null);
        void RightClick(int x, int y, [CallerLineNumber] int lineNumber = 0, [CallerMemberName] string caller = null);
        void RightDoubleClick(int x, int y, [CallerLineNumber] int lineNumber = 0, [CallerMemberName] string caller = null);
        void LeftDoubleClick(int x, int y, [CallerLineNumber] int lineNumber = 0, [CallerMemberName] string caller = null);
        void MoveMouse(int x, int y, [CallerLineNumber] int lineNumber = 0, [CallerMemberName] string caller = null);
        void HoldLeft();
        void ReleaseLeft();
        void HoldRight();
        void ReleaseRight();
        void KeyboardPress(VirtualKeyCode code);
        void KeyboardRelease(VirtualKeyCode code);
        void KeyboardType(string text);
        void BlockInput();
        Point GetCursorPosition();
        IntPtr GetWindowFromPoint(int x, int y);
        IntPtr GetWindowFromPoint(Point point);
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
        private Rectangle? rect = null;
        private IntPtr Screencaptureptr = IntPtr.Zero;
        private IntPtr ClickPtr = IntPtr.Zero;
        /// <summary>
        /// Tesseract object
        /// </summary>
        public object Tesseract { get; set; }
        /// <summary>
        /// Scale the click location
        /// </summary>
        public double ClickScale { get; set; } = 1;
        /// <summary>
        /// The screenshot method. Switch this if your screenshot not works
        /// </summary>
        public CaptureMethod CaptureMethod { get; set; } = CaptureMethod.GDIPlus;
        /// <summary>
        /// The clicking method. Switch this if your app isn't be clicked
        /// </summary>
        public ClickMethod ClickMethod { get; set; } = ClickMethod.RealMouseMove;
        /// <summary>
        /// Define a new ProcessController
        /// </summary>
        /// <param name="logger"></param>
        /// <param name="processPath"></param>
        /// <param name="processName"></param>
        /// <param name="arguments"></param>
        public ProcessController(ILog logger, string processPath, string processName, string arguments = null)
        {
            this.logger = logger;
            this.logger.SetLogPath("Log\\");
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
            for (int x = 0; x < 5; x++)
            {
                if(x > 3)
                {
                    //Try use DirectX
                    CaptureMethod = CaptureMethod.GraphicCopy;
                }
                if (CaptureMethod == CaptureMethod.HDCCapture)
                {
                    Graphics gfxBmp = Graphics.FromImage(bmp);
                    IntPtr hdcBitmap = gfxBmp.GetHdc();
                    Imports.PrintWindow(handle, hdcBitmap, 0);
                    gfxBmp.ReleaseHdc(hdcBitmap);
                    gfxBmp.Dispose();
                }
                else if(CaptureMethod == CaptureMethod.GDIPlus)
                {
                    IntPtr desktopDc;
                    IntPtr memoryDc;
                    IntPtr bitmap;
                    IntPtr oldBitmap;
                    Imports.SetForegroundWindow(handle);
                    Imports.GetForegroundWindow();
                    Screencaptureptr = Imports.GetDesktopWindow();
                    desktopDc = Imports.GetWindowDC(Screencaptureptr);
                    memoryDc = Imports.CreateCompatibleDC(desktopDc);
                    bitmap = Imports.CreateCompatibleBitmap(desktopDc, rc.Width, rc.Height);
                    oldBitmap = Imports.SelectObject(memoryDc, bitmap);
                    if (Imports.BitBlt(memoryDc, 0, 0, rc.Width, rc.Height, desktopDc, rc.Left, rc.Top, 0x00CC0020 | 0x40000000))
                    {
                        bmp = Image.FromHbitmap(bitmap);
                    }
                    else
                    {
                        logger.WritePrivateLog("Screenshot failed on GDI+!", lineNumber, caller);
                        throw new Win32Exception("Screenshot failed on GDI+ and PrintWindow! I can't processed anymore! Sorry! TAT");
                    }
                    Imports.SelectObject(memoryDc, oldBitmap);
                    Imports.DeleteObject(bitmap);
                    Imports.DeleteDC(memoryDc);
                    Imports.ReleaseDC(handle, desktopDc);
                }
                else
                {

                }
                if (bmp.AllBlack())
                {
                    logger.WritePrivateLog("Screenshot fetched all black!", lineNumber, caller);
                    continue;
                }
                logger.WritePrivateLog("Screenshot saved to memory used " + s.ElapsedMilliseconds + " ms", lineNumber, caller);
                s.Stop();
                break;
            }
            if (rect.HasValue)
            {
                return new ScreenShot(bmp, logger, Bgr_Rgb).Crop(rect.Value);
            }
            else
            {
                return new ScreenShot(bmp, logger, Bgr_Rgb);
            }
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
            proc.CloseMainWindow();
            Thread.Sleep(1000);
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
            if (proc == null)
            {
                var proclist = Process.GetProcessesByName(processName);
                if (proclist.Count() > 0)
                {
                    proc = proclist.First();
                }
            }
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
        /// Send left click at location
        /// </summary>
        /// <param name="location"></param>
        public void LeftClick(Point location, [CallerLineNumber] int lineNumber = 0, [CallerMemberName] string caller = null)
        {
            if(ClickMethod == ClickMethod.RealMouseMove)
            {
                MoveMouse(location);
                inputSimulator.Mouse.LeftButtonClick();
            }
            else
            {
                Imports.SendMessage(proc.MainWindowHandle, (int)WMessages.WM_LBUTTONDOWN, 1, Imports.CreateLParam(location.X, location.Y));
                Imports.SendMessage(proc.MainWindowHandle, (int)WMessages.WM_LBUTTONUP, 0, Imports.CreateLParam(location.X, location.Y));
            }
        }
        /// <summary>
        /// Send right click
        /// </summary>
        /// <param name="location"></param>
        public void RightClick(Point location, [CallerLineNumber] int lineNumber = 0, [CallerMemberName] string caller = null)
        {
            if (ClickMethod == ClickMethod.RealMouseMove)
            {
                MoveMouse(location);
                inputSimulator.Mouse.RightButtonClick();
            }
            else
            {
                Imports.SendMessage(proc.MainWindowHandle,(int)WMessages.WM_RBUTTONDOWN, 1, Imports.CreateLParam(location.X, location.Y));
                Imports.SendMessage(proc.MainWindowHandle, (int)WMessages.WM_RBUTTONUP, 0, Imports.CreateLParam(location.X, location.Y));
            }
        }
        /// <summary>
        /// Send double left click
        /// </summary>
        /// <param name="location"></param>
        public void LeftDoubleClick(Point location, [CallerLineNumber] int lineNumber = 0, [CallerMemberName] string caller = null)
        {
            if (ClickMethod == ClickMethod.RealMouseMove)
            {
                MoveMouse(location);
                inputSimulator.Mouse.LeftButtonDoubleClick();
            }
            else
            {
                Imports.SendMessage(proc.MainWindowHandle, (int)WMessages.WM_LBUTTONDBLCLK, 1, Imports.CreateLParam(location.X, location.Y));
                Imports.SendMessage(proc.MainWindowHandle, (int)WMessages.WM_LBUTTONUP, 0, Imports.CreateLParam(location.X, location.Y));
            }
        }
        /// <summary>
        /// Send double right click
        /// </summary>
        /// <param name="location"></param>
        public void RightDoubleClick(Point location, [CallerLineNumber] int lineNumber = 0, [CallerMemberName] string caller = null)
        {
            if (ClickMethod == ClickMethod.RealMouseMove)
            {
                MoveMouse(location);
                inputSimulator.Mouse.RightButtonDoubleClick();
            }
            else
            {
                Imports.SendMessage(proc.MainWindowHandle, (int)WMessages.WM_RBUTTONDBLCLK, 1, Imports.CreateLParam(location.X, location.Y));
                Imports.SendMessage(proc.MainWindowHandle, (int)WMessages.WM_RBUTTONUP, 0, Imports.CreateLParam(location.X, location.Y));
            }
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
        /// <summary>
        /// Set Image Capture Crop Rectangle
        /// </summary>
        /// <param name="rect"></param>
        public void SetImageRect(Rectangle rect)
        {
            this.rect = rect;
            logger.WritePrivateLog("Overrided capture rect as " + this.rect.Value.X + " " + this.rect.Value.Y + " " + this.rect.Value.Width + " " + this.rect.Value.Height);
        }
        /// <summary>
        /// Get current cursor position
        /// </summary>
        /// <returns></returns>
        public Point GetCursorPosition()
        {
            Imports.PointInter pos;
            Imports.GetCursorPos(out pos);
            return new Point(pos.X, pos.Y);
        }
        /// <summary>
        /// Get child IntPtr of the process. If no parent hWnd is input will auto use process's <see cref="Process.MainWindowHandle"/>. To get className and windowTitle, use WinSpy!
        /// </summary>
        /// <param name="className"></param>
        /// <param name="windowTitle"></param>
        /// <param name="parent"></param>
        /// <returns></returns>
        public IntPtr GetIntPtr(string className, string windowTitle, IntPtr parent = default(IntPtr))
        {
            if(parent == default(IntPtr))
            {
                parent = proc.MainWindowHandle;
            }
            return Imports.FindWindowEx(parent, IntPtr.Zero, className, windowTitle);
        }
        /// <summary>
        /// Set Mouse clicks send to which IntPtr. If not set will default use process's <see cref="Process.MainWindowHandle"/>.
        /// </summary>
        /// <param name="hWnd"></param>
        /// <returns>True if set success, but false if not valid</returns>
        public bool SetIntPtr(IntPtr hWnd, [CallerLineNumber] int lineNumber = 0, [CallerMemberName] string caller = null)
        {
            if(hWnd == default(IntPtr) || hWnd == IntPtr.Zero)
            {
                logger.WritePrivateLog("Click IntPtr set FAILED as IntPtr is Zero or not valid!", lineNumber, caller);
                //Unable to set Ptr
                return false;
            }
            ClickMethod = ClickMethod.WinAPI;
            logger.WritePrivateLog("Click IntPtr set SUCCESS with " + hWnd.ToInt32(), lineNumber, caller);
            ClickPtr = hWnd;
            return true;
        }
        /// <summary>
        /// Get list of child IntPtr of the process. If no parent hWnd is input will auto use process's <see cref="Process.MainWindowHandle"/>.
        /// </summary>
        /// <param name="parent"></param>
        /// <returns></returns>
        public IEnumerable<IntPtr> GetChildrenPtrs(IntPtr parent = default(IntPtr))
        {
            if (parent == default(IntPtr))
            {
                parent = proc.MainWindowHandle;
            }
            return Imports.GetAllChildHandles(parent);
        }
        /// <summary>
        /// Send Left Click to position point x y
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="lineNumber"></param>
        /// <param name="caller"></param>
        public void LeftClick(int x, int y, [CallerLineNumber] int lineNumber = 0, [CallerMemberName] string caller = null)
        {
            LeftClick(new Point(x, y));
        }
        /// <summary>
        /// Send Right Click to position point x y
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="lineNumber"></param>
        /// <param name="caller"></param>
        public void RightClick(int x, int y, [CallerLineNumber] int lineNumber = 0, [CallerMemberName] string caller = null)
        {
            RightClick(new Point(x, y));
        }
        /// <summary>
        /// Send Double Right Click to position point x y
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="lineNumber"></param>
        /// <param name="caller"></param>
        public void RightDoubleClick(int x, int y, [CallerLineNumber] int lineNumber = 0, [CallerMemberName] string caller = null)
        {
            RightDoubleClick(new Point(x, y));
        }
        /// <summary>
        /// Send Double Left Click to position point x y
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="lineNumber"></param>
        /// <param name="caller"></param>
        public void LeftDoubleClick(int x, int y, [CallerLineNumber] int lineNumber = 0, [CallerMemberName] string caller = null)
        {
            LeftDoubleClick(new Point(x, y));
        }
        /// <summary>
        /// Move mouse to position point x y
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="lineNumber"></param>
        /// <param name="caller"></param>
        public void MoveMouse(int x, int y, [CallerLineNumber] int lineNumber = 0, [CallerMemberName] string caller = null)
        {
            MoveMouse(new Point(x, y));
        }
        /// <summary>
        /// Get the window IntPtr from point x y
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns></returns>
        public IntPtr GetWindowFromPoint(int x, int y)
        {
            return Imports.WindowFromPoint(x, y);
        }
        /// <summary>
        /// Get the window IntPtr from point
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns></returns>
        public IntPtr GetWindowFromPoint(Point point)
        {
            return GetWindowFromPoint(point.X, point.Y);
        }

        private enum WMessages : int
        {
            WM_LBUTTONDOWN = 0x201, //Left mousebutton down
            WM_LBUTTONUP = 0x202,   //Left mousebutton up
            WM_LBUTTONDBLCLK = 0x203, //Left mousebutton doubleclick
            WM_RBUTTONDOWN = 0x204, //Right mousebutton down
            WM_RBUTTONUP = 0x205,   //Right mousebutton up
            WM_RBUTTONDBLCLK = 0x206, //Right mousebutton do
        }
    }

    public enum CaptureMethod
    {
        GDIPlus,
        HDCCapture,
        GraphicCopy
    }

    public enum ClickMethod
    {
        WinAPI,
        RealMouseMove
    }

}
