using System;
using System.Collections.Generic;

namespace Netly.Core
{
    /// <summary>
    /// Encodes and decodes data in proprietary format (Dict)
    /// </summary>
    public class Dict
    {
        /// <summary>
        /// Current Index (From Serialization)
        /// </summary>
        public int Position { get; set; } = 0;

        public const string TypeAccpted = "bool, byte, byte[], char, double, float, int, long, short, string";

        private byte[] buffer = new byte[0];
        private List<byte[]> Save { get; set; } = new List<byte[]>();

        /// <summary>
        /// Add the data to be deserialized (Dict format)
        /// </summary>
        /// <param name="value">Data/Value/Buffer (Dict format)</param>
        public void SetBytes(byte[] value)
        {
            if (value == null) return;

            buffer = new byte[value.Length];
            Buffer.BlockCopy(value, 0, buffer, 0, buffer.Length);
        }

        /// <summary>
        /// Take the serialized data (Dict format)
        /// </summary>
        /// <returns></returns>
        public byte[] GetBytes()
        {
            return Concat.Bytes(Save.ToArray());
        }

        
        private char GetChar(Type value)
        {
            if (value == typeof(bool))   /* bool   */ return 'a';
            if (value == typeof(byte))   /* byte   */ return 'b';
            if (value == typeof(byte[])) /* bytes  */ return 'c';
            if (value == typeof(char))   /* char   */ return 'd';
            if (value == typeof(double)) /* double */ return 'e';
            if (value == typeof(float))  /* float  */ return 'f';
            if (value == typeof(int))    /* int    */ return 'g';
            if (value == typeof(long))   /* long   */ return 'h';
            if (value == typeof(short))  /* short  */ return 'i';
            if (value == typeof(string)) /* string */ return 'j';
            /* unset  */
            return default;
        }

        /// <summary>
        /// Add data
        /// </summary>
        /// <typeparam name="T">Type</typeparam>
        /// <param name="value">Data</param>
        /// <returns>Returns true if the data was successfully added</returns>
        public bool Add<T>(T value)
        {
            if (value == null) return false;

            var type = GetChar(typeof(T));
            var prefix = BitConverter.GetBytes(type);
            var data = new byte[0];
            var amount = new byte[0];
            var target = (object)value;

            var result = Compile();
            
            if (result)
            {
                Save.Add(data);
            }

            return result;

            bool Compile()
            {
                switch (type)
                {
                    case 'a':
                        /* bool */
                        data = Concat.Bytes(prefix, BitConverter.GetBytes((bool)target));
                        return true;
                    case 'b':
                        /* byte */
                        data = Concat.Bytes(prefix, new byte[] { (byte)target });
                        return true;
                    case 'c':
                        /* bytes */
                        amount = (byte[])target;
                        data = Concat.Bytes(prefix, BitConverter.GetBytes(amount.Length), amount);
                        return true;
                    case 'd':
                        /* char */
                        data = Concat.Bytes(prefix, BitConverter.GetBytes((char)target));
                        return true;
                    case 'e':
                        /* double */
                        data = Concat.Bytes(prefix, BitConverter.GetBytes((double)target));
                        return true;
                    case 'f':
                        /* float */
                        data = Concat.Bytes(prefix, BitConverter.GetBytes((float)target));
                        return true;
                    case 'g':
                        /* int */
                        data = Concat.Bytes(prefix, BitConverter.GetBytes((int)target));
                        return true;
                    case 'h':
                        /* long */
                        data = Concat.Bytes(prefix, BitConverter.GetBytes((long)target));
                        return true;
                    case 'i':
                        /* short */
                        data = Concat.Bytes(prefix, BitConverter.GetBytes((short)target));
                        return true;
                    case 'j':
                        /* string */
                        amount = Encode.GetBytes((string)target, Encode.Mode.UTF8);
                        data = Concat.Bytes(prefix, BitConverter.GetBytes(amount.Length), amount);
                        return true;
                        /* unset */
                    default:
                        return false;
                }
            }
        }

        /// <summary>
        /// Serialized data collection
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns>Returns the collected data or default value if possible</returns>
        /// <exception cref="Exception">Unable to load data (invalid data)</exception>
        /// <exception cref="ArgumentException">Only data types are accepted: bool, byte, byte[], char, double, float, int, long, short, string</exception>
        public T Get<T>()
        {
            byte[] values = buffer;
            object value = new object();
            char prefix = GetChar(typeof(T));
            int size = 0;
            int index = (Position <= 0) ? 0 : Position;
            char _prefix = default;
            dynamic _value = new object();

            if(prefix == default)
            {
                throw new ArgumentException($"Only data types are accepted:\n{TypeAccpted}");
            }

            try
            {
                bool result = Compile();

                if (result)
                {
                    Position = index;
                }

                return (T)value;
            }
            catch (Exception e)
            {
                throw new Exception($"Unable to load data (invalid data)\n{e}");
            }

            bool Compile()
            {
                switch (prefix)
                {
                    case 'a':
                        /* bool */
                        
                        _prefix = BitConverter.ToChar(values, index);
                        index += sizeof(char);
                        if (_prefix != prefix) return false;

                        _value = BitConverter.ToBoolean(values, index);
                        index += sizeof(bool);

                        value = _value;

                        return true;
                    case 'b':
                        /* byte */

                        _prefix = BitConverter.ToChar(values, index);
                        index += sizeof(char);
                        if (_prefix != prefix) return false;

                        _value = (values[index]);
                        index += sizeof(byte);

                        value = _value;

                        return true;
                    case 'c':
                        /* bytes */

                        _prefix = BitConverter.ToChar(values, index);
                        index += sizeof(char);
                        if (_prefix != prefix) return false;

                        size = BitConverter.ToInt32(values, index);
                        index += sizeof(int);

                        byte[] m_value = new byte[size];
                        Buffer.BlockCopy(values, index, m_value, 0, size);
                        index += size;

                        _value = m_value;
                        value = m_value;

                        return true;
                    case 'd':
                        /* char */

                        _prefix = BitConverter.ToChar(values, index);
                        index += sizeof(char);
                        if (_prefix != prefix) return false;

                        _value = BitConverter.ToChar(values, index);
                        index += sizeof(char);

                        value = _value;

                        return true;
                    case 'e':
                        /* double */

                        _prefix = BitConverter.ToChar(values, index);
                        index += sizeof(char);
                        if (_prefix != prefix) return false;

                        _value = BitConverter.ToDouble(values, index);
                        index += sizeof(double);

                        value = _value;

                        return true;
                    case 'f':
                        /* float */

                        _prefix = BitConverter.ToChar(values, index);
                        index += sizeof(char);
                        if (_prefix != prefix) return false;

                        _value = BitConverter.ToSingle(values, index);
                        index += sizeof(float);

                        value = _value;

                        return true;
                    case 'g':
                        /* int */

                        _prefix = BitConverter.ToChar(values, index);
                        index += sizeof(char);
                        if (_prefix != prefix) return false;

                        _value = BitConverter.ToInt32(values, index);
                        index += sizeof(int);

                        value = _value;

                        return true;
                    case 'h':
                        /* long */

                        _prefix = BitConverter.ToChar(values, index);
                        index += sizeof(char);
                        if (_prefix != prefix) return false;

                        _value = BitConverter.ToInt64(values, index);
                        index += sizeof(long);

                        value = _value;

                        return true;
                    case 'i':
                        /* short */

                        _prefix = BitConverter.ToChar(values, index);
                        index += sizeof(char);
                        if (_prefix != prefix) return false;

                        _value = BitConverter.ToInt16(values, index);
                        index += sizeof(short);

                        value = _value;

                        return true;
                    case 'j':
                        /* string */
                        
                        _prefix = BitConverter.ToChar(values, index);
                        index += sizeof(char);
                        if (_prefix != prefix) return false;

                        size = BitConverter.ToInt32(values, index);
                        index += sizeof(int);

                        byte[] amount = new byte[size];
                        Buffer.BlockCopy(values, index, amount, 0, size);
                        index += size;

                        _value = Encode.GetString(amount, Encode.Mode.UTF8);

                        value = _value;

                        return true;
                    /* unset */
                    default:
                        return false;
                }
            }
        }
    }
}
