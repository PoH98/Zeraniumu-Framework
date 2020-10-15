using System.Runtime.CompilerServices;

namespace BotFramework
{
    /// <summary>
    /// The base interface of all controllers
    /// </summary>
    public interface IController
    {
        /// <summary>
        /// Get the image of the process or android emulator
        /// </summary>
        /// <param name="Bgr_Rgb"></param>
        /// <param name="lineNumber"></param>
        /// <param name="caller"></param>
        /// <returns></returns>
        IImageData Screenshot(bool Bgr_Rgb = true, [CallerLineNumber] int lineNumber = 0, [CallerMemberName] string caller = null);
        /// <summary>
        /// Prepair ocr files with specific language
        /// </summary>
        /// <param name="language"></param>
        void PrepairOCR(string language, string whiteList = null, [CallerLineNumber] int lineNumber = 0, [CallerMemberName] string caller = null);
        /// <summary>
        /// Get Tesseract here. We need avoid dependencies so we use object to return
        /// </summary>
        /// <returns></returns>
        object Tesseract { get; set; }
    }
}
