using System;
using System.Collections.Generic;
using System.IO;

namespace Netly
{
    public class NFraming
    {
        private const long MinMessageSize = 1;
        internal const long DefaultSize = 1024 * 1024 * 20; // 20.00 MB
        private static readonly byte[] Prefix = { 8, 16, 32, 64, 128 };
        private readonly object _locker = new object();
        private List<byte> _cache = new List<byte>();
        private long _size, _position;
        private Stream _stream;
        public long MaxSize { get; set; } = DefaultSize;


        public void Close()
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

        public bool Write(byte[] buffer, int bufferCount, Func<long, Stream> getStream, out Stream stream,
            out bool close, out bool recall)
        {
            stream = null;
            close = false;
            recall = false;

            try
            {
                if (buffer == null || buffer.Length < 1 || bufferCount < 1 || getStream == null)
                    throw new ApplicationException($"Invalid {GetType().FullName}.{nameof(Write)} params.");

                lock (_locker)
                {
                    // new stream detected
                    if (_size == 0)
                    {
                        // save buffer on cache
                        _cache.AddRange(new ArraySegment<byte>(buffer, 0, bufferCount));

                        // calculate entry size requirement
                        var count = Prefix.Length + sizeof(long);

                        // have sufficient entry buffer
                        if (_cache.Count >= count)
                        {
                            // entry bytes
                            var bytes = new byte[count];
                            _cache.CopyTo(0, bytes, 0, bytes.Length);

                            // get and verify size
                            var size = BitConverter.ToInt64(bytes, Prefix.Length);
                            if (size < MinMessageSize) throw new IndexOutOfRangeException($"{nameof(size)}: {size}");

                            // get and verify prefix

                            if (CompareSequences(Prefix, bytes)) _stream = getStream(size);
                            else
                                throw new InvalidDataException(
                                    $"{nameof(Prefix)}: {Format(Prefix)} != {Format(bytes)}");

                            // calculate left bytes
                            var left = _cache.Count - bytes.Length;

                            // update stream size
                            _size = size;

                            // reset write position
                            _position = 0;

                            // reset buffer count
                            bufferCount = 0;

                            // copy left bytes to stream
                            if (left > 0)
                            {
                                buffer = new byte[left];
                                bufferCount = buffer.Length;
                                _cache.CopyTo(bytes.Length, buffer, 0, buffer.Length);
                            }

                            // clear cache
                            _cache.Clear();
                            _cache = new List<byte>();
                        }
                    }

                    // is written buffer
                    if (_size > 0 && bufferCount > 0)
                    {
                        // calculate next cache size after write
                        var nextCount = _position + bufferCount;

                        // have sufficient cached buffer
                        if (nextCount >= _size)
                        {
                            var leftCount = (int)(_size - nextCount);
                            var readCount = bufferCount - leftCount;

                            // write remain buffer
                            _stream.Write(buffer, 0, readCount);

                            // reset stream size
                            _size = 0;

                            // reset write/read position
                            _stream.Position = 0;

                            // save remained data in cache
                            if (leftCount > 0)
                            {
                                // copy buffer
                                var leftBytes = new byte[leftCount];
                                Array.Copy(buffer, readCount, leftBytes, 0, leftBytes.Length);

                                // save buffer
                                _cache.Clear();
                                _cache = new List<byte>(leftBytes);

                                // set recall option (to collect data again)
                                recall = true;
                            }

                            // copy internal stream reference
                            stream = _stream;

                            // delete internal stream reference
                            _stream = null;

                            // return success stream read
                            return true;
                        }

                        // write buffer on cache
                        _stream.Write(buffer, 0, bufferCount);
                        _position += bufferCount;
                    }

                    // return false: wait for next entry
                    return false;
                }
            }
            catch (Exception e)
            {
                close = true;
                Close();
                NetlyEnvironment.Logger.Create(e);
                return false;
            }
        }

        private static bool CompareSequences(byte[] reference, byte[] compare)
        {
            try
            {
                for (var i = 0; i < Prefix.Length; i++)
                    if (reference[i] != compare[i])
                    {
                        NetlyEnvironment.Logger.Create($"Compare not match: {reference[i]}:{compare[i]}");
                        return false;
                    }

                return true;
            }
            catch (Exception e)
            {
                NetlyEnvironment.Logger.Create(e);
                return false;
            }
        }

        private static string Format<T>(T[] bytes)
        {
            return $"[{string.Join(",", bytes)}]";
        }

        public static Stream DefaultOnStream(long size)
        {
            if (size < 1024 * 1024 * 10) return new MemoryStream((int)size); // 10.00 MB                            
            throw new InternalBufferOverflowException($"{nameof(DefaultSize)}, {nameof(size)}: {size}");
        }

        public static byte[] Create(long size)
        {
            if (size < MinMessageSize) throw new IndexOutOfRangeException($"{nameof(size)}: {size}");

            var body = BitConverter.GetBytes(size);
            var buffer = new byte[Prefix.Length + body.Length];

            Buffer.BlockCopy(Prefix, 0, buffer, 0, Prefix.Length);
            Buffer.BlockCopy(body, 0, buffer, Prefix.Length, body.Length);

            return buffer;
        }
    }
}