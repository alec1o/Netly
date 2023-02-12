using System;
using System.Collections.Generic;
using System.Linq;

namespace Netly.Core
{
    public static class BufferParser
    {
        public static byte[] SetPrefix(ref byte[] buffer)
        {
            List<byte[]> result = new List<byte[]> { BitConverter.GetBytes((int)(buffer.Length)), buffer };

            return result.SelectMany(x => x).ToArray();
        }

        public static List<byte[]> GetMessages(ref byte[] buffer)
        {
            int index = 0;
            List<byte[]> messages = new List<byte[]>();

            while (index < buffer.Length)
            {
                int size = BitConverter.ToInt32(buffer, index);
                index += sizeof(int);

                byte[] message = new byte[size];
                Array.Copy(buffer, index, message, 0, message.Length);
                index += message.Length;

                messages.Add(message);
            }

            return messages;
        }
    }
}
