using System;
using System.Collections.Generic;
using System.IO;

namespace Netly
{
    public sealed class NFraming
    {
        private const long MinMessageSize = 1;

        /// <summary>
        ///     Default <see cref="NFraming" /> Size: 20 MB or 2.097.1520
        /// </summary>
        public const long DefaultSize = 1024 * 1024 * 20; // 20.00 MB

        /// <summary>
        ///     The prefix used in <see cref="NFraming" /> is: [0x1, 0x9, 0x3, 0x3] in hexadecimal,
        ///     which is [1, 9, 3, 3] in decimal. It serves as a “magic number”
        ///     to identify the start of a message in a TCP stream.
        ///     <br />
        ///     Inspired by computing history: in 1936, Alan Turing created
        ///     the Turing Machine, a theoretical model of computation. The main
        ///     challenge was defining when the machine should halt, leading to
        ///     the concept of the Halting Problem, establishing the limits of
        ///     what is computable.
        ///     See: https://en.wikipedia.org/wiki/Turing_machine
        /// </summary>
        public static readonly byte[] Prefix = { 0x01, 0x09, 0x03, 0x03 };

        private readonly byte[] _headerBuffer = new byte[Prefix.Length + sizeof(long)];
        private readonly object _locker = new object();
        private readonly LinkedList<Transaction> _transactions = new LinkedList<Transaction>();

        private int _headerOffset;
        private long _size;

        public NFraming()
        {
            Open();
        }

        /// <summary>
        ///     Gets or sets the maximum allowed size for a message. Default is <see cref="DefaultSize" />
        /// </summary>
        public long MaxSize { get; set; } = DefaultSize;

        /// <summary>
        ///     Gets whether the <see ref="NFraming" /> instance is opened for writing/reading.
        /// </summary>
        public bool IsOpened { get; private set; } = true;


        /// <summary>
        ///     Closes the <see ref="NFraming" /> instance, clearing all pending transactions and streams.
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
        ///     Opens the <see cref="NFraming" /> instance for reading and writing.
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
        ///     Writes data from a segment into the <see ref="NFraming" /> system. Handles
        ///     header processing, message framing, and transaction management.
        /// </summary>
        /// <param name="segment">The data segment to write.</param>
        /// <param name="getStream">
        ///     A function that creates a stream for a new message, given its size.
        /// </param>
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
                            // get and verify size
                            var size = BitConverter.ToInt64(_headerBuffer, Prefix.Length);

                            if (size < MinMessageSize)
                                throw new InvalidDataException($"{nameof(size)}: {size}");

                            // get and verify prefix
                            if (NUtils.ArraySequenced(Prefix, _headerBuffer))
                                _transactions.AddLast(new Transaction(getStream(size), false));
                            else
                                throw new InvalidDataException(
                                    $"{nameof(Prefix)}: {NUtils.Format(Prefix)} - {NUtils.Format(_headerBuffer)}");

                            segment = NUtils.SegmentShift(segment, copied);
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
                            var left = next - count;

                            stream.Write(segment.Array ?? throw new ArgumentNullException(nameof(segment)),
                                segment.Offset, count);

                            stream.Position = 0;
                            transaction.IsDone = true; // REQUIRED: Save stream state
                            _size = 0;

                            if (left > 0)
                            {
                                segment = NUtils.SegmentShift(segment, count);
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
        ///     The resulting array contains the prefix followed by the length in bytes.
        /// </summary>
        /// <param name="size">The size of the payload.</param>
        /// <returns>A byte array containing the prefix and length.</returns>
        /// <exception cref="IndexOutOfRangeException">Thrown if size is less than the minimum message size.</exception>
        public static byte[] Create(long size)
        {
            if (size < MinMessageSize) throw new IndexOutOfRangeException($"{nameof(size)}: {size}");

            var body = BitConverter.GetBytes(size);
            var buffer = new byte[Prefix.Length + body.Length];

            Buffer.BlockCopy(Prefix, 0, buffer, 0, Prefix.Length);
            Buffer.BlockCopy(body, 0, buffer, Prefix.Length, body.Length);

            return buffer;
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