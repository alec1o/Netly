using System.Collections.Generic;
using Netly.Core;

namespace Netly
{
    /// <summary>
    /// KeyValue Element Container
    /// </summary>
    public class KeyValueContainer
    {
        private readonly List<KeyValue<string, string>> _list;
        private readonly object _lock = new object();

        /// <summary>
        /// Return all KV elements
        /// </summary>
        public KeyValue<string, string>[] AllKeyValue => GetAllKeyValue();
        
        /// <summary>
        /// Return the size (count) of all KV elements
        /// </summary>
        public int Count => GetLength();
        
        /// <summary>
        /// Return the size (length) of all KV elements
        /// </summary>
        public int Length => GetLength();

        /// <summary>
        /// Create new instance
        /// </summary>
        public KeyValueContainer()
        {
            _list = new List<KeyValue<string, string>>();
        }

        
        /// <summary>
        /// Return the amount of all KV element
        /// </summary>
        /// <returns></returns>
        public int GetLength()
        {
            lock (_lock)
            {
                return _list.Count;
            }
        }

        /// <summary>
        /// Return all KV element
        /// </summary>
        /// <returns></returns>
        public KeyValue<string, string>[] GetAllKeyValue()
        {
            lock (_lock)
            {
                return _list.ToArray();
            }
        }

        /// <summary>
        /// Add a KV element in container
        /// </summary>
        /// <param name="name">name (Key)</param>
        /// <param name="value">data (Value)</param>
        public void Add(string name, string value)
        {
            if (string.IsNullOrWhiteSpace(name)) return;

            lock (_lock)
            {
                Remove(name);
                _list.Add(new KeyValue<string, string>(name, value));
            }
        }

        /// <summary>
        /// Remove a KV element if exist
        /// </summary>
        /// <param name="name">name (Key)</param>
        public void Remove(string name)
        {
            if (!string.IsNullOrEmpty(Get(name)))
            {
                lock (_lock)
                {
                    foreach (var kv in _list)
                    {
                        if (kv.Key.ToLower() == name.ToLower())
                        {
                            _list.Remove(kv);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Return value (data) of some KV element. <br/>
        /// </summary>
        /// <param name="name">name (Key)</param>
        /// <returns>It return value (data) of a name (key). <br/> Warning it return Empty (null) when data not found or KV element value (data) is Empty (White Space)</returns>
        public string Get(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return string.Empty;

            lock (_lock)
            {
                foreach (var kv in _list)
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