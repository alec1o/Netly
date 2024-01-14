namespace Netly.Core
{
    /// <summary>
    /// Generic Key-and-Value Implementation
    /// </summary>
    /// <typeparam name="TKey"></typeparam>
    /// <typeparam name="TValue"></typeparam>
    public struct KeyValue<TKey, TValue>
    {
        /// <summary>
        /// Key (name) 
        /// </summary>
        public TKey Key { get; internal set; }

        /// <summary>
        /// Value (data)
        /// </summary>
        public TValue Value { get; internal set; }

        /// <summary>
        /// Create instance of Generic Key-Value
        /// </summary>
        /// <param name="key">Key (Name)</param>
        /// <param name="value">Value (Data)</param>
        public KeyValue(TKey key, TValue value)
        {
            this.Key = key;
            this.Value = value;
        }

        public override string ToString()
        {
            return "{" + $"\"{Key}\":\"{Value}\"" + "}";
        }
    }
}