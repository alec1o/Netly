using System;
using System.Net.WebSockets;
using System.Text;
using System.Threading.Tasks;

namespace Netly.Interfaces
{
    public static partial class IHTTP
    {
        public interface WebSocketTo
        {
            /// <summary>
            ///     Open Client Connection
            /// </summary>
            /// <param name="host">Server Uri</param>
            Task Open(Uri host);

            /// <summary>
            ///     Close Client Connection
            /// </summary>
            Task Close();

            /// <summary>
            ///     Close Client Connection (with Close Status)
            /// </summary>
            /// <param name="closeStatus">Close Status</param>
            Task Close(WebSocketCloseStatus closeStatus);

            /// <summary>
            ///     Send data for server (bytes)
            /// </summary>
            /// <param name="buffer">Data buffer</param>
            /// <param name="messageType">Message Type (Binary|Text)</param>
            void Data(byte[] buffer, HTTP.MessageType messageType);

            /// <summary>
            ///     Send data for server (string)
            /// </summary>
            /// <param name="buffer">Data buffer</param>
            /// <param name="messageType">Message Type (Binary|Text)</param>
            void Data(string buffer, HTTP.MessageType messageType);

            /// <summary>
            ///     Send data for server (string)
            /// </summary>
            /// <param name="buffer">Data buffer</param>
            /// <param name="messageType">Message Type (Binary|Text)</param>
            /// <param name="encoding">String encoding</param>
            void Data(string buffer, HTTP.MessageType messageType, Encoding encoding);

            /// <summary>
            ///     Send Netly event for server (bytes)
            /// </summary>
            /// <param name="name">Event name</param>
            /// <param name="buffer">Event buffer</param>
            void Event(string name, byte[] buffer, HTTP.MessageType messageType);

            /// <summary>
            ///     Send Netly event for server (string)
            /// </summary>
            /// <param name="name">Event name</param>
            /// <param name="buffer">Event buffer</param>
            void Event(string name, string buffer, HTTP.MessageType messageType);

            /// <summary>
            ///     Send Netly event for server (string)
            /// </summary>
            /// <param name="name">Event name</param>
            /// <param name="buffer">Event buffer</param>
            /// <param name="encoding">String encoding</param>
            void Event(string name, string buffer, HTTP.MessageType messageType, Encoding encoding);
        }
    }
}