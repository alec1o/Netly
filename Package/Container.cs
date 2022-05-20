using System;
using System.Collections.Generic;
using System.Linq;

namespace Zenet.Package
{
    public class Container : IContainer
    {
        private int _index;
        private List<byte[]> _data;
        private List<string> _errors;
        private byte[] _target;

        public int Index => _index;

        public byte[] Serialized => _data.SelectMany(bytes => bytes).ToArray();

        public byte[] Deserialize { set => _target = value; }

        public string[] Errors => _errors.ToArray();

        public Container()
        {
            _index = 0;
            _data = new List<byte[]>();
            _errors = new List<string>();
            _target = null;
        }

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
            Add(Encoding2.Bytes(value, Encode.UTF8));
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

        public void Add(Vec3 value)
        {
            throw new NotImplementedException();
        }

        public void Add(Vec2 value)
        {
            Add(Vec2.ToBytes(value));
        }

        public void Add(DateTime value)
        {
            throw new NotImplementedException();
        }

        public bool GetBool()
        {
            throw new NotImplementedException();
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
            throw new NotImplementedException();
        }

        public char GetChar()
        {
            throw new NotImplementedException();
        }

        public DateTime GetDateTime()
        {
            throw new NotImplementedException();
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
            throw new NotImplementedException();
        }

        public Vec2 GetVec2()
        {
            throw new NotImplementedException();
        }

        public Vec3 GetVec3()
        {
            throw new NotImplementedException();
        }
    }
}