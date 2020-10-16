using System;
using System.Collections.Generic;
using System.Drawing.Text;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Zeraniumu
{
    /// <summary>
    /// Configuration of the bot
    /// </summary>
    public class Config
    {
        private static Config _instance;
        /// <summary>
        /// Get the singleton instance
        /// </summary>
        /// <returns></returns>
        public static Config GetInstance()
        {
            if(_instance == null)
            {
                _instance = new Config();
            }
            return _instance;
        }
        /// <summary>
        /// Get the singleton instance with config path
        /// </summary>
        /// <param name="configPath"></param>
        /// <returns></returns>
        internal static Config GetInstance(string configPath)
        {
            if (_instance == null)
            {
                _instance = new Config(configPath);
            }
            return _instance;
        }

        internal string ConfigPath;
        private Config()
        {
            ConfigPath = "Profile\\Bot";
            if (!Directory.Exists(ConfigPath))
            {
                Directory.CreateDirectory(ConfigPath);
            }
            if (!File.Exists(Path.Combine(ConfigPath, "config.ini")))
            {
                File.WriteAllText(Path.Combine(ConfigPath, "config.ini"), "[Emulator]");
            }
        }

        private Config(string configPath)
        {
            ConfigPath = "Profile\\" + configPath;
            if (!Directory.Exists(ConfigPath))
            {
                Directory.CreateDirectory(ConfigPath);
            }
            if (!File.Exists(Path.Combine(ConfigPath, "config.ini")))
            {
                File.WriteAllText(Path.Combine(ConfigPath, "config.ini"), "[Emulator]");
            }
        }
        /// <summary>
        /// The IniFile
        /// </summary>
        public IniFile IniFile { get; private set; }
        /// <summary>
        /// Read configurations
        /// </summary>
        public void ReadConfig()
        {
            IniFile = new IniFile();
            IniFile.FromString(File.ReadAllText(Path.Combine(ConfigPath, "config.ini")));
        }
        /// <summary>
        /// Save the current configurations
        /// </summary>
        public void SaveConfig()
        {
            File.WriteAllText(Path.Combine(ConfigPath, "config.ini"), IniFile.ToString());
        }
    }

    public class IniFile:Dictionary<string, IniData>
    {
        internal IniFile()
        {

        }
        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            foreach(var data in this)
            {
                sb.AppendLine("[" + data.Key + "]");
                foreach(var value in data.Value)
                {
                    sb.AppendLine(value.Key + "=" + value.Value);
                }
            }
            return sb.ToString();
        }
        
        public IniFile FromString(string text)
        {
            IniData data = null;
            foreach(var line in text.Split('\n'))
            {
                if(line.Contains("[") && line.Contains("]"))
                {
                    this.Add(line.Replace("[", "").Replace("]", ""), new IniData());
                    data = this.Last().Value;
                }
                else if (line.Contains("="))
                {
                    var key = line.Split('=')[0];
                    var value = line.Remove(0, key.Length + 1);
                    data.Add(key, value);
                }
            }
            return this;
        }
    }

    public class IniData:Dictionary<string, string>
    {
        internal IniData()
        {

        }
    }
}
