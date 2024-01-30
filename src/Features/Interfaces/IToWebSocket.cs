using System;

namespace Netly.Features
{
    public interface IToWebSocket
    {
        /// <summary>
        /// Open Client Connection
        /// </summary>
        /// <param name="host">Server Uri</param>
        void Open(Uri host);

        /// <summary>
        /// Close Client Connection
        /// </summary>
        void Close();

        /// <summary>
        /// Send data for server (bytes)
        /// </summary>
        /// <param name="buffer">Data buffer</param>
        /// <param name="isText">"True" meaning is Text format</param>
        void Data(byte[] buffer, bool isText);

        /// <summary>
        /// Send data for server (string)
        /// </summary>
        /// <param name="buffer">Data buffer</param>
        /// <param name="isText">"True" meaning is Text format</param>
        void Data(string buffer, bool isText);

        /// <summary>
        /// Send Netly event for server (bytes)
        /// </summary>
        /// <param name="name">Event name</param>
        /// <param name="buffer">Event buffer</param>
        void Event(string name, byte[] buffer);

        /// <summary>
        /// Send Netly event for server (string)
        /// </summary>
        /// <param name="name">Event name</param>
        /// <param name="buffer">Event buffer</param>
        void Event(string name, string buffer);
    }
}