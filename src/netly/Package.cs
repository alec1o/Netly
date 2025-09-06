using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Byter;

namespace Netly
{
    public class Package
    {
        private static readonly IList<byte> Prefix = "NETLY".GetBytes(Encoding.ASCII);
        private const int MinNameSize = 1, MinBodySize = 1, NameSize = sizeof(int), MaxNameSize = 1024;
        public int Count => _segments.Sum(x => x.Count);
        private readonly LinkedList<IList<byte>> _segments;
        public IEnumerable<IList<byte>> Segments { get; }

        private Package()
        {
            _segments = new LinkedList<IList<byte>>();
            Segments = _segments;
        }

        public void Clear()
        {
            _segments.Clear();
        }

        public static Package New(IList<byte> body)
        {
            if (body.Count < MinBodySize) throw new ArgumentOutOfRangeException(nameof(MinBodySize));
            
            var package = new Package();
            
            package._segments.AddLast(body);
            
            return package;
        }

        public static Package NewMessage(string name, IList<byte> body)
        {
            if (name.Length < MinNameSize) throw new ArgumentOutOfRangeException(nameof(MinNameSize));
            if (name.Length > MaxNameSize) throw new ArgumentOutOfRangeException(nameof(MaxNameSize));
            if (body.Count < MinBodySize) throw new ArgumentOutOfRangeException(nameof(MinBodySize));

            var bytesOfName = name.GetBytes(Encoding.UTF8);
            var package = new Package();

            package._segments.AddLast(Prefix);
            package._segments.AddLast(BitConverter.GetBytes(bytesOfName.Length));
            package._segments.AddLast(bytesOfName);
            package._segments.AddLast(body);

            return package;
        }

        public static bool ParseMessage(List<byte> bytes, out string name, out IList<byte> body)
        {
            body = null;
            name = null;


            // verify min bytes length
            if (bytes.Count < MinNameSize + MinBodySize + NameSize + Prefix.Count) return false;

            // initialize index/offset
            var offset = 0;

            // verify prefix
            foreach (var value in Prefix)
            {
                if (value != bytes[offset]) return false;
                offset++;
            }

            // get name size
            var size = BitConverter.ToInt32(bytes.GetRange(offset, NameSize).ToArray(), 0);
            offset += NameSize;

            // verify name size
            if (size < MinNameSize || size > MaxNameSize) return false;

            // get name
            name = Encoding.UTF8.GetString(bytes.GetRange(offset, size).ToArray());
            offset += size;

            // get buffer
            bytes.RemoveRange(0, offset); // removed header buffer
            body = bytes;

            return true;
        }
    }
}