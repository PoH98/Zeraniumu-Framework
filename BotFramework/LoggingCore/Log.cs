using System;
using System.Drawing;
using System.IO;
using System.Runtime.CompilerServices;
using System.Windows.Forms;
using Console = Colorful.Console;

namespace Zeraniumu
{
    public class Log:ILog
    {
        private RichTextBox LogTextBox;
        private string logfilepath;
        /// <summary>
        /// Create a log file and bind the <see cref="RichTextBox"/> for showing logs(optional)
        /// </summary>
        /// <param name="logPath"></param>
        /// <param name="textBox"></param>
        public Log(RichTextBox textBox = null)
        {
            LogTextBox = textBox;
        }
        /// <summary>
        /// Used to clear last 7 days logs, overrideable
        /// </summary>
        public virtual void ClearOldLogs()
        {
            if (logfilepath.Contains("\\"))
            {
                foreach (var file in Directory.GetFiles(logfilepath.Remove(logfilepath.LastIndexOf('\\'))))
                {
                    FileInfo info = new FileInfo(file);
                    if ((DateTime.Now - info.CreationTime).TotalDays >= 7)
                    {
                        info.Delete();
                    }
                }
            }
        }

        /// <summary>
        /// The default IControllers will set this, if you are implementing your own controller then you can customize the log file location. Path should be ended with '\\' to define as path
        /// </summary>
        /// <param name="logPath"></param>
        public virtual void SetLogPath(string logPath)
        {
            string oldPath = logfilepath;
            if (logPath.EndsWith("\\"))
            {
                if (!Directory.Exists(logPath))
                {
                    Directory.CreateDirectory(logPath);
                }
                //path
                logPath = Path.Combine(logPath, DateTime.Now.ToString().Replace(' ', '_').Replace('/', '_').Replace(':', '_') + ".log");
            }
            else
            {
                //file
            }
            logfilepath = logPath;
            if (File.Exists(oldPath))
            {
                File.Move(oldPath, logfilepath);
            }
            //Clear old log files
            ClearOldLogs();
            WritePrivateLog("Log Path Setted: " + logfilepath);
        }

        /// <summary>
        /// Write a log and show to user in the <see cref="RichTextBox"/> if had been set with specific <see cref="Color"/>
        /// </summary>
        /// <param name="log"></param>
        /// <param name="color"></param>
        public virtual void WriteLog(string log, Color color, [CallerLineNumber] int lineNumber = 0, [CallerMemberName] string caller = null)
        {
            if(LogTextBox != null)
            {
                LogTextBox.Invoke((MethodInvoker)delegate
                {
                    LogTextBox.SelectionColor = color;
                    LogTextBox.AppendText("[" + DateTime.Now.ToLongTimeString() + "]: " + log + "\n");
                });
            }
            else
            {
                Console.WriteLine(log, color);
            }
            try
            {
                File.AppendAllText(logfilepath, "[" + DateTime.Now.ToLongTimeString() + "]: [" + caller + "|" + lineNumber + "]: " + log + "\n");
            }
            catch
            {

            }

        }
        /// <summary>
        /// Write a log and show to user in the <see cref="RichTextBox"/> if had been set
        /// </summary>
        /// <param name="log"></param>
        /// <param name="color"></param>
        public virtual void WriteLog(string log, [CallerLineNumber] int lineNumber = 0, [CallerMemberName] string caller = null)
        {
            if (LogTextBox != null)
            {
                LogTextBox.Invoke((MethodInvoker)delegate
                {
                    LogTextBox.SelectionColor = LogTextBox.ForeColor;
                    LogTextBox.AppendText("[" + DateTime.Now.ToLongTimeString() + "]: " + log + "\n");
                });
            }
            else
            {
                Console.WriteLine(log);
            }
            WritePrivateLog(log, lineNumber, caller);
        }

        public virtual void WritePrivateLog(string log, [CallerLineNumber] int lineNumber = 0, [CallerMemberName] string caller = null)
        {
            if (!Directory.Exists(logfilepath.Remove(logfilepath.LastIndexOf('\\'))))
            {
                Directory.CreateDirectory(logfilepath.Remove(logfilepath.LastIndexOf('\\')));
            }
            File.AppendAllText(logfilepath, "[" + DateTime.Now.ToLongTimeString() + "]: [" + caller + "|" + lineNumber + "]: " + log + "\n");
        }
    }
}
