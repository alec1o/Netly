using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;


namespace Netly
{
    public partial class NetlyEnvironment
    {
        /// <summary>
        /// Netly: Message framing
        /// </summary>
        public class MessageFraming
        {
            /// <summary>
            /// Netly message framing prefix: [ 0, 8, 16, 32, 64, 128 ] (6 byte overhead/size)
            /// </summary>
            public static readonly byte[] Prefix = new byte[] { 0, 8, 16, 32, 64, 128 };

            private static int _maxSize = (1024 * 1024) * 8; // 8 MB
            private static int _udpBuffer = (1024 * 1024) * 1; // 1 MB
            private readonly object _lock = new object();

            private Action<byte[]> _onData;
            private Action<Exception> _onError;

            private List<byte> _buffer = new List<byte>();
            private int _size;

            /// <summary>
            /// Max buffer size (prevent memory leak). Default is 8.388.608 (8MB)
            /// </summary>
            public static int MaxSize
            {
                get => _maxSize;
                set => _maxSize = value > 0 ? value : _maxSize; // prevent negative value
            }

            /// <summary>
            /// Max udp package (prevent memory leak). Default is 1.048.576 (1MB)
            /// </summary>
            public static int UdpBuffer
            {
                get => _udpBuffer;
                set => _udpBuffer = value > 0 ? value : _udpBuffer; // prevent negative value
            }

            /// <summary>
            /// Create message framing bytes (attach prefix)<br/>
            /// Protocol:
            /// <br/> [ 0, 8, 16, 32, 64, 128 ] + [ BUFFER_LENGTH ] + [ BUFFER ]
            /// </summary>
            /// <param name="value">Input</param>
            /// <returns></returns>
            public static byte[] CreateMessage(byte[] value)
            {
                byte[] size = BitConverter.GetBytes((int)value.Length);
                return new byte[3][] { Prefix, size, value }.SelectMany(x => x).ToArray();
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
                    _buffer = Array.Empty<byte>().ToList();
                }
            }

            private static bool IsPrefix(byte[] buffer)
            {
                if (buffer == null || !(buffer.Length >= Prefix.Length)) return false;

                for (int i = 0; i < Prefix.Length; i++)
                {
                    if (buffer[i] != Prefix[i]) return false;
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
                    if (_size == 0 && _buffer.Count >= (sizeof(int) + Prefix.Length))
                    {
                        byte[] b = _buffer.GetRange(0, Prefix.Length).ToArray();
                        _buffer.RemoveRange(0, Prefix.Length);

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
}