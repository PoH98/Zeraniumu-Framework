using System.Threading;

namespace Zeraniumu
{
    public class Delay
    {
        public static void Wait(int interval)
        {
            Thread.Sleep(interval);
        }
    }
}
