using System.Threading;

namespace BotFramework
{
    public interface IScript
    {
        void Run();
    }
    public class Script
    {
        private IScript script;
        private Thread t;
        public Script(IScript script)
        {
            this.script = script;
        }

        public void Start()
        {
            try
            {
                if (t != null && t.IsAlive)
                {
                    return;
                }
                t = new Thread(script.Run);
                t.IsBackground = true;
                t.Start();
            }
            catch
            {

            }

        }

        public void Stop()
        {
            try
            {
                if (t != null && t.IsAlive)
                {
                    t.Abort();
                }
            }
            catch
            {

            }
        }
    }
}
