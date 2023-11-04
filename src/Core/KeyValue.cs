using System;

namespace Netly.Core
{
    public struct KeyValue<KeyType, ValueType>
    {
        public KeyType Key { get; internal set; }
        public ValueType Value { get; internal set; }
            
        public KeyValue(KeyType key, ValueType value)
        {
            this.Key = key;
            this.Value = value;
        }
    }
}