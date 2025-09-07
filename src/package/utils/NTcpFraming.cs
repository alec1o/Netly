using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Netly.Packages.Utils
{
    internal class NTcpFraming
    {
        internal const long DefaultSize = 1024 * 1024 * 20; // 20.00 MB
        internal long MaxSize { get; set; } = DefaultSize;
        private static readonly byte[] Prefix = { 8, 16, 32, 64, 128 };
        private Stream _stream;
        private long _size, _position;
        private readonly object _locker = new object();
        private List<byte> _cache;


        internal void Close()
        {
            lock (_locker)
            {
                _size = 0;
                _position = 0;
                _cache.Clear();
                _cache = new List<byte>();
                _stream?.Close();
            }
        }

        internal bool Write(byte[] buffer, int bufferCount, Func<long, Stream> getStream, out Stream stream,
            out bool close)
        {
            stream = null;
            close = false;

            if (buffer == null || buffer.Length < 1 || bufferCount < 1 || getStream == null)
            {
                close = true;
                NetlyEnvironment.Logger.Create($"Invalid {GetType().FullName}.{nameof(Write)} params.");
                return false;
            }

            lock (_locker)
            {
                while (true)
                {
                    if (_size == 0)
                    {
                        _position = 0;

                        var entryBytes = new byte[bufferCount];
                        Buffer.BlockCopy(buffer, 0, entryBytes, 0, entryBytes.Length);
                        _cache.AddRange(entryBytes);

                        var entrySize = Prefix.Length + sizeof(long);

                        if (_cache.Count >= entrySize)
                        {
                            // entry bytes
                            var bytes = _cache.GetRange(0, entrySize).ToArray();
                            _cache.RemoveRange(0, entrySize);

                            // invalid prefix
                            if (Prefix.Where((value, index) => value != bytes[index]).Any())
                            {
                                close = true;
                                Close();
                                NetlyEnvironment.Logger.Create($"Invalid Framing Prefix");
                                return false;
                            }

                            // message size
                            try
                            {
                                var size = BitConverter.ToInt64(bytes, Prefix.Length);
                                if (size < 1) throw new InvalidDataException($"Invalid Framing Size: {size}");
                                _stream = getStream(size);
                                _size = size;

                                if (_cache.Count > 0)
                                {
                                    // write buffer
                                    var cached = _cache.ToArray();
                                    _stream.Write(cached, 0, cached.Length);

                                    // clean memory
                                    _cache.Clear();
                                    _cache = new List<byte>();

                                    // update position
                                    _position += cached.Length; // REQUIRED
                                }

                                bufferCount = 0; // REQUIRED
                                continue;
                            }
                            catch (Exception e)
                            {
                                close = true;
                                Close();
                                NetlyEnvironment.Logger.Create(e);
                                return false;
                            }
                        }
                    }

                    if (bufferCount > 0)
                    {
                        var nextCount = _position + bufferCount;

                        if (nextCount >= _size)
                        {
                            var leftCount = (int)(_size - nextCount);
                            var readCount = bufferCount - leftCount;
                            _stream.Write(buffer, 0, readCount);
                            stream = _stream;

                            if (leftCount > 0)
                            {
                                var leftBytes = new byte[leftCount];
                                Array.Copy(buffer, readCount, leftBytes, 0, leftBytes.Length);
                                _cache.Clear();
                                _cache = new List<byte>(leftBytes);
                                bufferCount = 0; // REQUIRED
                                _size = 0; // REQUIRED
                                continue;
                            }

                            return true;
                        }

                        _stream.Write(buffer, 0, bufferCount);
                        _position += bufferCount;
                        return false;
                    }

                    break;
                }

                return false;
            }
        }

        internal static Stream DefaultOnStream(long size)
        {
            if (size < 1024 * 1024 * 10) return new MemoryStream((int)size); // 10.00 MB                            
            throw new InternalBufferOverflowException($"{nameof(DefaultSize)}, {nameof(size)}: {size}");
        }

        internal static byte[] Create(long size)
        {
            var body = BitConverter.GetBytes(size);
            var buffer = new byte[Prefix.Length + body.Length];

            Buffer.BlockCopy(Prefix, 0, buffer, 0, Prefix.Length);
            Buffer.BlockCopy(body, 0, buffer, Prefix.Length, body.Length);

            return buffer;
        }
    }
}