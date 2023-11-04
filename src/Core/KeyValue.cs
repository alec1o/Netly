namespace Netly.Core
{
    public struct KeyValue<TKey, TValue>
    {
        public TKey Key { get; internal set; }
        public TValue Value { get; internal set; }
        public KeyValue(TKey key, TValue value)
        {
            this.Key = key;
            this.Value = value;
        }
    }
}