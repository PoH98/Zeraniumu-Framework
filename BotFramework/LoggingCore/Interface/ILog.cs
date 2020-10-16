using System.Drawing;
using System.Runtime.CompilerServices;

namespace Zeraniumu
{
    public interface ILog
    {
        /// <summary>
        /// Write a log with specific <see cref="Color"/> and show to user
        /// </summary>
        /// <param name="log"></param>
        /// <param name="color"></param>
        void WriteLog(string log, Color color, [CallerLineNumber] int lineNumber = 0, [CallerMemberName] string caller = null);
        /// <summary>
        /// Write a log and show to user
        /// </summary>
        /// <param name="log"></param>
        /// <param name="color"></param>
        void WriteLog(string log, [CallerLineNumber] int lineNumber = 0, [CallerMemberName] string caller = null);
        /// <summary>
        /// Write log to file only
        /// </summary>
        /// <param name="log"></param>
        void WritePrivateLog(string log, [CallerLineNumber] int lineNumber = 0, [CallerMemberName] string caller = null);
        /// <summary>
        /// Set the log path
        /// </summary>
        void SetLogPath(string logPath);
        /// <summary>
        /// Clear old log files
        /// </summary>
        void ClearOldLogs();
    }
}
