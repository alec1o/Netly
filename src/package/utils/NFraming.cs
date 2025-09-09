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
        private readonly byte[] _headerBuffer = new byte[Prefix.Length + sizeof(long)];
        private readonly object _locker = new object();


        private readonly LinkedList<Transaction> _transactions = new LinkedList<Transaction>();
        private int _headerOffset;
        private long _size;
        public long MaxSize { get; set; } = DefaultSize;
        public bool IsOpened { get; set; } = true;


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
                            if (CompareSequences(Prefix, _headerBuffer))
                                _transactions.AddLast(new Transaction(getStream(size), false));
                            else
                                throw new InvalidDataException(
                                    $"{nameof(Prefix)}: {Format(Prefix)} - {Format(_headerBuffer)}");

                            segment = SegmentShift(segment, copied);
                            _size = size;
                            _headerOffset = 0;
                        }
                    }

                    if (_size > 0 && segment.Count > 0)
                    {
                        var transaction = _transactions.Last.Value;

                        if (transaction.IsDone)
                            throw new InvalidOperationException("Context stream isn't available to modify");

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
                                segment = SegmentShift(segment, count);
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

        private static ArraySegment<T> SegmentShift<T>(ArraySegment<T> segment, int shift)
        {
            if (shift < 0)
                throw new ArgumentOutOfRangeException(nameof(shift));

            if (segment.Array == null)
                throw new ArgumentNullException(nameof(segment));

            return shift >= segment.Count
                ? new ArraySegment<T>(segment.Array, segment.Offset + segment.Count, 0)
                : new ArraySegment<T>(segment.Array, shift + segment.Offset, segment.Count - shift);
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

        public static Stream NewStream(long size)
        {
            if (size <= DefaultSize) return new MemoryStream((int)size);
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