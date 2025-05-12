using System;
using System.Collections.Generic;
using System.Linq;

namespace Netly
{
    public partial class NetlyEnvironment
    {
        /// <summary>
        ///     Netly: Message framing
        /// </summary>
        public class MessageFraming
        {
            /// <summary>
            ///     Netly message framing prefix: [ 0, 8, 16, 32, 64, 128 ] (6 byte overhead/size)
            /// </summary>
            public static readonly byte[] Prefix = { 0, 8, 16, 32, 64, 128 };

            private static int _maxSize = 1024 * 1024 * 8; // 8 MB
            private static int _udpBuffer = 1024 * 1024 * 1; // 1 MB
            private readonly object _lock = new object();
            private readonly List<byte> _stream = new List<byte>();
            private string InstanceName => $"[{GetType().Namespace}.{GetType().Name}]";
            private bool _isLocked;

            private Action<byte[]> _onData;
            private Action<Exception> _onError;
            private int _size;

            /// <summary>
            ///     Max buffer size (prevent memory leak). Default is 8.388.608 (8MB)
            /// </summary>
            public static int MaxSize
            {
                get => _maxSize;
                set => _maxSize = value > 0 ? value : _maxSize; // prevent negative value
            }

            /// <summary>
            ///     Max udp package (prevent memory leak). Default is 1.048.576 (1MB)
            /// </summary>
            public static int UdpBuffer
            {
                get => _udpBuffer;
                set => _udpBuffer = value > 0 ? value : _udpBuffer; // prevent negative value
            }

            /// <summary>
            ///     Create message framing bytes (attach prefix)<br />
            ///     Protocol:
            ///     <br /> [ 0, 8, 16, 32, 64, 128 ] + [ BUFFER_LENGTH ] + [ BUFFER ]
            /// </summary>
            /// <param name="value">Input</param>
            /// <returns></returns>
            public static byte[] CreateMessage(byte[] value)
            {
                var size = BitConverter.GetBytes(value.Length);
                return new[] { Prefix, size, value }.SelectMany(x => x).ToArray();
            }

            /// <summary>
            ///     Called when have data
            /// </summary>
            /// <param name="callback">Callback</param>
            public void OnData(Action<byte[]> callback)
            {
                _onData = callback;
            }

            /// <summary>
            ///     Called when have error
            /// </summary>
            /// <param name="callback">Callback</param>
            public void OnError(Action<Exception> callback)
            {
                _onError = callback;
            }

            /// <summary>
            ///     Clear buffer
            /// </summary>
            public void Clear()
            {
                lock (_lock)
                {
                    _stream.Clear();
                }
            }

            private static bool IsPrefix(List<byte> buffer)
            {
                if (buffer == null || buffer.Count != Prefix.Length) return false;
                return !Prefix.Where((value, index) => buffer[index] != value).Any();
            }

            /// <summary>
            ///     Add buffer in flow
            /// </summary>
            /// <param name="buffer"></param>
            public void Add(byte[] buffer)
            {
                lock (_lock)
                {
                    if (_isLocked) return;

                    _stream.AddRange(buffer);

                    while (true)
                    {
                        if (_size == 0 && _stream.Count >= sizeof(int) + Prefix.Length)
                        {
                            // check is first bytes is equal to prefix
                            var isPrefix = IsPrefix(_stream.GetRange(0, Prefix.Length));
                            // remove prefix bytes from flow.
                            _stream.RemoveRange(0, Prefix.Length);
                            // close if prefix is invalid
                            if (isPrefix is false)
                            {
                                ErrorClose($"{InstanceName} prefix not match.");
                                return;
                            }

                            // get data size
                            _size = BitConverter.ToInt32(_stream.GetRange(0, sizeof(int)).ToArray(), 0);
                            // remove size bytes from flow.
                            _stream.RemoveRange(0, sizeof(int));
                            // close if data size is invalid
                            if (_size <= 0 || _size > MaxSize)
                            {
                                ErrorClose($"{nameof(InstanceName)} invalid size: {_size}, min: {1}, max:{MaxSize}");
                                return;
                            }
                        }

                        if (_size > 0 && _stream.Count >= _size)
                        {
                            var bytes = new byte[_size];
                            _stream.CopyTo(0, bytes, 0, bytes.Length);
                            _stream.RemoveRange(0, _size);

                            // reset size
                            _size = 0;

                            // send receive bytes.
                            _onData?.Invoke(bytes);

                            // verify is still have data to process.
                            if (_stream.Count > 0) continue;
                        }

                        return;
                    }
                }
            }

            private void ErrorClose(string message)
            {
                _onError?.Invoke(new Exception(message));

                lock (_lock)
                {
                    _isLocked = true;
                    Clear();
                }
            }
        }
    }
}