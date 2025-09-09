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
        private long _size;

        private readonly LinkedList<Transaction> _transactions = new LinkedList<Transaction>();
        private List<byte> _memory = new List<byte>();
        public long MaxSize { get; set; } = DefaultSize;
        public bool IsOpened { get; set; } = true;


        public void Close()
        {
            lock (_locker)
            {
                // reset proprieties
                IsOpened = false;
                _size = 0;

                // clear memory
                _memory.Clear();
                _memory = new List<byte>();

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

        public void Write(byte[] buffer, int length, Func<long, Stream> getStream)
        {
            if (buffer == null || buffer.Length < 1 || length < 1 || getStream == null)
                throw new ArgumentException($"Invalid {GetType().FullName}.{nameof(Write)} [arguments]");

            lock (_locker)
            {
                ThrowIfClose();

                for (;;)
                {
                    if (_size == 0)
                    {
                        var count = Prefix.Length + sizeof(long);

                        if (length > 0)
                            _memory.AddRange(new ArraySegment<byte>(buffer, 0, length));

                        if (_memory.Count >= count)
                        {
                            // entry bytes
                            var bytes = new byte[count];
                            _memory.CopyTo(0, bytes, 0, bytes.Length);

                            // get and verify size
                            var size = BitConverter.ToInt64(bytes, Prefix.Length);

                            if (size < MinMessageSize)
                                throw new IndexOutOfRangeException($"{nameof(size)}: {size}");

                            // get and verify prefix
                            if (CompareSequences(Prefix, bytes))
                                _transactions.AddLast(new Transaction(getStream(size), false));
                            else
                                throw new InvalidDataException($"{nameof(Prefix)}: {Format(Prefix)} - {Format(bytes)}");

                            var left = _memory.Count - bytes.Length;
                            length = 0;
                            _size = size;

                            if (left > 0)
                            {
                                buffer = new byte[left];
                                length = buffer.Length;
                                _memory.CopyTo(bytes.Length, buffer, 0, buffer.Length);
                            }

                            _memory.Clear();
                            _memory = new List<byte>();
                        }
                    }

                    if (_size > 0 && length > 0)
                    {
                        var transaction = _transactions.Last.Value;

                        if (transaction.IsDone)
                            throw new InvalidOperationException("Context stream isn't available to modify");

                        var stream = transaction.Stream;
                        var next = transaction.Position + length;

                        if (next >= _size)
                        {
                            var count = (int)(_size - transaction.Position);
                            var left = next - count;

                            stream.Write(buffer, 0, count);
                            stream.Position = 0;
                            transaction.IsDone = true; // REQUIRED: Save stream state
                            _size = 0;

                            if (left > 0)
                            {
                                // copy context
                                var leftBytes = new byte[left];
                                Array.Copy(buffer, count, leftBytes, 0, leftBytes.Length);

                                // update context
                                buffer = leftBytes;
                                length = buffer.Length;

                                continue; // REQUIRED: Read new stream again!
                            }
                        }
                        else
                        {
                            stream.Write(buffer, 0, length);
                            transaction.Position += length;
                        }
                    }

                    break;
                }
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

        public static Stream NewStream(long size)
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

        private class Transaction
        {
            public Stream Stream { get; }
            public bool IsDone { get; set; }
            public long Size { get; set; }
            public long Position { get; set; }

            public Transaction(Stream stream, bool isDone)
            {
                Stream = stream;
                IsDone = isDone;
            }
        }
    }
}