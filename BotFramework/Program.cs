using Microsoft.CSharp;
using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using Console = Colorful.Console;

namespace Zeraniumu
{
    class Program
    {
        const uint ENABLE_QUICK_EDIT = 0x0040;

        // STD_INPUT_HANDLE (DWORD): -10 is the standard input device.
        const int STD_INPUT_HANDLE = -10;

        [DllImport("kernel32.dll", SetLastError = true)]
        static extern IntPtr GetStdHandle(int nStdHandle);

        [DllImport("kernel32.dll")]
        static extern bool GetConsoleMode(IntPtr hConsoleHandle, out uint lpMode);

        [DllImport("kernel32.dll")]
        static extern bool SetConsoleMode(IntPtr hConsoleHandle, uint dwMode);
        static void Main(string[] args)
        {
            DisableSelect();
            Console.WriteLine("======================================================================", Color.Aqua);
            Console.WriteAscii("Zeraniumu", Color.Aqua);
            Console.WriteAscii("Framework", Color.Aqua);
            Console.WriteLine("Dev by PoH98 under GPL-3.0 License", Color.Aqua);
            Console.WriteLine("=======================================================================", Color.Aqua);
            if (args.Length < 1)
            {
                Console.WriteLine("To use this program, please use arguments with this format in cmd: \n Zeraniumu <scriptpath>\nEach scriptpath will be declared as one thread");
                Console.WriteLine("Or you can just type in your script name now, using space to split multiple of them for multi-threaded!");
                var scripts = Console.ReadLine();
                args = scripts.Split(' ');
            }
            Console.WriteLine("Press enter to stop the script!",Color.Orange);
            foreach(var scriptfilePath in args)
            {
                if (!File.Exists(scriptfilePath))
                {
                    Console.Write("[" + DateTime.Now.ToString("dd/mm/yyyy HH:mm:ss") + "] ", Color.White);
                    Console.WriteLine(scriptfilePath + " not found, unable to execute script!",Color.OrangeRed);
                    continue;
                }
                else
                {
                    Thread t = new Thread(() =>
                    {
                        try
                        {
                            FileWatcher.CreateFileWatcher(scriptfilePath);
                            var watch = Stopwatch.StartNew();
                            ScriptParser parser = new ScriptParser(scriptfilePath);
                            var assembly = parser.Compile();
                            if(assembly == null)
                            {
                                return;
                            }
                            watch.Stop();
                            Console.Write("[" + DateTime.Now.ToString("dd/mm/yyyy HH:mm:ss") + "] ", Color.White);
                            Console.WriteLine("File compiled in " + watch.ElapsedMilliseconds + " ms", Color.FromArgb(207, 3, 252));
                            assembly.GetType(scriptfilePath.Remove(scriptfilePath.IndexOf(".")) + ".Executer").GetMethod("Run").Invoke(null, null);
                        }
                        catch (Exception ex)
                        {
                            Console.Write("[" + DateTime.Now.ToString("dd/mm/yyyy HH:mm:ss") + "] ", Color.White);
                            Console.WriteLine("An error found in " + scriptfilePath, Color.Red);
                            Console.WriteLine(ex.ToString(), Color.Red);
                        }
                        Console.Write("[" + DateTime.Now.ToString("dd/mm/yyyy HH:mm:ss") + "] ", Color.White);
                        Console.WriteLine("Script " + scriptfilePath + " has been executed completed!", Color.Lime);
                    });
                    t.IsBackground = true;
                    t.Start();
                }
            }
            Console.ReadLine();
        }

        internal static bool DisableSelect()
        {

            IntPtr consoleHandle = GetStdHandle(STD_INPUT_HANDLE);

            // get current console mode
            uint consoleMode;
            if (!GetConsoleMode(consoleHandle, out consoleMode))
            {
                // ERROR: Unable to get console mode.
                return false;
            }

            // Clear the quick edit bit in the mode flags
            consoleMode &= ~ENABLE_QUICK_EDIT;

            // set the new mode
            if (!SetConsoleMode(consoleHandle, consoleMode))
            {
                // ERROR: Unable to set console mode
                return false;
            }

            return true;
        }
    }

    class ScriptParser
    {
        string script, path;
        CSharpCodeProvider provider = new CSharpCodeProvider();
        CompilerParameters parameters = new CompilerParameters();
        int skiplineCount = 7;
        public ScriptParser(string scriptPath)
        {
            path = scriptPath;
            // Reference to System.Drawing library
            parameters.ReferencedAssemblies.Add("System.Drawing.dll");
            parameters.ReferencedAssemblies.Add("System.dll");
            parameters.ReferencedAssemblies.Add(Application.ExecutablePath);
            parameters.ReferencedAssemblies.Add("System.Windows.Forms.dll");
            parameters.ReferencedAssemblies.Add("System.IO.dll");
            parameters.ReferencedAssemblies.Add("System.Linq.dll");
            parameters.ReferencedAssemblies.Add("System.Core.dll");
            // True - memory generation, false - external file generation
            parameters.GenerateInMemory = true;
            // True - exe file generation, false - dll file generation
            parameters.GenerateExecutable = false;
            script = "using System;\nusing System.Collections.Generic;\nusing System.IO;\nusing Zeraniumu;\nusing System.Drawing;\nusing System.Linq;\nnamespace "+path.Remove(path.IndexOf("."))+"{\npublic class Executer{\n" + ReadScript(scriptPath, out List<string> scriptusing) + "}}";
            foreach (var u in scriptusing)
            {
                if (u.EndsWith(";"))
                {
                    script = "using " + u + "\n" + script;
                    parameters.ReferencedAssemblies.Add(u.Replace(";", ".dll"));
                }
                else
                {
                    script = "using " + u + ";\n" + script;
                    parameters.ReferencedAssemblies.Add(u  + ".dll");
                }
            }
            skiplineCount += scriptusing.Count;
            scriptusing.Clear();
        }

        public Assembly Compile()
        {
            CompilerResults results = provider.CompileAssemblyFromSource(parameters, script);
            if (results.Errors.HasErrors)
            {
                StringBuilder sb = new StringBuilder();
                foreach (CompilerError error in results.Errors)
                {
                    sb.AppendLine(String.Format("Error ({0}): {1} at line {2} and {3} in {4}", error.ErrorNumber, error.ErrorText, error.Line - skiplineCount, error.Column, path));
                }
                Console.WriteLine(sb.ToString(), Color.Red);
                Console.Write("[" + DateTime.Now.ToString("dd/mm/yyyy HH:mm:ss") + "] ", Color.White);
                Console.WriteLine(path + " unable to continue...", Color.OrangeRed);
                return null;
            }
            return results.CompiledAssembly;
        }

        private string ReadScript(string path, out List<string> scriptusing)
        {
            scriptusing = new List<string>();
            var script = File.ReadAllLines(path);
            StringBuilder sb = new StringBuilder();
            foreach(var line in script)
            {
                sb.AppendLine(line);
                if(line.Trim().ToLower().StartsWith("@include"))
                {
                    var file = line.Trim().ToLower().Replace("@include", "").Trim();
                    sb.Replace(line, ReadScript(file, out List<string> tempList));
                    scriptusing.AddRange(tempList);
                }
                else if (line.Trim().ToLower().StartsWith("@using"))
                {
                    var file = line.Trim().Substring(6).Trim();
                    scriptusing.Add(file);
                    sb.Replace(line, "");
                }
            }
            return sb.ToString();
        }
    }
}
