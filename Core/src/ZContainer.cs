using System;
using System.Collections.Generic;
using System.Linq;

using Zenet.Core;

namespace Zenet.Core.Message
{
    public class ZContainer : IZContainer
    {
        #region Header

        private int _index;
        private List<byte[]> _data;
        private List<string> _errors;
        private byte[] _target;

        public int Index => _index;

        public byte[] Serialized => _data.SelectMany(bytes => bytes).ToArray();

        public byte[] Deserialize { set => _target = value; }

        public string[] Errors => _errors.ToArray();

        #endregion

        #region Init

        public ZContainer()
        {
            _index = 0;
            _data = new List<byte[]>();
            _errors = new List<string>();
            _target = null;
        }

        #endregion

        #region Add

        public void Add(byte value)
        {
            _data.Add(new byte[1] { value });
        }

        public void Add(int value)
        {
            _data.Add(BitConverter.GetBytes(value));
        }

        public void Add(short value)
        {
            _data.Add(BitConverter.GetBytes(value));
        }

        public void Add(long value)
        {
            _data.Add(BitConverter.GetBytes(value));
        }

        public void Add(float value)
        {
            _data.Add(BitConverter.GetBytes(value));
        }

        public void Add(double value)
        {
            _data.Add(BitConverter.GetBytes(value));
        }

        public void Add(string value)
        {
            Add(ZEncoding.ToBytes(value, ZEncode.UTF8));
        }

        public void Add(char value)
        {
            _data.Add(BitConverter.GetBytes(value));
        }

        public void Add(bool value)
        {
            _data.Add(BitConverter.GetBytes(value));
        }

        public void Add(byte[] value)
        {
            Add(value.Length);
            _data.Add(value);
        }

        public void Add(ZVector3 value)
        {
            Add(ZVector3.ToBytes(value));
        }

        public void Add(ZVector2 value)
        {
            Add(ZVector2.ToBytes(value));
        }

        #endregion

        #region Get

        public bool GetBool()
        {
            try
            {
                var value = BitConverter.ToBoolean(_target, _index);
                _index += sizeof(bool);
                return value;
            }
            catch
            {
                _errors.Add($"[{nameof(GetBool)}] on index {_index}");
                return false;
            }
        }

        public byte GetByte()
        {
            try
            {
                byte value = _target[_index];
                _index += sizeof(byte);
                return value;
            }
            catch
            {
                _errors.Add($"[{nameof(GetByte)}] on index {_index}");
                return 0;
            }
        }

        public byte[] GetBytes()
        {
            try
            {
                var length = GetInt();
                var value = new byte[length];
                Buffer.BlockCopy(_target, _index, value, 0, length);

                _index += value.Length;

                return value;
            }
            catch
            {
                _errors.Add($"[{nameof(GetBytes)}] on index {_index}");
                return null;
            }
        }

        public char GetChar()
        {
            try
            {
                char value = BitConverter.ToChar(_target, _index);
                _index += sizeof(char);
                return value;
            }
            catch
            {
                _errors.Add($"[{nameof(GetChar)}] on index {_index}");
                return new char();
            }
        }

        public double GetDouble()
        {
            try
            {
                double value = BitConverter.ToDouble(_target, _index);
                _index += sizeof(double);
                return value;
            }
            catch
            {
                _errors.Add($"[{nameof(GetDouble)}] on index {_index}");
                return 0;
            }
        }

        public float GetFloat()
        {
            try
            {
                float value = BitConverter.ToSingle(_target, _index);
                _index += sizeof(float);
                return value;
            }
            catch
            {
                _errors.Add($"[{nameof(GetFloat)}] on index {_index}");
                return 0;
            }
        }

        public int GetInt()
        {
            try
            {
                int value = BitConverter.ToInt32(_target, _index);
                _index += sizeof(int);
                return value;
            }
            catch
            {
                _errors.Add($"[{nameof(GetInt)}] on index {_index}");
                return 0;
            }
        }

        public long GetLong()
        {
            try
            {
                var value = BitConverter.ToInt64(_target, _index);
                _index += sizeof(long);
                return value;
            }
            catch
            {
                _errors.Add($"[{nameof(GetLong)}] on index {_index}");
                return 0;
            }
        }

        public short GetShort()
        {
            try
            {
                short value = BitConverter.ToInt16(_target, _index);
                _index += sizeof(short);
                return value;
            }
            catch
            {
                _errors.Add($"[{nameof(GetShort)}] on index {_index}");
                return 0;
            }
        }

        public string GetString()
        {
            try
            {
                var data = GetBytes();
                var text = ZEncoding.ToString(data, ZEncode.UTF8);

                if (string.IsNullOrEmpty(text))
                {
                    _errors.Add($"[{nameof(GetString)}] on index {_index}: {nameof(string.IsNullOrEmpty)}");
                    return null;
                }

                return text;
            }
            catch
            {
                _errors.Add($"[{nameof(GetString)}] on index {_index}");
                return null;
            }
        }

        public ZVector2 GetVec2()
        {
            try
            {
                var data = GetBytes();
                var vec2 = ZVector2.ToVec2(data);

                if (vec2 == null)
                {
                    _errors.Add($"[{nameof(GetVec2)}] on index {_index}: {"Vec2 not found"}");
                    return null;
                }

                return vec2;
            }
            catch
            {
                _errors.Add($"[{nameof(GetVec2)}] on index {_index}");
                return null;
            }
        }

        public ZVector3 GetVec3()
        {
            try
            {
                var data = GetBytes();
                var vec3 = ZVector3.ToVec3(data);

                if (vec3 == null)
                {
                    _errors.Add($"[{nameof(GetVec3)}] on index {_index}: {"Vec3 not found"}");
                    return null;
                }

                return vec3;
            }
            catch
            {
                _errors.Add($"[{nameof(GetVec3)}] on index {_index}");
                return null;
            }
        }

        #endregion

        #region Snippet

        /*
                            ...  Add this char even before the data ...
                    
                this will help in decoding the data because you will be sure that the data
            type being fetched is the same as the data type that is in the list. will also
                            bring improvement in coding error collection

                            _______________________________________
                            |   Type    | Pin  | Data Type        |
                            |___________|______|__________________|
                            | -Vec2     |  2   |   Vec2           |
                            | -Vec3     |  3   |   Vec3           |
                            | -Byte     |  b   |   byte           |    
                            | -Bytes    |  B   |   bytes (byte[]) |
                            | -Tongle   |  t   |   bool           |    
                            | -Double   |  d   |   double         |        
                            | -Char     |  c   |   char           |    
                            | -Float    |  f   |   float          |    
                            | -Int      |  i   |   int            |    
                            | -Long     |  l   |   long           |    
                            | -String   |  s   |   string         |        
                            | -Short    |  S   |   short          |            
                            |___________|______|__________________|
         */

        private void AddData(char type, byte[] data)
        {
            if (data == null || data.Length <= 0)
                throw new Exception("invalid data");

            switch (type)
            {
                // add Vec2
                case '2': break;

                // add Vec3
                case '3': break;

                // add byte
                case 'b': break;

                // add byte[]
                case 'B': break;

                // add bool
                case 't': break;

                // add double
                case 'd': break;

                // add char
                case 'c': break;

                // add float
                case 'f': break;

                // add int
                case 'i': break;

                // add long
                case 'l': break;

                // add string
                case 's': break;

                // add Short
                case 'S': break;

                default: throw new Exception($"The (char) to identify the data type is invalid: [{type}]");
            }

            _data.Add(ZConcat.Bytes(BitConverter.GetBytes(type), data));
        }

        #endregion
    }
}