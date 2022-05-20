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

        public void Add(byte value)
        {
            throw new NotImplementedException();
        }

        public void Add(int value)
        {
            throw new NotImplementedException();
        }

        public void Add(uint value)
        {
            throw new NotImplementedException();
        }

        public void Add(short value)
        {
            throw new NotImplementedException();
        }

        public void Add(ushort value)
        {
            throw new NotImplementedException();
        }

        public void Add(long value)
        {
            throw new NotImplementedException();
        }

        public void Add(ulong value)
        {
            throw new NotImplementedException();
        }

        public void Add(float value)
        {
            throw new NotImplementedException();
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
            throw new NotImplementedException();
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

        public uint GetUInt()
        {
            throw new NotImplementedException();
        }

        public ulong GetULong()
        {
            throw new NotImplementedException();
        }

        public ushort GetUShort()
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