using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;

namespace Netly
{
    public unsafe class Blob : IDisposable
    {
        public class Chuck
        {
            public byte* Bytes { get; }
            public long Size { get; }
            public bool IsCopy { get; set; }
            public bool IsReleased { get; set; }
            private readonly object _locker = new object();

            public Chuck Copy()
            {
                lock (_locker)
                {
                    if (IsReleased) throw new InvalidOperationException(nameof(Copy));
                    IsCopy = true;
                    return new Chuck(Bytes, Size);
                }
            }

            public void Free()
            {
                lock (_locker)
                {
                    IsReleased = true;
                    if (IsCopy) return;
                    Marshal.FreeHGlobal((IntPtr)Bytes);
                }
            }

            public Chuck(byte* bytes, long size)
            {
                Bytes = bytes;
                Size = size;
                IsCopy = false;
                IsReleased = false;
            }
        }

        public readonly List<Chuck> Chucks = new List<Chuck>();
        private readonly object _locker = new object();
        public long Count { get; private set; }

        public Blob()
        {
            Update();
        }

        private Blob(byte* bytes, long size)
        {
            Append(bytes, size);
        }

        public static implicit operator byte[](Blob blob)
        {
            return blob.ToBytes();
        }

        public static explicit operator Blob(byte[] bytes)
        {
            var size = bytes.LongLength;
            fixed (byte* pointer = bytes)
            {
                var buffer = (byte*)Marshal.AllocHGlobal(bytes.Length);
                Buffer.MemoryCopy(pointer, buffer, size, size);
                return new Blob(buffer, size);
            }
        }

        public void Clear()
        {
            lock (_locker)
            {
                foreach (var chuck in Chucks)
                {
                    chuck.Free();
                }

                Chucks.Clear();
                Update();
            }
        }

        public void Add(Blob blob)
        {
            lock (_locker)
            {
                foreach (var chuck in blob.Chucks)
                {
                    Append(chuck.Copy());
                }
            }
        }

        public Blob Read(long count, bool remove)
        {
            lock (_locker)
            {
                if (count <= 0 || count > Count) throw new ArgumentOutOfRangeException(nameof(count));

                var blob = new Blob();
                var remaining = count;

                for (long i = 0; i < Chucks.Count; i++)
                {
                    var chuck = Chucks[(int)i];

                    if (chuck.Size <= remaining)
                    {
                        blob.Append(chuck.Copy());

                        if (remove)
                        {
                            chuck.Free();
                        }

                        remaining -= chuck.Size;
                    }
                    else
                    {
                        var toRead = remaining;
                        var readBytes = (byte*)Marshal.AllocHGlobal((IntPtr)toRead);
                        Buffer.MemoryCopy(chuck.Bytes, readBytes, toRead, toRead);
                        blob.Append(readBytes, toRead);

                        if (remove)
                        {
                            var restSize = chuck.Size - toRead;
                            var restBytes = (byte*)Marshal.AllocHGlobal((IntPtr)restSize);
                            Buffer.MemoryCopy(chuck.Bytes + toRead, restBytes, restSize, restSize);
                            chuck.Free();
                            Chucks[(int)i] = new Chuck(restBytes, restSize);
                        }

                        remaining = 0;
                    }

                    if (remaining == 0) break;
                }

                Update();

                return blob;
            }
        }

        private void ReleaseUnmanagedResources()
        {
            lock (_locker)
            {
                Clear();
            }
        }

        public void Dispose()
        {
            ReleaseUnmanagedResources();
            GC.SuppressFinalize(this);
        }

        private void Append(byte* bytes, long size)
        {
            Append(new Chuck(bytes, size));
        }

        private void Append(Chuck chuck)
        {
            lock (_locker)
            {
                Chucks.Add(chuck);
                Update();
            }
        }

        private byte[] ToBytes()
        {
            lock (_locker)
            {
                var bytes = new byte[Count];
                var offset = 0L;

                foreach (var chunk in Chucks)
                {
                    Marshal.Copy((IntPtr)chunk.Bytes, bytes, (int)offset, (int)chunk.Size);
                    offset += chunk.Size;
                }

                return bytes;
            }
        }

        private void Update()
        {
            lock (_locker)
            {
                Chucks.RemoveAll(x => x.IsReleased);
                Count = Chucks.Sum(x => x.Size);
            }
        }

        ~Blob()
        {
            ReleaseUnmanagedResources();
        }
    }
}