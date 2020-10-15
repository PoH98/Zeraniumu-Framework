using System.Threading;

namespace BotFramework
{
    public class Delay
    {
        public static void Wait(int interval)
        {
            Thread.Sleep(interval);
        }
    }
}
