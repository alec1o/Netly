using System;

namespace Zenet.Core
{
    public class ZVector2
    {
        public float X { get; set; }
        public float Y { get; set; }

        public ZVector2()
        {
            X = 0;
            Y = 0;
        }

        public ZVector2(float x, float y)
        {
            X = x;
            Y = y;
        }

        public static byte[] ToBytes(ZVector2 Vec2)
        {
            if (Vec2 == null) return null;

            var x = BitConverter.GetBytes(Vec2.X);
            var y = BitConverter.GetBytes(Vec2.Y);

            return ZConcat.Bytes(x, y);
        }

        public static ZVector2 ToVec2(byte[] Vec2)
        {
            if (Vec2 == null || Vec2.Length < (sizeof(float) * 2)) return null;

            try
            {
                var x = BitConverter.ToSingle(Vec2, sizeof(float) * 0);
                var y = BitConverter.ToSingle(Vec2, sizeof(float) * 1);

                return new ZVector2(x, y);
            }
            catch { return null; }
        }

        public override string ToString()
        {
            return $"X {X}, Y {Y}";
        }
    }
}