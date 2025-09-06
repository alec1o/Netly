using System;
using System.Collections.Generic;
using System.Text;

namespace Netly.Packages
{
    public class Framing
    {
        public static readonly byte[] Prefix = Encoding.ASCII.GetBytes("Ny://");

        public static byte[] CreateFrame(long size)
        {
            var list = new List<byte>();
            list.AddRange(Prefix);
            list.AddRange(BitConverter.GetBytes(size));
            var bytes = list.ToArray();
            list.Clear();
            return bytes;
        }

        public static byte[] CreateMessage(string name)
        {
            var list = new List<byte>();
            list.AddRange(Prefix);
            list.AddRange(BitConverter.GetBytes(name.Length));
            var bytes = list.ToArray();
            list.Clear();
            return bytes;
        }

        public static bool NextMessage(ref LinkedList<byte[]> list, out string s)
        {
            throw new NotImplementedException();
        }

        public static bool NextFrame(ref long inSize, ref LinkedList<byte[]> inBuffer, out LinkedList<byte[]>  outBuffer)
        {
            throw new NotImplementedException();
        }
    }
}