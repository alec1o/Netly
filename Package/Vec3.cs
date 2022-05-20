using System;

namespace Zenet.Package
{
    public class Vec3
    {
        public float X { get; set; }
        public float Y { get; set; }
        public float Z { get; set; }

        public Vec3()
        {
            X = 0;
            Y = 0;
            Z = 0;
        }

        public Vec3(float x, float y, float z)
        {
            X = x;
            Y = y;
            Z = z;
        }

        public static byte[] ToBytes(Vec3 value)
        {
            if (value == null) return null;

            var x = BitConverter.GetBytes(value.X);
            var y = BitConverter.GetBytes(value.Y);
            var z = BitConverter.GetBytes(value.Z);

            return Concat.Bytes(x, y, z);
        }

        public static Vec3 ToVec3(byte[] value)
        {
            if (value == null || value.Length < (sizeof(float) * 3)) return null;

            try
            {
                var x = BitConverter.ToSingle(value, sizeof(float) * 0);
                var y = BitConverter.ToSingle(value, sizeof(float) * 1);
                var z = BitConverter.ToSingle(value, sizeof(float) * 2);

                return new Vec3(x, y, z);
            }
            catch { return null; }
        }

        public override string ToString()
        {
            return $"X {X}, Y {Y}, Z {Z}";
        }
    }
}