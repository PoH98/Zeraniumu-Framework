using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.OCR;
using Emgu.CV.Structure;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Xml;

namespace BotFramework
{
    public class ScreenShot : ImgData
    {        
        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        /// <returns><inheritdoc/></returns>
        public ScreenShot(Bitmap data, ILog logger, bool Bgr_Rgb = true) : base(data, logger, Bgr_Rgb)
        {
        }
        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        /// <returns><inheritdoc/></returns>
        public ScreenShot(string fileName, ILog logger, bool Bgr_Rgb = true, bool xmlFile = true) : base(fileName, logger, Bgr_Rgb, xmlFile)
        {
        }
        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        /// <returns><inheritdoc/></returns>
        public override bool ColorMatch(Point location, Color color, double matchRadius, [CallerLineNumber] int lineNumber = 0, [CallerMemberName] string caller = null)
        {
            Stopwatch stopwatch = Stopwatch.StartNew();
            if(matchRadius > 1 || matchRadius < 0.1)
            {
                throw new ArgumentException("MatchRadius should between 0.1 to 1.0!");
            }
            double bluemin = color.B * matchRadius;
            double bluemax = color.B / matchRadius;
            double greenmin = color.G * matchRadius;
            double greenmax = color.G / matchRadius;
            double redmin = color.R * matchRadius;
            double redmax = color.R / matchRadius;
            if (Bgr)
            {
                var bgr = (image as Image<Bgr, byte>)[location.X, location.Y];
                if(bgr.Blue >= bluemin && bgr.Blue <= bluemax)
                {
                    if(bgr.Green >= greenmin && bgr.Green <= greenmax)
                    {
                        if(bgr.Red >= redmin && bgr.Red <= redmax)
                        {
                            stopwatch.Stop();
                            logger.WritePrivateLog("Color matched in " + stopwatch.ElapsedMilliseconds + "ms", lineNumber, caller);
                            return true;
                        }
                    }
                }
                stopwatch.Stop();
                logger.WritePrivateLog("Color not matched in " + stopwatch.ElapsedMilliseconds + "ms, point color is " + bgr.Red + "," + bgr.Green + "," + bgr.Blue, lineNumber, caller);
                return false;
            }
            else
            {
                var rgb = (image as Image<Rgb, byte>)[location.X, location.Y];
                if (rgb.Blue >= bluemin && rgb.Blue <= bluemax)
                {
                    if (rgb.Green >= greenmin && rgb.Green <= greenmax)
                    {
                        if (rgb.Red >= redmin && rgb.Red <= redmax)
                        {
                            stopwatch.Stop();
                            logger.WritePrivateLog("Color matched in " + stopwatch.ElapsedMilliseconds + "ms", lineNumber, caller);
                            return true;
                        }
                    }
                }
                stopwatch.Stop();
                logger.WritePrivateLog("Color not matched in " + stopwatch.ElapsedMilliseconds + "ms, point color is " + rgb.Red + "," + rgb.Green + "," + rgb.Blue, lineNumber, caller);
                return false;
            }
        }
        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        /// <returns><inheritdoc/></returns>
        public override IImageData Crop(Rectangle area, [CallerLineNumber] int lineNumber = 0, [CallerMemberName] string caller = null)
        {
            try
            {
                var watch = Stopwatch.StartNew();
                if (Bgr)
                {
                    var temp = (this.image as Image<Bgr, byte>);
                    var bmp = temp.ToBitmap();
                    Bitmap newBm = bmp.Clone(area, bmp.PixelFormat);
                    watch.Stop();
                    logger.WritePrivateLog("Image cropped in " + watch.ElapsedMilliseconds + "ms.", lineNumber, caller);
                    return new ScreenShot(newBm, logger, Bgr);
                }
                else
                {
                    var temp = (this.image as Image<Rgb, byte>);
                    var bmp = temp.ToBitmap();
                    Bitmap newBm = bmp.Clone(area, bmp.PixelFormat);
                    watch.Stop();
                    logger.WritePrivateLog("Image cropped in " + watch.ElapsedMilliseconds + "ms.", lineNumber, caller);
                    return new ScreenShot(newBm, logger, Bgr);
                }
            }
            catch
            {
                return this;
            }

        }
        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        /// <returns><inheritdoc/></returns>
        public override Point? FindColor(Rectangle rectangle, Color color, double matchRadius, [CallerLineNumber] int lineNumber = 0, [CallerMemberName] string caller = null)
        {
            Stopwatch watch = Stopwatch.StartNew();
            var crop = Crop(rectangle);
            var bmp = crop.ToBitmap();
            int Width = bmp.Width;
            int Height = bmp.Height;
            int PixelCount = Width * Height;
            Rectangle rect = new Rectangle(0, 0, Width, Height);
            int Depth = Bitmap.GetPixelFormatSize(bmp.PixelFormat);
            if (Depth != 8 && Depth != 24 && Depth != 32)
            {
                logger.WritePrivateLog("Image bit per pixel format not supported");
                return null;
            }
            BitmapData bd = bmp.LockBits(rect, ImageLockMode.ReadOnly, bmp.PixelFormat);
            int step = Depth / 8;
            byte[] pixel = new byte[PixelCount * step];
            IntPtr ptr = bd.Scan0;
            Marshal.Copy(ptr, pixel, 0, pixel.Length);
            var redmin = color.R * matchRadius;
            var redmax = color.R / matchRadius;
            var greenmin = color.G * matchRadius;
            var greenmax = color.G / matchRadius;
            var bluemin = color.B * matchRadius;
            var bluemax = color.B / matchRadius;
            for (int i = 0; i < bmp.Height; i++)
            {
                for (int j = 0; j < bmp.Width; j++)
                {

                    //Get the color at each pixel
                    Color clr = GetPixel(j, i, step, Width, Depth, pixel);
                    if (clr.R >= redmin && clr.R <= redmax)
                    {
                        if (clr.G >= greenmin && clr.G <= greenmax)
                        {
                            if (clr.B >= bluemin && clr.B <= bluemax)
                            {
                                watch.Stop();
                                logger.WritePrivateLog("Color found completed in " + watch.ElapsedMilliseconds + " ms", lineNumber, caller);
                                bmp.UnlockBits(bd);
                                return new Point(j, i);
                            }
                        }
                    }
                }
            }
            watch.Stop();
            logger.WritePrivateLog("Color not found completed in " + watch.ElapsedMilliseconds + " ms", lineNumber, caller);
            bmp.UnlockBits(bd);
            return null;
        }
        private Color GetPixel(int x, int y, int step, int Width, int Depth, byte[] pixel)
        {
            Color clr = Color.Empty;
            int i = ((y * Width + x) * step);
            if (i > pixel.Length)
            {
                logger.WritePrivateLog("index of pixel array out of range at GetPixel");
                return clr;
            }
            if (Depth == 32 || Depth == 24)
            {
                byte b = pixel[i];
                byte g = pixel[i + 1];
                byte r = pixel[i + 2];
                clr = Color.FromArgb(r, g, b);
            }
            else if (Depth == 8)
            {
                byte b = pixel[i];
                clr = Color.FromArgb(b, b, b);
            }
            return clr;
        }
        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        /// <returns><inheritdoc/></returns>
        public override List<Point> FindImage(IImageData imageData, bool FindOnlyOne, double matchRadius, [CallerLineNumber] int lineNumber = 0, [CallerMemberName] string caller = null)
        {
            var watch = Stopwatch.StartNew();
            var image = imageData as ImgData;
            List<Point> matched = new List<Point>();
            if (image.Bgr)
            {
                using (var dest = new Image<Bgr, byte>(image.Size))
                {
                    //For thread safe
                    CvInvoke.cvCopy(image.GetPtr(), dest.Ptr, IntPtr.Zero);
                    if (Bgr)
                    {
                        using (Image<Gray, float> result = (this.image as Image<Bgr, byte>).MatchTemplate(dest, TemplateMatchingType.CcoeffNormed))
                        {
                            result.MinMax(out double[] minValues, out double[] maxValues, out Point[] minLocations, out Point[] maxLocations);
                            for (int x = 0; x < maxValues.Length; x++)
                            {
                                if (maxValues[x] > matchRadius)
                                {
                                    matched.Add(maxLocations[x]);
                                    if (FindOnlyOne)
                                    {
                                        break;
                                    }
                                }
                            }
                        }
                    }
                    else
                    {
                        using (Image<Gray, float> result = (this.image as Image<Rgb, byte>).MatchTemplate(dest.Convert<Rgb, byte>(), TemplateMatchingType.CcoeffNormed))
                        {
                            result.MinMax(out double[] minValues, out double[] maxValues, out Point[] minLocations, out Point[] maxLocations);
                            for (int x = 0; x < maxValues.Length; x++)
                            {
                                if (maxValues[x] > matchRadius)
                                {
                                    matched.Add(maxLocations[x]);
                                    if (FindOnlyOne)
                                    {
                                        break;
                                    }
                                }
                            }
                        }
                    }
                }
            }
            else
            {
                using (var dest = new Image<Rgb, byte>(image.Size))
                {
                    //For thread safe
                    CvInvoke.cvCopy(image.GetPtr(), dest.Ptr, IntPtr.Zero);
                    if (Bgr)
                    {
                        using (Image<Gray, float> result = (this.image as Image<Bgr, byte>).MatchTemplate(dest.Convert<Bgr, byte>(), TemplateMatchingType.CcoeffNormed))
                        {
                            result.MinMax(out double[] minValues, out double[] maxValues, out Point[] minLocations, out Point[] maxLocations);
                            for (int x = 0; x < maxValues.Length; x++)
                            {
                                if (maxValues[x] > matchRadius)
                                {
                                    matched.Add(maxLocations[x]);
                                    if (FindOnlyOne)
                                    {
                                        break;
                                    }
                                }
                            }
                        }
                    }
                    else
                    {
                        using (Image<Gray, float> result = (this.image as Image<Rgb, byte>).MatchTemplate(dest, TemplateMatchingType.CcoeffNormed))
                        {
                            result.MinMax(out double[] minValues, out double[] maxValues, out Point[] minLocations, out Point[] maxLocations);
                            for (int x = 0; x < maxValues.Length; x++)
                            {
                                if (maxValues[x] > matchRadius)
                                {
                                    matched.Add(maxLocations[x]);
                                    if (FindOnlyOne)
                                    {
                                        break;
                                    }
                                }
                            }
                        }
                    }
                }
            }
            watch.Stop();
            logger.WritePrivateLog("Image processed in " + watch.ElapsedMilliseconds + "ms.", lineNumber, caller);
            return matched;
        }
        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        /// <returns><inheritdoc/></returns>
        public override Color GetColor(Point location, [CallerLineNumber] int lineNumber = 0, [CallerMemberName] string caller = null)
        {
            if (Bgr)
            {
                var bgr = (image as Image<Bgr, byte>)[location.X, location.Y];
                return Color.FromArgb((int)bgr.Red, (int)bgr.Green, (int)bgr.Blue);
            }
            else
            {
                var rgb = (image as Image<Rgb, byte>)[location.X, location.Y];
                return Color.FromArgb((int)rgb.Red, (int)rgb.Green, (int)rgb.Blue);
            }
        }
        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        /// <returns><inheritdoc/></returns>
        public override void SaveFile(string path, [CallerLineNumber] int lineNumber = 0, [CallerMemberName] string caller = null)
        {
            var watch = Stopwatch.StartNew();
            if (Bgr)
            {
                (image as Image<Bgr, byte>).Save(path);
            }
            else
            {
                (image as Image<Rgb, byte>).Save(path);
            }
            watch.Stop();
            logger.WritePrivateLog("Image saved file in " + watch.ElapsedMilliseconds + "ms.", lineNumber, caller);
        }
        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        /// <returns><inheritdoc/></returns>
        public override void SaveXml(string path, [CallerLineNumber] int lineNumber = 0, [CallerMemberName] string caller = null)
        {
            var watch = Stopwatch.StartNew();
            using (XmlWriter writer = XmlWriter.Create(path))
            {
                if (Bgr)
                {
                    (image as Image<Bgr, byte>).WriteXml(writer);
                }
                else
                {
                    (image as Image<Rgb, byte>).WriteXml(writer);
                }
            }
            watch.Stop();
            logger.WritePrivateLog("Image saved xml in " + watch.ElapsedMilliseconds + "ms.", lineNumber, caller);
        }
        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        /// <returns></returns>
        public override string OCR(IController controller)
        {
            var tess = controller.Tesseract as Tesseract;
            if (Bgr)
            {
                tess.SetImage(image as Image<Bgr, byte>);
            }
            else
            {
                tess.SetImage(image as Image<Rgb, byte>);
            }
            tess.Recognize();
            return tess.GetUTF8Text();
        }
    }
}
