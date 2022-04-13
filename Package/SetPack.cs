using System;
using System.Collections.Generic;
using System.Linq;

namespace Zenet.Package
{
    public class SetPack
    {
        private readonly Encode encode;
        private readonly List<byte[]> list;
        public byte[] Data => list.SelectMany(bytes => bytes).ToArray();

        public SetPack(Encode encode = Encode.UTF8)
        {
            this.encode = encode;
            list = new List<byte[]>();
        }

        public void Bool(bool value)
        {
            list.Add(BitConverter.GetBytes(value));
        }

        public void Bytes(byte[] value)
        {
            if (value == null || value.Length <= 0) return;

            list.Add(BitConverter.GetBytes(value.Length));
            list.Add(value);
        }

        public void Float(float value)
        {
            list.Add(BitConverter.GetBytes(value));
        }

        public void Int(int value)
        {
            list.Add(BitConverter.GetBytes(value));
        }

        public void Long(long value)
        {
            list.Add(BitConverter.GetBytes(value));
        }

        public void Short(short value)
        {
            list.Add(BitConverter.GetBytes(value));
        }

        public void String(string value)
        {
            Bytes(Encoding.Bytes(value, encode));
        }

        public void Vec2(Vec2 value)
        {
            Bytes(Package.Vec2.ToBytes(value));
        }

        public void Vec3(Vec3 value)
        {
            Bytes(Package.Vec3.ToBytes(value));
        }
    }
}