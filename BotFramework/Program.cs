using Microsoft.CSharp;
using System;
using System.CodeDom.Compiler;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;

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
            string scriptfilePath = string.Empty;
            try
            {
                scriptfilePath = args[0];
            }
            catch
            {
                Console.WriteLine("To use this program, please use arguments with this format in cmd: \n Zeraniumu <scriptpath>");
                Console.ReadLine();
                Environment.Exit(0);
            }
            FileWatcher.CreateFileWatcher(scriptfilePath);
            var watch = Stopwatch.StartNew();
            ScriptParser parser = new ScriptParser(scriptfilePath);
            var assembly = parser.Compile();
            watch.Stop();
            Console.WriteLine("File compiled in " + watch.ElapsedMilliseconds + " ms");
            try
            {
                assembly.GetType("Script.Executer").GetMethod("Run").Invoke(null, null);
            }
            catch(Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine(ex.ToString());
                Console.ForegroundColor = ConsoleColor.Gray;
            }

            Console.WriteLine("Script executed complete!");
            Console.ReadKey();
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
        string script;
        CSharpCodeProvider provider = new CSharpCodeProvider();
        CompilerParameters parameters = new CompilerParameters();
        public ScriptParser(string scriptPath)
        {
            // Reference to System.Drawing library
            parameters.ReferencedAssemblies.Add("System.Drawing.dll");
            parameters.ReferencedAssemblies.Add("System.dll");
            parameters.ReferencedAssemblies.Add(Application.ExecutablePath);
            parameters.ReferencedAssemblies.Add("System.Windows.Forms.dll");
            parameters.ReferencedAssemblies.Add("System.IO.dll");
            parameters.ReferencedAssemblies.Add("System.Linq.dll");
            // True - memory generation, false - external file generation
            parameters.GenerateInMemory = true;
            // True - exe file generation, false - dll file generation
            parameters.GenerateExecutable = false;
            script = "using System;\nusing System.IO;\nusing Zeraniumu;\nusing System.Drawing;\nusing System.Linq;\nnamespace Script{\npublic class Executer{\n" + ReadScript(scriptPath) + "}}";
        }

        public Assembly Compile()
        {
            CompilerResults results = provider.CompileAssemblyFromSource(parameters, script);
            if (results.Errors.HasErrors)
            {
                StringBuilder sb = new StringBuilder();

                foreach (CompilerError error in results.Errors)
                {
                    sb.AppendLine(String.Format("Error ({0}): {1} at line {2} and {3}", error.ErrorNumber, error.ErrorText, error.Line - 6, error.Column));
                }

                Console.WriteLine(sb.ToString());
                Console.ReadLine();
                Environment.Exit(0);
            }
            return results.CompiledAssembly;
        }

        private string ReadScript(string path)
        {
            var script = File.ReadAllLines(path);
            StringBuilder sb = new StringBuilder();
            foreach(var line in script)
            {
                sb.AppendLine(line);
                if(line.Trim().ToLower().StartsWith("@include"))
                {
                    var file = line.Trim().ToLower().Replace("@include", "").Trim();
                    sb.Replace(line, ReadScript(file));
                }
            }
            return sb.ToString();
        }
    }
}
