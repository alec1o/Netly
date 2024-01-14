using System.Collections.Generic;
using Netly.Core;
using System.Linq;

namespace Netly
{
    /// <summary>
    /// KeyValue Element Container
    /// </summary>
    public class KeyValueContainer<T>
    {
        private readonly List<KeyValue<string, T>> _list;
        private readonly object _lock = new object();

        /// <summary>
        /// Return all KV elements
        /// </summary>
        public KeyValue<string, T>[] AllKeyValue => GetAllKeyValue();

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
            _list = new List<KeyValue<string, T>>();
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
        public KeyValue<string, T>[] GetAllKeyValue()
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
        public void Add(string name, T value)
        {
            if (string.IsNullOrWhiteSpace(name)) return;

            lock (_lock)
            {
                Remove(name);
                _list.Add(new KeyValue<string, T>(name, value));
            }
        }

        /// <summary>
        /// Add a KV elements in container
        /// </summary>
        /// <param name="elements">element array</param>
        public void AddRange(params KeyValue<string, T>[] elements)
        {
            var elementList = elements.ToList().FindAll(x => !string.IsNullOrEmpty(x.Key));

            if (elementList.Count > 0)
            {
                lock (_lock)
                {
                    _list.AddRange(elementList);
                }
            }
        }

        /// <summary>
        /// Remove a KV element if exist
        /// </summary>
        /// <param name="name">name (Key)</param>
        /// <returns>return true if element is removed</returns>
        public bool Remove(string name)
        {
            lock (_lock)
            {
                int index = _list.FindIndex((x) => x.Key == name);
                if (index >= 0)
                {
                    _list.RemoveAt(index);
                }

                return index >= 0;
            }
        }

        /// <summary>
        /// Return true if key exist
        /// </summary>
        /// <param name="name">name (Key)</param>
        /// <returns></returns>
        public bool ExistKey(string name)
        {
            lock (_lock)
            {
                var result = _list.FirstOrDefault((x) => x.Key == name);
                return !string.IsNullOrEmpty(result.Key);
            }
        }

        /// <summary>
        /// Return value (data) of some KV element. <br/>
        /// </summary>
        /// <param name="name">name (Key)</param>
        /// <returns>It return value (data) of a name (key). <br/> Warning it return Empty (null) when data not found or KV element value (data) is Empty (White Space)</returns>
        public T Get(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return default;

            lock (_lock)
            {
                return _list.FirstOrDefault((x) => x.Key == name).Value;
            }
        }

        public override string ToString()
        {
            string elements = "";
            bool isFirst = true;

            foreach (var e in AllKeyValue)
            {
                if (!isFirst) elements += ", ";
                isFirst = false;

                elements += e.ToString();
            }

            string str = "{" + $"\"Length\":{Length}, \"Elements\":[{elements}]" + "}";

            return str;
        }
    }
}