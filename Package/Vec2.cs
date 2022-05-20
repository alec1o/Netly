using System;

namespace Zenet.Package
{
    public class Vec2
    {
        public float X { get; set; }
        public float Y { get; set; }

        public Vec2()
        {
            X = 0;
            Y = 0;
        }

        public Vec2(float x, float y)
        {
            X = x;
            Y = y;
        }

        public static byte[] ToBytes(Vec2 Vec2)
        {
            if (Vec2 == null) return null;

            var x = BitConverter.GetBytes(Vec2.X);
            var y = BitConverter.GetBytes(Vec2.Y);

            return Concat.Bytes(x, y);
        }

        public static Vec2 ToVec2(byte[] Vec2)
        {
            if (Vec2 == null || Vec2.Length < (sizeof(float) * 2)) return null;

            try
            {
                var x = BitConverter.ToSingle(Vec2, sizeof(float) * 0);
                var y = BitConverter.ToSingle(Vec2, sizeof(float) * 1);

                return new Vec2(x, y);
            }
            catch { return null; }
        }

        public override string ToString()
        {
            return $"X {X}, Y {Y}";
        }
    }
}