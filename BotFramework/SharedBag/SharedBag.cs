using System.Collections.Concurrent;

namespace Zeraniumu
{
    public class SharedBag
    {
        private static readonly ConcurrentDictionary<string, dynamic> data = new ConcurrentDictionary<string, dynamic>();

        public static T GetValue<T>(string key)
        {
            if(data.TryGetValue(key, out dynamic value))
            {
                return value;
            }
            else
            {
                return default;
            }
        }

        public static void SaveValue(string key, object value)
        {
            if (!data.ContainsKey(key))
            {
                data.TryAdd(key, value);
            }
            else
            {
                data[key] = value;
            }
        }

        public static T DeleteValue<T>(string key)
        {
            if (data.ContainsKey(key))
            {
                if(data.TryRemove(key, out dynamic obj))
                {
                    return obj;
                }
                else
                {
                    return default;
                }
            }
            else
            {
                return default;
            }
        }
    }
}