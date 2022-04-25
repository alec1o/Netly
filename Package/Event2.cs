using System;

namespace Zenet.Package
{
    public static class Event2
    {
        private readonly static byte[] KEY_BTS = Encoding2.Bytes("ZENET:::", Encode.ASCII);
        private readonly static string KEY_STR = Encoding2.String(KEY_BTS, Encode.ASCII);

        public static byte[] Create(string name, byte[] value, Encode encode = Encode.UTF8)
        {
            if (name == null || value == null) return null;

            var bytes = Encoding2.Bytes(name, encode);
            var length = BitConverter.GetBytes(bytes.Length);

            return Concat.Bytes(KEY_BTS, length, bytes, value);
        }

        public static (string name, byte[] value) Verify(byte[] value, Encode encode = Encode.UTF8)
        {
            // [" int KEY bytes"][ "name size" 4 bytes] [ "name" min 1 bytes ] [ "bytes" min 1 byte ] = 6 bytes min
            if (value == null || value.Length < 6 + KEY_BTS.Length) return (null, null);
            var length = value.Length;
            var index = 0;

            try
            {
                #region KEY OF DATA
                //WARNING: SE REMOVER PODE CAUSAR VAZAMENTO DE MEMORIA

                //get KEY                
                var key = new byte[KEY_BTS.Length];
                Array.Copy(value, key, key.Length);

                //verify key
                var keyName = Encoding2.String(key, Encode.ASCII);
                if (keyName != KEY_STR) return (null, null);

                //update index, add key size
                index += KEY_BTS.Length;
                #endregion

                //get "name length"
                var nameLength = BitConverter.ToInt32(value, index);

                //verify length and update index
                if (nameLength < 1) return (null, null);
                index += sizeof(int);  

                //get "name bytes"
                //WARNING: SE REMOVER PODE CAUSAR VAZAMENTO DE MEMORIA
                if(nameLength > length - index)
                {
                    return (null, null);
                }

                var nameBytes = new byte[nameLength];
                Array.Copy(value, index, nameBytes, 0, nameLength);

                //update index
                index += nameBytes.Length;

                //get "value length"
                var valueLength = length - index;
                if (valueLength < 1) return (null, null);

                //get "value bytes"
                var myValue = new byte[valueLength];
                Array.Copy(value, index, myValue, 0, valueLength);

                //get "name string"
                var myName = Encoding2.String(nameBytes, encode);

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
