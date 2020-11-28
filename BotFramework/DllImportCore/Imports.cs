using System;
using System.Runtime.InteropServices;
using System.Drawing;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;
using System.Linq;
using System.Diagnostics;

namespace Zeraniumu
{
    /// <summary>
    /// C# dll imports
    /// </summary>
    internal class Imports
    {
        /// <summary>
        /// Get the commandline for 32bit programs
        /// </summary>
        /// <param name="nProcId"></param>
        /// <param name="sb"></param>
        /// <param name="dwSizeBuf"></param>
        /// <returns></returns>
        [DllImport("ProcCmdLine32.dll", CharSet = CharSet.Unicode, EntryPoint = "GetProcCmdLine")]
        private extern static bool GetProcCmdLine32(uint nProcId, StringBuilder sb, uint dwSizeBuf);
        /// <summary>
        /// Get the commandline for 64bit programs
        /// </summary>
        /// <param name="nProcId"></param>
        /// <param name="sb"></param>
        /// <param name="dwSizeBuf"></param>
        /// <returns></returns>
        [DllImport("ProcCmdLine64.dll", CharSet = CharSet.Unicode, EntryPoint = "GetProcCmdLine")]
        private extern static bool GetProcCmdLine64(uint nProcId, StringBuilder sb, uint dwSizeBuf);
        /// <summary>
        /// Get commandline arguments of programs
        /// </summary>
        /// <param name="proc"></param>
        /// <returns></returns>
        public static string GetCommandLineOfProcess(Process proc)
        {
            var sb = new StringBuilder(0xFFFF);
            switch (IntPtr.Size)
            {
                case 4: GetProcCmdLine32((uint)proc.Id, sb, (uint)sb.Capacity); break;
                case 8: GetProcCmdLine64((uint)proc.Id, sb, (uint)sb.Capacity); break;
            }
            return sb.ToString();
        }
        /// <summary>
        /// Bring window to front
        /// </summary>
        /// <param name="title"></param>
        public static void BringToFront(Process process)
        {
            // Get a handle to the Calculator application.
            IntPtr handle = process.MainWindowHandle;

            // Verify that Calculator is a running process.
            if (handle == IntPtr.Zero)
            {
                return;
            }

            // Make Calculator the foreground application
            SetForegroundWindow(handle);
        }

        /// <summary>
        /// Block user input
        /// </summary>
        /// <param name="fBlockIt"></param>
        /// <returns></returns>
        [DllImport("user32.dll", EntryPoint = "BlockInput")]
        public static extern bool BlockInput([MarshalAs(UnmanagedType.Bool)] bool fBlockIt);

        /// <summary>
        /// Set User agent for whole process of accessing internet
        /// </summary>
        /// <param name="dwOption">Use URLMON_OPTION_USERAGENT</param>
        /// <param name="pBuffer"></param>
        /// <param name="dwBufferLength"></param>
        /// <param name="dwReserved"></param>
        /// <returns></returns>
        [DllImport("urlmon.dll", CharSet = CharSet.Ansi)]

        public static extern int UrlMkSetSessionOption(int dwOption, string pBuffer, int dwBufferLength, int dwReserved);
        /// <summary>
        /// Set user agent
        /// </summary>
        public const int URLMON_OPTION_USERAGENT = 0x10000001;
        /// <summary>
        /// Set other IntPtr's parent to new IntPtr
        /// </summary>
        /// <param name="hwndChild"></param>
        /// <param name="hwndNewParent"></param>
        /// <returns></returns>
        [DllImport("user32.dll")]
        public static extern IntPtr SetParent(IntPtr hwndChild, IntPtr hwndNewParent);
        /// <summary>
        /// Move specific window to new location
        /// </summary>
        /// <param name="hwnd"></param>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="nWidth"></param>
        /// <param name="nHeight"></param>
        /// <param name="bRepaint"></param>
        /// <returns></returns>
        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool MoveWindow(IntPtr hwnd, int x, int y, int nWidth, int nHeight, bool bRepaint);
        /// <summary>
        /// Return IntPtr of specific window, using class name and window text
        /// </summary>
        /// <param name="className"></param>
        /// <param name="WindowText"></param>
        /// <returns></returns>
        [DllImport("user32.dll")]
        public static extern IntPtr FindWindow(string className, string WindowText);
        private delegate bool EnumWindowProc(IntPtr hwnd, IntPtr lParam);
        /// <summary>
        /// Return IntPtr of specific window, using class name and window text, with parent handler
        /// </summary>
        /// <param name="parentHandle"></param>
        /// <param name="childAfter"></param>
        /// <param name="lclassName"></param>
        /// <param name="windowTitle"></param>
        /// <returns></returns>
        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        public static extern IntPtr FindWindowEx(IntPtr parentHandle, IntPtr childAfter, string lclassName, string windowTitle);
        /// <summary>
        /// Set the window state with show or hide
        /// </summary>
        /// <param name="hWnd"></param>
        /// <param name="nCmdShow"></param>
        /// <returns></returns>
        [DllImport("user32.dll")]
        public static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);
        [DllImport("user32")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool EnumChildWindows(IntPtr window, EnumWindowProc callback, IntPtr lParam);
        /// <summary>
        /// Get all the child handle IntPtr of a program
        /// </summary>
        /// <param name="mainHandle"></param>
        /// <returns></returns>
        public static List<IntPtr> GetAllChildHandles(IntPtr mainHandle)
        {
            List<IntPtr> childHandles = new List<IntPtr>();

            GCHandle gcChildhandlesList = GCHandle.Alloc(childHandles);
            IntPtr pointerChildHandlesList = GCHandle.ToIntPtr(gcChildhandlesList);

            try
            {
                EnumWindowProc childProc = new EnumWindowProc(EnumWindow);
                EnumChildWindows(mainHandle, childProc, pointerChildHandlesList);
            }
            finally
            {
                gcChildhandlesList.Free();
            }

            return childHandles;
        }
        /// <summary>
        /// Register Hotkey for some UI or button
        /// </summary>
        /// <param name="hWnd"></param>
        /// <param name="id"></param>
        /// <param name="fsModifiers"></param>
        /// <param name="key"></param>
        /// <returns></returns>

        [DllImport("user32.dll", PreserveSig = false)]
        public static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, Keys key);
        /// <summary>
        /// Unregister the hotkey from UI or button
        /// </summary>
        /// <param name="hWnd"></param>
        /// <param name="id"></param>
        /// <returns></returns>
        [DllImport("user32.dll")]
        public static extern bool UnregisterHotKey(IntPtr hWnd, int id);
        /// <summary>
        /// Send Message to Program
        /// </summary>
        /// <param name="hWnd"></param>
        /// <param name="Msg"></param>
        /// <param name="wParam"></param>
        /// <param name="lParam"></param>
        /// <returns></returns>
        [DllImport("user32.dll", EntryPoint = "SendMessage", CharSet = CharSet.Auto)]
        public static extern bool SendMessage(IntPtr hWnd, uint Msg, int wParam, StringBuilder lParam);
        /// <summary>
        /// Send Message to Program
        /// </summary>
        /// <param name="hWnd"></param>
        /// <param name="Msg"></param>
        /// <param name="wparam"></param>
        /// <param name="lparam"></param>
        /// <returns></returns>
        [DllImport("user32.dll", SetLastError = true)]
        public static extern IntPtr SendMessage(int hWnd, int Msg, int wparam, int lparam);

        const int WM_GETTEXT = 0x000D;
        const int WM_GETTEXTLENGTH = 0x000E;
        /// <summary>
        /// Get control's Text
        /// </summary>
        /// <param name="hWnd"></param>
        /// <returns></returns>
        public static string GetControlText(IntPtr hWnd)
        {

            // Get the size of the string required to hold the window title (including trailing null.) 
            Int32 titleSize = SendMessage((int)hWnd, WM_GETTEXTLENGTH, 0, 0).ToInt32();

            // If titleSize is 0, there is no title so return an empty string (or null)
            if (titleSize == 0)
                return String.Empty;

            StringBuilder title = new StringBuilder(titleSize + 1);

            SendMessage(hWnd, (int)WM_GETTEXT, title.Capacity, title);

            return title.ToString();
        }
        private static bool EnumWindow(IntPtr hWnd, IntPtr lParam)
        {
            GCHandle gcChildhandlesList = GCHandle.FromIntPtr(lParam);

            if (gcChildhandlesList == null || gcChildhandlesList.Target == null)
            {
                return false;
            }

            List<IntPtr> childHandles = gcChildhandlesList.Target as List<IntPtr>;
            childHandles.Add(hWnd);

            return true;
        }
        /// <summary>
        /// Set program location
        /// </summary>
        /// <param name="hWnd"></param>
        /// <param name="hWndInsertAfter"></param>
        /// <param name="X"></param>
        /// <param name="Y"></param>
        /// <param name="cx"></param>
        /// <param name="cy"></param>
        /// <param name="uFlags">Reference <see cref="SWP_NOSIZE"/> <see cref="SWP_NOZORDER"/></param>
        /// <returns></returns>
        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);
        /// <summary>
        /// uflags of <see cref="SetWindowPos"/>
        /// </summary>
        public const uint SWP_NOSIZE = 0x0001, SWP_NOZORDER = 0x0004;
        /// <summary>
        /// Get Window's Rectangle
        /// </summary>
        /// <param name="hWnd"></param>
        /// <param name="rect"></param>
        /// <returns></returns>
        [DllImport("user32.dll")]
        public static extern IntPtr GetWindowRect(IntPtr hWnd, ref Rectangle rect);
        /// <summary>
        /// Set window to foreground
        /// </summary>
        /// <param name="hWnd"></param>
        /// <returns></returns>
        [DllImport("USER32.DLL")]
        public static extern bool SetForegroundWindow(IntPtr hWnd);
        /// <summary>
        /// Get Parent's hWnd
        /// </summary>
        /// <param name="hWnd"></param>
        /// <returns></returns>
        [DllImport("user32.dll", ExactSpelling = true, CharSet = CharSet.Auto)]
        public static extern IntPtr GetParent(IntPtr hWnd);
        /// <summary>
        /// Get all child window handles
        /// </summary>
        /// <param name="hParent"></param>
        /// <param name="Class"></param>
        /// <param name="name"></param>
        /// <param name="maxCount"></param>
        /// <returns></returns>
        public static List<IntPtr> GetAllChildrenWindowHandles(IntPtr hParent, string Class, string name, int maxCount)
        {
            List<IntPtr> result = new List<IntPtr>();
            int ct = 0;
            IntPtr prevChild = IntPtr.Zero;
            IntPtr currChild = IntPtr.Zero;
            while (true && ct < maxCount)
            {
                currChild = FindWindowEx(hParent, prevChild, Class, name);
                if (currChild == IntPtr.Zero) break;
                result.Add(currChild);
                prevChild = currChild;
                ++ct;
            }
            return result;
        }
        /// <summary>
        /// Send Message to window
        /// </summary>
        /// <param name="hWnd"></param>
        /// <param name="Msg"></param>
        /// <param name="wParam"></param>
        /// <param name="lParam"></param>
        /// <returns></returns>
        [DllImport("user32.dll")]
        public static extern int SendMessage(IntPtr hWnd, int Msg, int wParam, int lParam);
        /// <summary>
        /// Used for borderless window to release mouse click
        /// </summary>
        /// <returns></returns>
        [DllImport("user32.dll")]
        public static extern bool ReleaseCapture();
        /// <summary>
        /// Get this process
        /// </summary>
        /// <returns></returns>
        [DllImport("kernel32.dll")]
        public static extern IntPtr GetCurrentProcess();
        /// <summary>
        /// 
        /// </summary>
        /// <param name="moduleName"></param>
        /// <returns></returns>
        [DllImport("kernel32.dll")]
        public static extern IntPtr GetModuleHandle(string moduleName);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="hModule"></param>
        /// <param name="procName"></param>
        /// <returns></returns>
        [DllImport("kernel32")]

        public static extern IntPtr GetProcAddress(IntPtr hModule, string procName);
        /// <summary>
        /// 
        /// </summary>
        /// <param name="hProcess"></param>
        /// <param name="wow64Process"></param>
        /// <returns></returns>
        [DllImport("kernel32.dll")]
        public static extern bool IsWow64Process(IntPtr hProcess, out bool wow64Process);
        /// <summary>
        /// 
        /// </summary>
        public const int WM_NCLBUTTONDOWN = 0xA1;
        /// <summary>
        /// 
        /// </summary>
        public const int HT_CAPTION = 0x2;
        /// <summary>
        /// 
        /// </summary>
        /// <param name="hWnd"></param>
        /// <returns></returns>
        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool IsWindow(IntPtr hWnd);
        /// <summary>
        /// 
        /// </summary>
        /// <param name="control"></param>
        /// <param name="type"></param>
        /// <returns></returns>
        public static IEnumerable<Control> GetAll(Control control, Type type = null)
        {
            var controls = control.Controls.Cast<Control>();
            //check the all value, if true then get all the controls
            //otherwise get the controls of the specified type
            if (type == null)
                return controls.SelectMany(ctrl => GetAll(ctrl, type)).Concat(controls);
            else
                return controls.SelectMany(ctrl => GetAll(ctrl, type)).Concat(controls).Where(c => c.GetType() == type);
        }
        [StructLayout(LayoutKind.Sequential)]
        private struct LASTINPUTINFO
        {
            public uint cbSize;
            public uint dwTime;
        }
        [DllImport("user32.dll")]
        static extern bool GetLastInputInfo(ref LASTINPUTINFO plii);
        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public static TimeSpan GetInactiveTime()
        {
            LASTINPUTINFO info = new LASTINPUTINFO();
            info.cbSize = (uint)Marshal.SizeOf(info);
            if (GetLastInputInfo(ref info))
                return TimeSpan.FromMilliseconds(Environment.TickCount - info.dwTime);
            else
                return TimeSpan.FromMilliseconds(0);
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="dwFlags"></param>
        /// <param name="dx"></param>
        /// <param name="dy"></param>
        /// <param name="cButtons"></param>
        /// <param name="dwExtraInfo"></param>
        [DllImport("user32.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall)]
        public static extern void mouse_event(uint dwFlags, int dx, int dy, uint cButtons, uint dwExtraInfo);
        /// <summary>
        /// 
        /// </summary>
        /// <param name="hWnd"></param>
        /// <param name="bRevert"></param>
        /// <returns></returns>
        [DllImport("user32")]
        public static extern IntPtr GetSystemMenu(IntPtr hWnd, bool bRevert);
        /// <summary>
        /// 
        /// </summary>
        /// <param name="hMenu"></param>
        /// <param name="itemId"></param>
        /// <param name="uEnable"></param>
        /// <returns></returns>

        [DllImport("user32")]
        public static extern bool EnableMenuItem(IntPtr hMenu, uint itemId, uint uEnable);
        /// <summary>
        /// 
        /// </summary>
        //Mouse actions
        public const int MOUSEEVENTF_LEFTDOWN = 0x02;
        /// <summary>
        /// 
        /// </summary>
        public const int MOUSEEVENTF_LEFTUP = 0x04;
        /// <summary>
        /// 
        /// </summary>
        public const int MOUSEEVENTF_RIGHTDOWN = 0x08;
        /// <summary>
        /// 
        /// </summary>
        public const int MOUSEEVENTF_RIGHTUP = 0x10;
        /// <summary>
        /// 
        /// </summary>
        public const int MOUSEEVENTF_MOVE = 0x0001;
        /// <summary>
        /// 
        /// </summary>
        /// <param name="hWnd"></param>
        /// <param name="hdcBlt"></param>
        /// <param name="nFlags"></param>
        /// <returns></returns>
        [DllImport("user32.dll")]
        public static extern bool PrintWindow(IntPtr hWnd, IntPtr hdcBlt, int nFlags);
        /// <summary>
        /// Force redraw the window region
        /// </summary>
        /// <param name="hwnd"></param>
        /// <param name="rcUpdate"></param>
        /// <param name="hrgnUpdate"></param>
        /// <param name="flags"></param>
        /// <returns></returns>
        [DllImport("user32.dll", CharSet = CharSet.Auto, ExactSpelling = true)]
        public static extern bool RedrawWindow(IntPtr hwnd, Rectangle rcUpdate, IntPtr hrgnUpdate, int flags);
        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool UpdateWindow(IntPtr hWnd);
        /// <summary>
        /// GDI+ Capture
        /// </summary>
        /// <param name="hdcDest"></param>
        /// <param name="nxDest"></param>
        /// <param name="nyDest"></param>
        /// <param name="nWidth"></param>
        /// <param name="nHeight"></param>
        /// <param name="hdcSrc"></param>
        /// <param name="nXSrc"></param>
        /// <param name="nYSrc"></param>
        /// <param name="dwRop"></param>
        /// <returns></returns>
        [DllImport("gdi32.dll")]
        public static extern bool BitBlt(IntPtr hdcDest, int nxDest, int nyDest, int nWidth, int nHeight, IntPtr hdcSrc, int nXSrc, int nYSrc, int dwRop);

        [DllImport("gdi32.dll")]
        public static extern IntPtr CreateCompatibleBitmap(IntPtr hdc, int width, int nHeight);

        [DllImport("gdi32.dll")]
        public static extern IntPtr CreateCompatibleDC(IntPtr hdc);

        [DllImport("gdi32.dll")]
        public static extern IntPtr DeleteDC(IntPtr hdc);

        [DllImport("gdi32.dll")]
        public static extern IntPtr DeleteObject(IntPtr hObject);

        [DllImport("user32.dll")]
        public static extern IntPtr GetDesktopWindow();

        [DllImport("user32.dll")]
        public static extern IntPtr GetWindowDC(IntPtr hWnd);

        [DllImport("user32.dll")]
        public static extern bool ReleaseDC(IntPtr hWnd, IntPtr hDc);

        [DllImport("gdi32.dll")]
        public static extern IntPtr SelectObject(IntPtr hdc, IntPtr hObject);

        [DllImport("gdi32.dll")]
        public static extern int GetDeviceCaps(IntPtr hdc, int nIndex);
        public enum DeviceCap
        {
            VERTRES = 10,
            DESKTOPVERTRES = 117,
        }
    }
}
