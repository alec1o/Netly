using System;

namespace Netly.Core
{
    /// <summary>
    /// The module for encoding events
    /// </summary>
    public static class Events
    {
        private static readonly byte[] _prefix = Encode.GetBytes("NETLY://", Encode.Mode.ASSCI);

        /// <summary>
        /// Used to revert, check the event
        /// </summary>
        /// <param name="name">Event name</param>
        /// <param name="value">Event data/value</param>
        
        /// <returns>Returns the data "event" in byte format</returns>
        public static byte[] Create(string name, byte[] value)
        {
            Dict dict = new Dict();
            
            dict.Add(_prefix);
            dict.Add(name);
            dict.Add(data);
            
            return dict.GetBytes();
        }

        /// <summary>
        /// Used to revert, check the event
        /// </summary>
        /// <param name="value">Event/value in byte format</param>
        /// <returns>returns a (Tuple) containing the (name) and (value) of the event received</returns>
        public static (string name, byte[] value) Verify(byte[] value)
        {
            try
            {
                 if (value == null) return (null, null);
                 
                 Dict dict = new Dict();
                 dict.SetBytes(value);
                 
                 byte[] prefix = dict.Get(typeof(byte[]));
                 
                 if (prefix == null || !Compare.Bytes(prefix, _prefix)) return (null, null);
                 
                 string name = dict.Get(typeof(string));
                 
                 if (name == null) return (null, null);
                 
                 byte[] data = dict.Get(typeof(byte[]));
                 
                 if(data == null) return (null, null);
                 
                 return (name, data);
            }
            catch
            {
                return (null, null);
            }
        }
    }
}
