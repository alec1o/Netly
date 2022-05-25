using System;
using System.Collections.Generic;
using System.Linq;

namespace Zenet.Core
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

        public void AddByte(byte value)
        {
            AddData('b', new byte[1] { value });
        }

        public void AddInt(int value)
        {
            AddData('i', BitConverter.GetBytes(value));
        }

        public void AddShort(short value)
        {
            AddData('S', BitConverter.GetBytes(value));
        }

        public void AddLong(long value)
        {
            AddData('l', BitConverter.GetBytes(value));
        }

        public void AddFloat(float value)
        {
            AddData('f', BitConverter.GetBytes(value));
        }

        public void AddDouble(double value)
        {
            AddData('d', BitConverter.GetBytes(value));
        }

        public void AddString(string value)
        {
            byte[] data = ZEncoding.ToBytes(value, ZEncode.UTF8);
            AddData('s', ZConcat.Bytes(BitConverter.GetBytes(data.Length), data));
        }

        public void AddChar(char value)
        {
            AddData('c', BitConverter.GetBytes(value));
        }

        public void AddBool(bool value)
        {
            AddData('t', BitConverter.GetBytes(value));
        }

        public void AddBytes(byte[] value)
        {
            AddData('B', ZConcat.Bytes(BitConverter.GetBytes(value.Length), value));
        }

        public void AddVector2(ZVector2 value)
        {
            byte[] data = ZVector2.ToBytes(value);
            AddData('2', ZConcat.Bytes(BitConverter.GetBytes(data.Length), data));
        }

        public void AddVector3(ZVector3 value)
        {
            byte[] data = ZVector3.ToBytes(value);
            AddData('3', ZConcat.Bytes(BitConverter.GetBytes(data.Length), data));
        }

        #endregion

        #region Get

        public bool GetBool()
        {
            try
            {
                if (BitConverter.ToChar(_target, _index) != 't')
                {
                    _errors.Add($"[{nameof(GetBool)}] on key [index: {_index}]");
                    return false;
                }

                _index += sizeof(char);

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
                if (BitConverter.ToChar(_target, _index) != 'b')
                {
                    _errors.Add($"[{nameof(GetByte)}] on key [index: {_index}]");
                    return 0;
                }

                _index += sizeof(char);

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
                if (BitConverter.ToChar(_target, _index) != 'B')
                {
                    _errors.Add($"[{nameof(GetBytes)}] on key [index: {_index}]");
                    return null;
                }

                _index += sizeof(char);

                int _start = BitConverter.ToInt32(_target, _index);

                _index += sizeof(int);

                byte[] data = new byte[_start];

                Buffer.BlockCopy(_target, _index, data, 0, data.Length);

                var vec2 = ZVector2.ToVec2(data);

                _index += data.Length;

                return data;
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
                if (BitConverter.ToChar(_target, _index) != 'c')
                {
                    _errors.Add($"[{nameof(GetChar)}] on key [index: {_index}]");
                    return new char();
                }

                _index += sizeof(char);

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
                if (BitConverter.ToChar(_target, _index) != 'd')
                {
                    _errors.Add($"[{nameof(GetDouble)}] on key [index: {_index}]");
                    return 0;
                }

                _index += sizeof(char);


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
                if (BitConverter.ToChar(_target, _index) != 'f')
                {
                    _errors.Add($"[{nameof(GetFloat)}] on key [index: {_index}]");
                    return 0;
                }

                _index += sizeof(char);


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
                if (BitConverter.ToChar(_target, _index) != 'i')
                {
                    _errors.Add($"[{nameof(GetInt)}] on key [index: {_index}]");
                    return 0;
                }

                _index += sizeof(char);


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
                if (BitConverter.ToChar(_target, _index) != 'l')
                {
                    _errors.Add($"[{nameof(GetLong)}] on key [index: {_index}]");
                    return 0;
                }

                _index += sizeof(char);

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
                if (BitConverter.ToChar(_target, _index) != 'S')
                {
                    _errors.Add($"[{nameof(GetShort)}] on key [index: {_index}]");
                    return 0;
                }

                _index += sizeof(char);

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
                if (BitConverter.ToChar(_target, _index) != 's')
                {
                    _errors.Add($"[{nameof(GetByte)}] on key [index: {_index}]");
                    return null;
                }

                _index += sizeof(char);

                int _start = BitConverter.ToInt32(_target, _index);

                _index += sizeof(int);

                var data = new byte[_start];

                Buffer.BlockCopy(_target, _index, data, 0, data.Length);

                var text = ZEncoding.ToString(data, ZEncode.UTF8);

                if (string.IsNullOrEmpty(text))
                {
                    _errors.Add($"[{nameof(GetString)}] on index {_index}: {nameof(string.IsNullOrEmpty)}");
                    return null;
                }

                _index += data.Length;

                return text;
            }
            catch
            {
                _errors.Add($"[{nameof(GetString)}] on index {_index}");
                return null;
            }
        }

        public ZVector2 GetVector2()
        {
            try
            {
                if (BitConverter.ToChar(_target, _index) != '2')
                {
                    _errors.Add($"[{nameof(GetVector2)}] on key [index: {_index}]");
                    return null;
                }

                _index += sizeof(char);

                int _start = BitConverter.ToInt32(_target, _index);

                _index += sizeof(int);

                byte[] data = new byte[_start];

                Buffer.BlockCopy(_target, _index, data, 0, data.Length);

                var vec2 = ZVector2.ToVec2(data);

                _index += data.Length;

                if (vec2 == null)
                {
                    _errors.Add($"[{nameof(GetVector2)}] on index {_index}: {"Vec2 not found"}");
                    return null;
                }

                return vec2;
            }
            catch
            {
                _errors.Add($"[{nameof(GetVector2)}] on index {_index}");
                return null;
            }
        }

        public ZVector3 GetVector3()
        {
            try
            {
                Console.WriteLine(BitConverter.ToChar(_target, _index));

                if (BitConverter.ToChar(_target, _index) != '3')
                {
                    _errors.Add($"[{nameof(GetVector3)}] on key [index: {_index}] 0");
                    return null;
                }

                _index += sizeof(char);

                int _start = BitConverter.ToInt32(_target, _index);

                _index += sizeof(int);

                byte[] data = new byte[_start];

                Buffer.BlockCopy(_target, _index, data, 0, data.Length);

                var vec3 = ZVector3.ToVec3(data);

                _index += data.Length;

                if (vec3 == null)
                {
                    _errors.Add($"[{nameof(GetVector3)}] on index {_index}: {"Vec2 not found"}");
                    return null;
                }

                return vec3;
            }
            catch
            {
                _errors.Add($"[{nameof(GetVector3)}] on index {_index}");
                return null;
            }
        }

        #endregion

        #region Container Manager: AddData

        /*
                            ...  Add this char even before the data ...
                    
                this will help in decoding the data because you will be sure that the data
            type being fetched is the same as the data type that is in the list. will also
                            bring improvement in coding error collection

                            _______________________________________
                            |   Type    | Pin  | Data Type        |
                            |___________|______|__________________|
                            | -Vector2  |  2   |   Vector2        |
                            | -Vector3  |  3   |   Vector3        |
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

        private void AddData(char key, byte[] data)
        {
            if (data == null || data.Length <= 0)
                throw new Exception("invalid data");

            switch (key)
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

                default: throw new Exception($"The (char) to identify the data type is invalid: [{key}]");
            }

            _data.Add(ZConcat.Bytes(BitConverter.GetBytes(key), data));
        }

        #endregion
    }
}