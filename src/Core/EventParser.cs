using System;
using Byter;

namespace Netly
{
    public static class EventParser
    {
        private const string KEY = "KE0://";
        public static (string name, byte[] data) Verify(byte[] buffer)
        {
            using (Writer w = new Writer(buffer))
            {
                using (Reader r = new Reader(w))
                {
                    string key = r.Read<string>();

                    if (!r.Success || !string.Equals(key, KEY)) return (null, null);

                    string name = r.Read<string>();
                    byte[] data = r.Read<byte[]>();

                    if (!r.Success) return (null, null);

                    return (name, data);
                }
            }
        }

        public static byte[] Create(string name, byte[] data)
        {
            if (string.IsNullOrWhiteSpace(name)) throw new ArgumentNullException(nameof(name));
            if (data == null || data.Length <= 0) throw new ArgumentNullException(nameof(data));

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