using Emgu.CV;
using Emgu.CV.Structure;
using Emgu.CV.OCR;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Runtime.CompilerServices;
using System.Xml.Serialization;

namespace Zeraniumu
{
    /// <summary>
    /// Interface of ImgData
    /// </summary>
    public interface IImageData: IDisposable
    {
        IntPtr GetPtr([CallerLineNumber] int lineNumber = 0, [CallerMemberName] string caller = null);

        List<Point> FindImage(IImageData image, bool FindOnlyOne, double matchRadius, [CallerLineNumber] int lineNumber = 0, [CallerMemberName] string caller = null);

        IImageData Crop(Rectangle area, [CallerLineNumber] int lineNumber = 0, [CallerMemberName] string caller = null);

        bool ColorMatch(Point location, Color color, double matchRadius, [CallerLineNumber] int lineNumber = 0, [CallerMemberName] string caller = null);

        Color GetColor(Point location, [CallerLineNumber] int lineNumber = 0, [CallerMemberName] string caller = null);

        Point? FindColor(Rectangle rectangle, Color color, double matchRadius, [CallerLineNumber] int lineNumber = 0, [CallerMemberName] string caller = null);

        void SaveXml(string path, [CallerLineNumber] int lineNumber = 0, [CallerMemberName] string caller = null);

        void SaveFile(string path, [CallerLineNumber] int lineNumber = 0, [CallerMemberName] string caller = null);

        Bitmap ToBitmap();

        string OCR(IController controller);
    }
    /// <summary>
    /// Image data
    /// </summary>
    public abstract class ImgData:IImageData
    {
        /// <summary>
        /// The image object, which is using the EmguCV Image
        /// </summary>
        protected object image;
        /// <summary>
        /// The logger object for logs
        /// </summary>
        protected readonly ILog logger;
        /// <summary>
        /// OCR Text recognization
        /// </summary>
        protected readonly Tesseract tesseract;
        /// <summary>
        /// Check if image is Bgr or Rgb
        /// </summary>
        internal readonly bool Bgr;
        /// <summary>
        /// Image's size
        /// </summary>
        internal Size Size;
        /// <summary>
        /// Create a new ImgData object with Bgr formatted data or Rgb formatted
        /// </summary>
        public ImgData(Bitmap data, ILog logger, bool Bgr_Rgb = true)
        {
            this.Bgr = Bgr_Rgb;
            this.Size = data.Size;
            this.logger = logger;
            if (Bgr_Rgb)
            {
                image = data.ToImage<Bgr, byte>();
            }
            else
            {
                image = data.ToImage<Rgb, byte>();
            }
        }
        /// <summary>
        /// Convert a file with xml into ImgData OR we convert the normal image file
        /// </summary>
        /// <param name="fileName"></param>
        public ImgData(string fileName, ILog logger, bool Bgr_Rgb = true, bool xmlFile = true)
        {
            this.logger = logger;
            if (xmlFile)
            {
                this.Bgr = Bgr_Rgb;
                if (Bgr_Rgb)
                {
                    using(FileStream stream  = File.OpenRead(fileName))
                    {
                        Image<Bgr, Byte> image = (Image<Bgr, Byte>)new XmlSerializer(typeof(Image<Bgr, Byte>)).Deserialize(stream);
                        this.Size = new Size(image.Width, image.Height);
                        this.image = image;
                    }
                }
                else
                {
                    using (FileStream stream = File.OpenRead(fileName))
                    {
                        Image<Rgb, Byte> image = (Image<Rgb, Byte>)new XmlSerializer(typeof(Image<Rgb, Byte>)).Deserialize(stream);
                        this.Size = new Size(image.Width, image.Height);
                        this.image = image;
                    }
                }
            }
            else
            {
                this.Bgr = Bgr_Rgb;
                if (Bgr_Rgb)
                {
                    var buffer = new Image<Bgr, byte>(fileName);
                    image = buffer;
                    this.Size = new Size(buffer.Width, buffer.Height);
                }
                else
                {
                    var buffer = new Image<Rgb, byte>(fileName);
                    image = buffer;
                    this.Size = new Size(buffer.Width, buffer.Height);
                }
            }
        }
        /// <summary>
        /// Get back the image IntPtr
        /// </summary>
        /// <returns></returns>
        public IntPtr GetPtr([CallerLineNumber] int lineNumber = 0, [CallerMemberName] string caller = null)
        {
            if (Bgr)
            {
                return (image as Image<Bgr, byte>).Ptr;
            }
            else
            {
                return (image as Image<Rgb, byte>).Ptr;
            }
        }

        /// <summary>
        /// Find the image
        /// </summary>
        /// <param name="image"></param>
        /// <param name="FindOnlyOne"></param>
        /// <param name="matchRadius"></param>
        /// <returns></returns>
        public abstract List<Point> FindImage(IImageData image, bool FindOnlyOne, double matchRadius, [CallerLineNumber] int lineNumber = 0, [CallerMemberName] string caller = null);

        /// <summary>
        /// Crop the image
        /// </summary>
        /// <param name="area"></param>
        /// <returns></returns>
        public abstract IImageData Crop(Rectangle area, [CallerLineNumber] int lineNumber = 0, [CallerMemberName] string caller = null);

        /// <summary>
        /// Save the image to file 
        /// </summary>
        /// <param name="path"></param>
        public virtual void SaveFile(string path, [CallerLineNumber] int lineNumber = 0, [CallerMemberName] string caller = null)
        {
            throw new ImageException("This image is not savable to this type or not implemented!");
        }
        /// <summary>
        /// Save the image to xml file
        /// </summary>
        /// <param name="path"></param>
        /// <param name="lineNumber"></param>
        /// <param name="caller"></param>
        public virtual void SaveXml(string path, [CallerLineNumber] int lineNumber = 0, [CallerMemberName] string caller = null)
        {
            throw new ImageException("This image is not savable to this type or not implemented!");
        }

        /// <summary>
        /// Destroy the imageData object
        /// </summary>
        public void Dispose()
        {
            if (Bgr)
            {
                 (image as Image<Bgr, byte>).Dispose();
            }
            else
            {
                 (image as Image<Rgb, byte>).Dispose();
            }
            GC.Collect();
        }
        /// <summary>
        /// Match color from image and location. MatchRadius should be between 0.1 to 1.0
        /// </summary>
        /// <param name="location"></param>
        /// <param name="color"></param>
        /// <param name="matchRadius"></param>
        /// <param name="lineNumber"></param>
        /// <param name="caller"></param>
        /// <returns><see cref="true"/> if <see cref="Color"/> is match</returns>
        public abstract bool ColorMatch(Point location, Color color, double matchRadius, [CallerLineNumber] int lineNumber = 0, [CallerMemberName] string caller = null);
        /// <summary>
        /// Get the color in specific location of the image
        /// </summary>
        /// <param name="location"></param>
        /// <param name="lineNumber"></param>
        /// <param name="caller"></param>
        /// <returns>The <see cref="Color"/> from specific location</returns>
        public abstract Color GetColor(Point location, [CallerLineNumber] int lineNumber = 0, [CallerMemberName] string caller = null);
        /// <summary>
        /// Find a color in the area of image. MatchRadius should be between 0.1 to 1.0
        /// </summary>
        /// <param name="rectangle"></param>
        /// <param name="color"></param>
        /// <param name="matchRadius"></param>
        /// <param name="FoundLocation"></param>
        /// <param name="lineNumber"></param>
        /// <param name="caller"></param>
        /// <returns>found <see cref="Color"/> location <see cref="Point"/>, else <see cref="null"/></returns>
        public abstract Point? FindColor(Rectangle rectangle, Color color, double matchRadius, [CallerLineNumber] int lineNumber = 0, [CallerMemberName] string caller = null);
        /// <summary>
        /// Convert ImgData to Bitmap
        /// </summary>
        /// <returns></returns>
        public virtual Bitmap ToBitmap()
        {
            if (Bgr)
            {
                return (image as Image<Bgr, byte>).ToBitmap();
            }
            else
            {
                return (image as Image<Rgb, byte>).ToBitmap();
            }
        }
        /// <summary>
        /// Recognize the text from image. Make sure you had cropped the image to specify the text area for reading
        /// </summary>
        /// <returns></returns>
        public abstract string OCR(IController controller);
    }
}
