using System.Threading;

namespace Zeraniumu
{
    public interface IScript
    {
        void Run();
    }
    public class Script
    {
        protected IScript script;
        private Thread t;
        public Script(IScript script)
        {
            this.script = script;
        }

        public virtual void Start()
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

        public virtual void Stop()
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
