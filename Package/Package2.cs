using System;
using System.Collections.Generic;
using System.Linq;

namespace Zenet.Package
{
    public static class Package2
    {
        /// <summary>
        /// *GET Is: GetPack.cs
        /// </summary>
        public class GetData
        {
            private int index;
            private readonly byte[] data;
            private readonly Encode encode;
            public bool ContainError { get; private set; }

            public GetData(byte[] value, Encode encode = Encode.UTF8)
            {
                this.index = 0;
                this.data = value;
                this.encode = encode;
            }

            public bool Bool()
            {
                try
                {
                    var value = BitConverter.ToBoolean(data, index);
                    index += sizeof(bool);
                    return value;
                }
                catch
                {
                    ContainError = true;
                    return false;
                }
            }

            public byte[] Bytes()
            {
                try
                {
                    var length = Int();
                    var value = new byte[length];
                    Buffer.BlockCopy(data, index, value, 0, length);

                    index += value.Length;

                    return value;
                }
                catch
                {
                    ContainError = true;
                    return null;
                }
            }

            public float Float()
            {
                try
                {
                    var value = BitConverter.ToSingle(data, index);
                    index += sizeof(float);
                    return value;
                }
                catch
                {
                    ContainError = true;
                    return -1;
                }
            }

            public int Int()
            {
                try
                {
                    var value = BitConverter.ToInt32(data, index);
                    index += sizeof(int);
                    return value;
                }
                catch
                {
                    ContainError = true;
                    return -1;
                }
            }

            public long Long()
            {
                try
                {
                    var value = BitConverter.ToInt64(data, index);
                    index += sizeof(long);
                    return value;
                }
                catch
                {
                    ContainError = true;
                    return -1;
                }
            }

            public short Short()
            {
                try
                {
                    var value = BitConverter.ToInt16(data, index);
                    index += sizeof(short);
                    return value;
                }
                catch
                {
                    ContainError = true;
                    return -1;
                }
            }

            public string String()
            {
                try
                {
                    var data = Bytes();
                    var text = Encoding2.String(data, encode);

                    if (string.IsNullOrEmpty(text))
                    {
                        ContainError = true;
                        return null;
                    }

                    return text;
                }
                catch
                {
                    ContainError = true;
                    return null;
                }

            }

            public Vec2 Vec2()
            {
                try
                {
                    var bytes = Bytes();

                    var vec2 = Package.Vec2.ToVec2(bytes);

                    if (vec2 == null)
                    {
                        ContainError = true;
                        return null;
                    }

                    return vec2;
                }
                catch
                {
                    ContainError = true;
                    return null;
                }
            }

            public Vec3 Vec3()
            {
                try
                {
                    var bytes = Bytes();

                    var vec3 = Package.Vec3.ToVec3(bytes);

                    if (vec3 == null)
                    {
                        ContainError = true;
                        return null;
                    }

                    return vec3;
                }
                catch
                {
                    ContainError = true;
                    return null;
                }
            }
        }

        /// <summary>
        /// *SET Is: SetPack.cs
        /// </summary>
        public class SetData
        {
            private readonly Encode encode;
            private readonly List<byte[]> list;
            public byte[] Data => list.SelectMany(bytes => bytes).ToArray();

            public SetData(Encode encode = Encode.UTF8)
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
                Bytes(Encoding2.Bytes(value, encode));
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
}
