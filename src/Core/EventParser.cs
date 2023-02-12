using System;
using Byter;

namespace Netly
{
    public static class EventParser
    {
        private const string KEY = "KE0://";
        public static (string name, byte[] data) Verify(byte[] buffer)
        {
            using (Reader r = new Reader(buffer))
            {
                string key = r.Read<string>();
                string name = r.Read<string>();
                byte[] data = r.Read<byte[]>();

                if (r.Success is true && key is KEY) return (name, data);

                return (null, null);
            }
        }

        public static byte[] Create(string name, byte[] data)
        {
            using (Writer w = new Writer())
            {
                w.Write(KEY);
                w.Write(name);
                w.Write(data);
                return w.GetBytes();
            }
        }

    }
}