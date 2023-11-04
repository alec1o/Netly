using System.Collections.Generic;
using Netly.Core;

namespace Netly
{
    /// <summary>
    /// Http Header Container
    /// </summary>
    public class Headers
    {
        private readonly List<KeyValue<string, string>> _headers;
        private readonly object _lock = new object();

        /// <summary>
        /// Return all headers
        /// </summary>
        public KeyValue<string, string>[] AllHeaders => GetAllHeaders();
        
        /// <summary>
        /// Return the size (count) of all headers
        /// </summary>
        public int Count => GetLength();
        
        /// <summary>
        /// Return the size (length) of all headers
        /// </summary>
        public int Length => GetLength();

        /// <summary>
        /// Create new instance
        /// </summary>
        public Headers()
        {
            _headers = new List<KeyValue<string, string>>();
        }

        
        /// <summary>
        /// Return the amount of all headers
        /// </summary>
        /// <returns></returns>
        public int GetLength()
        {
            lock (_lock)
            {
                return _headers.Count;
            }
        }

        /// <summary>
        /// Return all headers
        /// </summary>
        /// <returns></returns>
        public KeyValue<string, string>[] GetAllHeaders()
        {
            lock (_lock)
            {
                return _headers.ToArray();
            }
        }

        /// <summary>
        /// Add a header at container
        /// </summary>
        /// <param name="name">Header name (Key)</param>
        /// <param name="value">Header data (Value)</param>
        public void Add(string name, string value)
        {
            if (string.IsNullOrWhiteSpace(name)) return;

            lock (_lock)
            {
                Remove(name);
                _headers.Add(new KeyValue<string, string>(name, value));
            }
        }

        /// <summary>
        /// Remove a header if exist
        /// </summary>
        /// <param name="name">Header name (Key)</param>
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

        /// <summary>
        /// Return value (data) of some header. <br/>
        /// </summary>
        /// <param name="name">Header name (Key)</param>
        /// <returns>It return value (data) of a header name (key). <br/> Warning it return Empty (null) when data not found or header value (data) is Empty (White Space)</returns>
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