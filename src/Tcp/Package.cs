using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Netly.Core
{
    public class Package
    {
        public static int MAX_SIZE = (1024 * 8);
        private int count = 0;
        private List<byte> bytes = new List<byte>();
        private EventHandler<byte[]> onOutputHandler;


        public void Output(Action<byte[]> callback)
        {
            onOutputHandler += (_, data) => callback?.Invoke(data);
        }
    }
}
