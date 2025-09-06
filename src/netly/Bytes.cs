using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace Netly
{
    public class Bytes
    {

        public byte this[long index]
        {
            get { return default; }
            set
            {
                
            }
        }


        public static implicit operator Bytes(byte[] value)
        {
            return default;
        }

        public static implicit operator byte[](Bytes value)
        {
            return default;
        }

        public static implicit operator Bytes(ArraySegment<byte> value)
        {
            return default;
        }

        public static implicit operator ArraySegment<byte>(Bytes value)
        {
            return default;
        }

        public void Dispose()
        {
        }
    }
}