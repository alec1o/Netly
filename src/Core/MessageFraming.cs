using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Netly.Core
{
    /// <summary>
    /// Netly: Message framing
    /// </summary>
    public class MessageFraming
    {
        /// <summary>
        /// Netly message framing prefix: [ 0, 8, 16, 32, 64, 128 ] (6 byte overhead/size)
        /// </summary>
        public static readonly byte[] PREFIX = new byte[] { 0, 8, 16, 32, 64, 128 };

        private static int _MaxSize = (1024 * 1024) * 32; // 32 MB
        private readonly object _lock = new object();

        private Action<byte[]> _onData;
        private Action<Exception> _onError;

        public List<byte> _buffer = new List<byte>();
        private int _size;

        /// <summary>
        /// Return true if message framing is started
        /// </summary>
        public bool Started { get; private set; }

        /// <summary>
        /// Max buffer size (prevent memory leak). Default is 33.554.432 (32MB)
        /// </summary>
        public static int MaxSize
        {
            get { return _MaxSize; }
            set { if (value > 0) _MaxSize = value; }
        }

        /// <summary>
        /// Create message framing bytes (attach prefix)<br/>
        /// Protocol:
        /// <br/> 1 -> [ 0, 8, 16, 32, 64 ] --size: 6 bytes --value: {0, 8, 16, 32, 64}
        /// <br/> 2 -> [ buffer length ] --size: 4 bytes --value: dynamic
        /// <br/> 3 -> [ buffer ] --size: dynamic --value: dynamic
        /// </summary>
        /// <param name="value">Input</param>
        /// <returns></returns>
        public static byte[] CreateMessage(byte[] value)
        {
            byte[] size = BitConverter.GetBytes((int)value.Length);
            return new byte[3][] { PREFIX, size, value }.SelectMany(x => x).ToArray();
        }

        /// <summary>
        /// Called when have data
        /// </summary>
        /// <param name="callback">Callback</param>
        public void OnData(Action<byte[]> callback)
        {
            _onData = callback;
        }

        /// <summary>
        /// Called when have error
        /// </summary>
        /// <param name="callback">Callback</param>
        public void OnError(Action<Exception> callback)
        {
            _onError = callback;
        }

        /// <summary>
        /// Clear buffer
        /// </summary>
        public void Clear()
        {
            lock (_lock)
            {
                _buffer.Clear();
                _buffer = new byte[0].ToList();
            }
        }

        private bool IsPrefix(byte[] buffer)
        {
            if (buffer == null || !(buffer.Length >= PREFIX.Length)) return false;

            for (int i = 0; i < PREFIX.Length; i++)
            {
                if (buffer[i] != PREFIX[i]) return false;
            }

            return true;
        }

        /// <summary>
        /// Add buffer in flow
        /// </summary>
        /// <param name="buffer"></param>
        public void Add(byte[] buffer)
        {
            lock (_lock)
            {
                _buffer.AddRange(buffer);

            INIT:
                if (_size == 0 && _buffer.Count >= (sizeof(int) + PREFIX.Length))
                {
                    byte[] b = _buffer.GetRange(0, PREFIX.Length).ToArray();
                    _buffer.RemoveRange(0, PREFIX.Length);

                    if (!IsPrefix(b))
                    {
                        _onError?.Invoke(new InvalidDataException("Netly Message framing prefix not found"));
                        return;
                    }

                    int len = BitConverter.ToInt32(_buffer.GetRange(0, sizeof(int)).ToArray(), 0);

                    if (len > MaxSize || len <= 0)
                    {
                        _onError?.Invoke(new ArgumentOutOfRangeException(
                            "you haven't use MessageFraming protocol or you MessageFraming.MaxSize " +
                            $"is low, (received value: {len}, max value: {MaxSize}"));
                        return;
                    }
                    else
                    {
                        _size = len;
                        _buffer.RemoveRange(0, sizeof(int));
                    }
                }

                if (_size > 0 && _buffer.Count >= _size)
                {
                    byte[] data = _buffer.GetRange(0, _size).ToArray();
                    _buffer.RemoveRange(0, _size);
                    _size = 0;

                    byte[] _temp = _buffer.GetRange(0, _buffer.Count).ToArray();
                    _buffer = _temp.ToList();

                    _onData?.Invoke(data);

                    if (_buffer.Count > 0)
                    {
                        goto INIT;
                    }
                }
            }
        }
    }
}
