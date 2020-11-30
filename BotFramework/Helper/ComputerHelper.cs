using System;
using System.Drawing;

namespace Zeraniumu.Helper
{
    internal class ComputerHelper
    {
        internal static float getScalingFactor()
        {
            Graphics g = Graphics.FromHwnd(IntPtr.Zero);
            IntPtr desktop = g.GetHdc();
            int LogicalScreenHeight = Imports.GetDeviceCaps(desktop, (int)Imports.DeviceCap.VERTRES);
            int PhysicalScreenHeight = Imports.GetDeviceCaps(desktop, (int)Imports.DeviceCap.DESKTOPVERTRES);
            float ScreenScalingFactor = (float)PhysicalScreenHeight / (float)LogicalScreenHeight;
            g.ReleaseHdc(desktop);
            g.Dispose();
            return ScreenScalingFactor;
        }
    }
}
