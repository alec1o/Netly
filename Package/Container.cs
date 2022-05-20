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

        public void Add(decimal value)
        {
            throw new NotImplementedException();
        }

        public void Add(double value)
        {
            throw new NotImplementedException();
        }

        public void Add(string value)
        {
            throw new NotImplementedException();
        }

        public void Add(char value)
        {
            throw new NotImplementedException();
        }

        public void Add(bool value)
        {
            throw new NotImplementedException();
        }

        public void Add(byte[] value)
        {
            throw new NotImplementedException();
        }

        public void Add(Vec3 value)
        {
            throw new NotImplementedException();
        }

        public void Add(Vec2 value)
        {
            throw new NotImplementedException();
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
                byte _byte = _target[_index];
                _index += sizeof(byte);
                return _byte;
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

        public decimal GetDecimal()
        {
            throw new NotImplementedException();
        }

        public double GetDouble()
        {
            throw new NotImplementedException();
        }

        public float GetFloat()
        {
            throw new NotImplementedException();
        }

        public int GetInt()
        {
            throw new NotImplementedException();
        }

        public long GetLong()
        {
            throw new NotImplementedException();
        }

        public short GetShort()
        {
            throw new NotImplementedException();
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