using System;

namespace Netly.Core
{
    /// <summary>
    /// The module for encoding events
    /// </summary>
    public static class Events
    {
        private static readonly byte[] Prefix = Encode.GetBytes(":: NETL ::", Encode.Mode.ASSCI);

        /// <summary>
        /// Used to revert, check the event
        /// </summary>
        /// <param name="name">Event name</param>
        /// <param name="value">Event data/value</param>
        /// <returns>Returns the data "event" in byte format</returns>
        public static byte[] Create(string name, byte[] value)
        {
            byte[] data = Encode.GetBytes(name, Encode.Mode.UTF8);
            byte[] size = BitConverter.GetBytes(data.Length);
            
            return Concat.Bytes(Prefix, size, data, value);
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
                int index = 0;

                #region Verify Prefix

                if (value == null || value.Length < Prefix.Length + 6)
                {
                    return (String.Empty, new byte[0]);
                }

                var prefix = new byte[Prefix.Length];
                Buffer.BlockCopy(value, 0, prefix, 0, Prefix.Length);

                if(!Compare.Bytes(Prefix, prefix))
                {
                    return (String.Empty, new byte[0]);
                }

                index += Prefix.Length;

                #endregion

                #region Get Name Size

                int nameSize = BitConverter.ToInt32(value, index);

                if (nameSize > value.Length - index)
                {
                    return (String.Empty, new byte[0]);
                }

                index += sizeof(int);

                #endregion

                #region Get Name

                var nameBytes = new byte[nameSize];
                Buffer.BlockCopy(value, index, nameBytes, 0, nameSize);

                var name = Encode.GetString(nameBytes, Encode.Mode.UTF8);

                index += nameBytes.Length;

                #endregion

                #region Get Data

                var data = new byte[value.Length - index];
                Array.Copy(value, index, data, 0, data.Length);

                #endregion

                return (name, data);
            }
            catch
            {
                return (String.Empty, new byte[0]);
            }
        }
    }
}
