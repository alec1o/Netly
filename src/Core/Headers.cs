using System.Collections.Generic;
using Netly.Core;

namespace Netly
{
    public class Headers
    {
        private readonly List<KeyValue<string, string>> _headers;
        private readonly object _lock = new object();

        public KeyValue<string, string>[] AllHeaders => GetAllHeaders();
        public int Count => GetLength();
        public int Length => GetLength();

        public Headers()
        {
            _headers = new List<KeyValue<string, string>>();
        }


        public int GetLength()
        {
            lock (_lock)
            {
                return _headers.Count;
            }
        }

        public KeyValue<string, string>[] GetAllHeaders()
        {
            lock (_lock)
            {
                return _headers.ToArray();
            }
        }

        public void Add(string name, string value)
        {
            if (string.IsNullOrWhiteSpace(name)) return;

            lock (_lock)
            {
                Remove(name);
                _headers.Add(new KeyValue<string, string>(name, value));
            }
        }

        public void Remove(string name)
        {
            if (!string.IsNullOrEmpty(Get(name)))
            {
                lock (_lock)
                {
                    foreach (var kv in _headers)
                    {
                        if (kv.Key.ToLower() == name.ToLower())
                        {
                            _headers.Remove(kv);
                        }
                    }
                }
            }
        }

        public string Get(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return string.Empty;

            lock (_lock)
            {
                foreach (var kv in _headers)
                {
                    if (kv.Key.ToLower() == name.ToLower())
                    {
                        return kv.Value;
                    }
                }
            }

            return string.Empty;
        }
    }
}