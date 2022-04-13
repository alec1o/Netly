using System;

namespace Zenet.Package
{
    public static class Event
    {
        public static byte[] Create(string name, byte[] value, Encode encode = Encode.UTF8)
        {
            if (name == null || value == null) return null;

            var bytes = Encoding.Bytes(name, encode);
            var length = BitConverter.GetBytes(bytes.Length);

            return Concat.Bytes(length, bytes, value);
        }

        public static (string name, byte[] value) Verify(byte[] value, Encode encode = Encode.UTF8)
        {
            // [ "name size" 4 bytes] [ "name" min 1 bytes ] [ "bytes" min 1 byte ] = 6 bytes min
            if (value == null || value.Length < 6) return (null, null);
            var length = value.Length;
            var index = 0;

            try
            {
                //get "name length"
                var nameLength = BitConverter.ToInt32(value, 0);

                //verify length and update index
                if (nameLength < 1) return (null, null);
                index += sizeof(int);

                //get "name bytes"
                var nameBytes = new byte[nameLength];
                Buffer.BlockCopy(value, index, nameBytes, 0, nameLength);

                //update index
                index += nameBytes.Length;

                //get "value length"
                var valueLength = length - index;
                if (valueLength < 1) return (null, null);

                //get "value bytes"
                var myValue = new byte[valueLength];
                Buffer.BlockCopy(value, index, myValue, 0, valueLength);

                //get "name string"
                var myName = Encoding.String(nameBytes, encode);

                //verify name
                if (myName.Length < 1) return (null, null);

                return (myName, myValue);
            }
            catch
            {
                return (null, null);
            }
        }
    }
}
