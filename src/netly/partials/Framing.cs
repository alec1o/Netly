using System;

namespace Netly
{
    internal class Framing : IFraming
    {
        private long _minSize, _maxSize, _size;
        private Action<Blob> _readCallback;
        private Action<Exception> _failCallback;
        private readonly object _locker = new object();
        public bool IsActive { get; set; }
        public long Prefix { get; set; }

        public long MaxSize
        {
            get => _maxSize;
            set
            {
                if (value > 0) _maxSize = value;
                else throw new ArgumentOutOfRangeException($"[{nameof(IFraming)}.{nameof(MaxSize)} = {MaxSize}]");
            }
        }

        public long MinSize
        {
            get => _minSize;
            set
            {
                if (value < MaxSize) _minSize = value;
                else throw new ArgumentOutOfRangeException($"[{nameof(IFraming)}.{nameof(MinSize)} = {MinSize}]");
            }
        }

        public Blob Stream { get; }

        private Framing()
        {
            IsActive = false;
            Prefix = BitConverter.ToInt64(new byte[] { 0, 2, 4, 8, 16, 32, 64, 128 }, 0);
            MaxSize = 1024 * 1024 * 8;
            MinSize = 1;
            Stream = new Blob();
        }

        public Framing(bool isActive) : this()
        {
            IsActive = isActive;
        }

        public Framing(IFraming reference) : this()
        {
            IsActive = reference.IsActive;
            Prefix = reference.Prefix;
            MaxSize = reference.MaxSize;
        }

        public void Clear()
        {
            lock (_locker)
            {
                _size = 0;
                _readCallback = null;
                Stream.Clear();
            }
        }

        public void Write(Blob blob)
        {
            lock (_locker)
            {
                if (!IsActive || Stream == null) return;

                Stream.Add(blob);

                while (true)
                {
                    const long minSize = sizeof(long) + sizeof(long);

                    if (_size == 0 && Stream.Count >= minSize)
                    {
                        // read prefix
                        var prefixBlob = Stream.Read(sizeof(long), true);
                        var prefix = BitConverter.ToInt64(prefixBlob, 0);
                        prefixBlob.Dispose();

                        if (prefix == Prefix) // validate prefix
                        {
                            Fail($"{GetType().FullName} prefix not match.");
                            return;
                        }

                        // read size
                        var sizeBlob = Stream.Read(sizeof(long), true);
                        _size = BitConverter.ToInt64(sizeBlob, 0);
                        sizeBlob.Dispose();

                        if (_size < MinSize || _size > MaxSize) // validate size
                        {
                            Fail($"{GetType().FullName} Invalid Size: {_size}, MinSize: {MinSize}, MaxSize: {MaxSize}");
                            return;
                        }
                    }

                    if (_size > 0 && Stream.Count >= _size)
                    {
                        var data = Stream.Read(_size, true);
                        _size = 0;
                        if (_readCallback == null) data.Dispose();
                        else _readCallback.Invoke(data);

                        // verify is still have data to process.
                        if (Stream.Count > 0) continue;
                    }

                    return;
                }
            }
        }

        public void OnRead(Action<Blob> callback)
        {
            lock (_locker)
            {
                _readCallback = callback;
            }
        }

        public void OnFail(Action<Exception> callback)
        {
            lock (_locker)
            {
                _failCallback = callback;
            }
        }

        private void Fail(string message)
        {
            lock (_locker)
            {
                IsActive = false;
                Clear();
                _failCallback?.Invoke(new Exception(message));
            }
        }
    }
}