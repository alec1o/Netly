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

        public static byte[] ToBytes(Vec3 vec3)
        {
            if (vec3 == null) return null;

            var x = BitConverter.GetBytes(vec3.X);
            var y = BitConverter.GetBytes(vec3.Y);
            var z = BitConverter.GetBytes(vec3.Z);

            return Concat.Bytes(x, y, z);
        }

        public static Vec3 ToVec3(byte[] vec3)
        {
            if (vec3 == null || vec3.Length < (sizeof(float) * 3)) return null;

            try
            {
                var x = BitConverter.ToSingle(vec3, sizeof(float) * 0);
                var y = BitConverter.ToSingle(vec3, sizeof(float) * 1);
                var z = BitConverter.ToSingle(vec3, sizeof(float) * 2);

                return new Vec3(x, y, z);
            }
            catch { return null; }
        }
    }
}