using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Zeraniumu
{
    class ImageException:Exception
    {
        private string message;
        public ImageException(string message = "")
        {
            this.message = message;
        }

        public override string Message
        {
            get
            {
                return message;
            }
        }
    }
}
