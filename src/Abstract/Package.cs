using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Netly.Abstract
{
    public class Package
    {
        public static int MAX_SIZE = (1024 /* 1KB */ * 1024 /* 1MB */) * 8 /* 8MB */;
        private int count = 0;
        private List<byte> bytes = new List<byte>();
        private EventHandler<byte[]> onOutputHandler;

        public static byte[] Create(byte[] buffer) => Create(ref buffer);
        public static byte[] Create(ref byte[] buffer)
        {
            List<byte[]> result = new List<byte[]> { BitConverter.GetBytes(buffer.Length), buffer };

            return result.SelectMany(x => x).ToArray();
        }


        public void Output(Action<byte[]> callback)
        {
            onOutputHandler += (_, data) => callback?.Invoke(data);
        }

        public void Input(byte[] buffer)
        {
            foreach (byte b in buffer) bytes.Add(b);
            Calc();
        }

        private void Calc()
        {
            if (count == 0)
            {
                if (bytes.Count < sizeof(int)) return;

                count = GetCount();

                if (count == 0) return;
            }

            if (bytes.Count < count) return;

            byte[] buffer = new byte[count];

            GetBuffer(ref buffer);

            onOutputHandler?.Invoke(null, buffer);

            count = 0;

            Calc();
            return;
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
            byte[] buffer = new byte[sizeof(int)];

            for (int i = 0; i < sizeof(int); i++)
            {
                buffer[i] = bytes[i];
            }

            bytes.RemoveRange(0, sizeof(int));

            int value = BitConverter.ToInt32(buffer, 0);

            return value > MAX_SIZE ? 0 : value;
        }
    }
}
