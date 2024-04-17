using System;
using System.Net.Sockets;

namespace Netly.Interfaces
{
    public static partial class UDP
    {
        /// <summary>
        ///     UDP-Client callbacks container (interface)
        /// </summary>
        public interface IClientOn : IOn<Socket>
        {
            /// <summary>
            ///     Handle received raw data
            /// </summary>
            /// <param name="callback">Callback function</param>
            void Data(Action<byte[]> callback);

            /// <summary>
            ///     Handle received event (netly event)
            /// </summary>
            /// <param name="callback">Callback function</param>
            void Event(Action<string, byte[]> callback);
        }
    }
}