using System;
using System.Collections.Generic;
using System.IO;

namespace Netly
{
    internal sealed class Framing
    {
        private const long MinMessageSize = 1;

        /// <summary>
        ///     Default <see cref="Framing" /> Size: 20 MB or 2.097.1520
        /// </summary>
        public const long DefaultSize = 1024 * 1024 * 20; // 20.00 MB

        /// <summary>
        ///     Version of the header, the first byte of every message. A peer with another version is refused.
        /// </summary>
        public const byte Version = 1;

        /// <summary>
        ///     Size of the header: Version (1) + Length (8, unsigned, big-endian) + Checksum (1).
        ///     <br />
        ///     The checksum (CRC-8 of Version + Length) takes the place of a magic prefix: it tells a valid header
        ///     from data out of alignment (or random) without reading the payload.
        /// </summary>
        public const int HeaderSize = 1 + sizeof(ulong) + 1;

        private readonly byte[] _headerBuffer = new byte[HeaderSize];
        private readonly object _locker = new object();
        private readonly LinkedList<Transaction> _transactions = new LinkedList<Transaction>();

        private int _headerOffset;
        private long _size;

        public Framing()
        {
            Open();
        }

        /// <summary>
        ///     Gets or sets the maximum allowed size for a message. Default is <see cref="DefaultSize" />
        /// </summary>
        public long MaxSize { get; set; } = DefaultSize;

        /// <summary>
        ///     Gets whether the <see ref="Framing" /> instance is opened for writing/reading.
        /// </summary>
        public bool IsOpened { get; private set; } = true;

        /// <summary>
        ///     Gets whether a header has started and is not complete yet (a peer that stops in the middle of it).
        /// </summary>
        public bool IsHeaderPending
        {
            get
            {
                lock (_locker)
                {
                    return _size == 0 && _headerOffset > 0;
                }
            }
        }

        /// <summary>
        ///     Gets how many headers were completed, it tells a new header from the one that was already waited.
        /// </summary>
        public long HeaderCount { get; private set; }


        /// <summary>
        ///     Closes the <see ref="Framing" /> instance, clearing all pending transactions and streams.
        ///     After calling this, writing or reading is not allowed.
        /// </summary>
        public void Close()
        {
            lock (_locker)
            {
                // reset proprieties
                IsOpened = false;
                _size = 0;
                _headerOffset = 0;

                // clear streams
                foreach (var transaction in _transactions) transaction.Stream.Close();
                _transactions.Clear();
            }
        }

        /// <summary>
        ///     Opens the <see cref="Framing" /> instance for reading and writing.
        ///     After calling this, the instance can accept new data via <see cref="Write" />
        ///     and completed transactions can be read via <see cref="Read" />.
        /// </summary>
        public void Open()
        {
            lock (_locker)
            {
                Close();
                IsOpened = true;
            }
        }

        /// <summary>
        ///     Reads the next completed transaction stream from the queue.
        ///     Returns true if a completed stream was available; otherwise false.
        /// </summary>
        /// <param name="stream">The output stream containing the complete message.</param>
        /// <returns>True if a stream was available; false otherwise.</returns>
        public bool Read(out Stream stream)
        {
            lock (_locker)
            {
                ThrowIfClose();

                stream = null;

                if (_transactions.Count < 1)
                    return false;

                if (!_transactions.First.Value.IsDone)
                    return false;

                stream = _transactions.First.Value.Stream;

                _transactions.RemoveFirst();

                return true;
            }
        }

        private void ThrowIfClose()
        {
            if (IsOpened is false)
                throw new InvalidOperationException($"{GetType().FullName}.{nameof(IsOpened)} = {IsOpened}");
        }

        /// <summary>
        ///     Writes data from a segment into the <see ref="Framing" /> system. Handles
        ///     header processing, message framing, and transaction management.
        /// </summary>
        /// <param name="segment">The data segment to write.</param>
        /// <param name="getStream">
        ///     A function that creates a stream for a new message, given its size.
        /// </param>
        /// <remarks>
        ///     When it throws, the state is undefined (a partial header or message may be buffered):
        ///     the caller must close the connection and <see cref="Open" /> again before reusing it.
        /// </remarks>
        /// <exception cref="ArgumentException">Thrown if arguments are invalid.</exception>
        /// <exception cref="InvalidDataException">Thrown if the prefix or size is invalid.</exception>
        /// <exception cref="InvalidOperationException">Thrown if trying to write to a completed stream.</exception>
        /// <exception cref="InvalidOperationException">Context stream isn't available to write.</exception>
        public void Write(ArraySegment<byte> segment, Func<long, Stream> getStream)
        {
            if (segment == default || segment.Array == null || getStream == null)
                throw new ArgumentException($"Invalid {GetType().FullName}.{nameof(Write)} [arguments]");

            lock (_locker)
            {
                ThrowIfClose();

                for (;;)
                {
                    if (_size == 0 && segment.Count > 0)
                    {
                        var copied = Math.Min(segment.Count, _headerBuffer.Length - _headerOffset);

                        Buffer.BlockCopy(segment.Array ?? throw new ArgumentNullException(nameof(segment)),
                            segment.Offset, _headerBuffer, _headerOffset, copied);

                        _headerOffset += copied;

                        if (_headerOffset == _headerBuffer.Length)
                        {
                            // verify header
                            if (_headerBuffer[HeaderSize - 1] != Checksum(_headerBuffer, HeaderSize - 1))
                                throw new InvalidDataException(
                                    $"Header checksum: {Helper.Format(_headerBuffer)}");

                            if (_headerBuffer[0] != Version)
                                throw new InvalidDataException($"Version: {_headerBuffer[0]} != {Version}");

                            var length = ReadLength(_headerBuffer);

                            // get and verify size
                            if (length < (ulong)MinMessageSize || length > long.MaxValue)
                                throw new InvalidDataException($"{nameof(length)}: {length}");

                            var size = (long)length;

                            HeaderCount++;

                            // size comes from the remote peer, never allocate/open a stream above the limit.
                            if (size > MaxSize)
                                throw new InvalidDataException($"{nameof(size)}: {size} > {nameof(MaxSize)}: {MaxSize}");

                            _transactions.AddLast(new Transaction(getStream(size), false));

                            segment = Helper.SegmentShift(segment, copied);
                            _size = size;
                            _headerOffset = 0;
                            _transactions.Last.Value.Stream.Position = 0;
                        }
                    }

                    if (_size > 0 && segment.Count > 0)
                    {
                        var transaction = _transactions.Last.Value;

                        if (transaction.IsDone || !transaction.Stream.CanWrite)
                            throw new InvalidOperationException("Context stream isn't available to write");

                        var stream = transaction.Stream;
                        var next = transaction.Position + segment.Count;

                        if (next >= _size)
                        {
                            var count = (int)(_size - transaction.Position);
                            var left = segment.Count - count;

                            stream.Write(segment.Array ?? throw new ArgumentNullException(nameof(segment)),
                                segment.Offset, count);

                            stream.Position = 0;
                            transaction.IsDone = true; // REQUIRED: Save stream state
                            _size = 0;

                            if (left > 0)
                            {
                                segment = Helper.SegmentShift(segment, count);
                                continue; // REQUIRED: Read new stream again!
                            }
                        }
                        else
                        {
                            stream.Write(segment.Array ?? throw new ArgumentNullException(nameof(segment)),
                                segment.Offset, segment.Count);
                            transaction.Position += segment.Count;
                        }
                    }

                    break;
                }
            }
        }

        /// <summary>
        ///     Creates a byte array representing a framed message header for a given size.
        ///     The resulting array contains: Version, Length (8 bytes, big-endian) and Checksum.
        /// </summary>
        /// <param name="size">The size of the payload.</param>
        /// <returns>A byte array containing the header.</returns>
        /// <exception cref="IndexOutOfRangeException">Thrown if size is less than the minimum message size.</exception>
        public static byte[] Create(long size)
        {
            if (size < MinMessageSize) throw new IndexOutOfRangeException($"{nameof(size)}: {size}");

            return Header(Version, (ulong)size);
        }

        /// <summary>
        ///     Reads and validates the header at the start of a message that is already in memory (RUDP): checksum,
        ///     version and size (between 1 and <paramref name="maxSize" />).
        /// </summary>
        /// <returns>True when the header is valid, <paramref name="length" /> is the size of the payload that follows.</returns>
        internal static bool TryReadHeader(byte[] data, int offset, int count, long maxSize, out long length)
        {
            length = 0;

            if (count < HeaderSize) return false;

            var header = new byte[HeaderSize];

            Buffer.BlockCopy(data, offset, header, 0, HeaderSize);

            if (header[HeaderSize - 1] != Checksum(header, HeaderSize - 1) || header[0] != Version) return false;

            var value = ReadLength(header);

            if (value < (ulong)MinMessageSize || value > long.MaxValue || (long)value > maxSize) return false;

            length = (long)value;
            return true;
        }

        // no validation of the size
        internal static byte[] Header(byte version, ulong length)
        {
            var buffer = new byte[HeaderSize];

            buffer[0] = version;

            for (var i = 0; i < sizeof(ulong); i++)
                buffer[1 + i] = (byte)(length >> (8 * (sizeof(ulong) - 1 - i)));

            buffer[HeaderSize - 1] = Checksum(buffer, HeaderSize - 1);

            return buffer;
        }

        private static ulong ReadLength(byte[] header)
        {
            ulong length = 0;

            for (var i = 0; i < sizeof(ulong); i++)
                length = (length << 8) | header[1 + i];

            return length;
        }

        // crc-8 (polynomial 0x07), the seed is not zero so a header of zeros is not valid
        internal static byte Checksum(byte[] data, int count)
        {
            var crc = (byte)0xFF;

            for (var i = 0; i < count; i++)
            {
                crc ^= data[i];

                for (var bit = 0; bit < 8; bit++)
                    crc = (crc & 0x80) != 0 ? (byte)((crc << 1) ^ 0x07) : (byte)(crc << 1);
            }

            return crc;
        }

        private class Transaction
        {
            public Transaction(Stream stream, bool isDone)
            {
                Stream = stream;
                IsDone = isDone;
            }

            public Stream Stream { get; }
            public bool IsDone { get; set; }
            public long Position { get; set; }
        }
    }
}