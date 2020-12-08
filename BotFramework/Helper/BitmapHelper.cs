using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using Emgu.CV;
using Emgu.CV.Structure;

namespace Zeraniumu.Helper
{
    public static class BitmapHelper
    {
        internal static bool AllBlack(this Bitmap bmp)
        {
            // Lock the bitmap's bits.  
            Rectangle rect = new Rectangle(0, 0, bmp.Width, bmp.Height);
            Bitmap cloned = new Bitmap(bmp);
            BitmapData bmpData = cloned.LockBits(rect, ImageLockMode.ReadWrite, bmp.PixelFormat);

            // Get the address of the first line.
            IntPtr ptr = bmpData.Scan0;

            // Declare an array to hold the bytes of the bitmap.
            int bytes = bmpData.Stride * bmp.Height;
            byte[] rgbValues = new byte[bytes];

            // Copy the RGB values into the array.
            Marshal.Copy(ptr, rgbValues, 0, bytes);

            // Scanning for non-zero bytes
            bool allBlack = true;
            for (int index = 0; index < rgbValues.Length; index++)
                if (rgbValues[index] != 0)
                {
                    allBlack = false;
                    break;
                }
            // Unlock the bits.
            cloned.UnlockBits(bmpData);
            cloned.Dispose();
            return allBlack;
        }
    }
}
