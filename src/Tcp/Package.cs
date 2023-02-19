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
        public void Input(byte[] buffer)
        {
        }
        private void Calc()
        {
        }
        private void GetBuffer(ref byte[] buffer)
        {
            for (int i = 0; i < buffer.Length; i++)
            {
                buffer[i] = bytes[i];
            }

            bytes.RemoveRange(0, buffer.Length);
        }

        private int GetCount()
        {
            byte[] buffer = new byte[sizeof(Int32)];

            for (int i = 0; i < sizeof(Int32); i++)
            {
                buffer[i] = bytes[i];
            }

            bytes.RemoveRange(0, sizeof(Int32));

            int value = BitConverter.ToInt32(buffer, 0);

            return (value > MAX_SIZE) ? 0 : value;
        }
    }
}
