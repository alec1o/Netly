using System;
using System.Net.WebSockets;
using System.Threading.Tasks;

namespace Netly
{
    public static partial class HTTP
    {
        public partial class WebSocket
        {
            public interface ITo
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
                /// <param name="isText">"True" meaning is Text format</param>
                void Data(byte[] buffer, bool isText);

                /// <summary>
                ///     Send data for server (string)
                /// </summary>
                /// <param name="buffer">Data buffer</param>
                /// <param name="isText">"True" meaning is Text format</param>
                void Data(string buffer, bool isText);

                /// <summary>
                ///     Send Netly event for server (bytes)
                /// </summary>
                /// <param name="name">Event name</param>
                /// <param name="buffer">Event buffer</param>
                void Event(string name, byte[] buffer);

                /// <summary>
                ///     Send Netly event for server (string)
                /// </summary>
                /// <param name="name">Event name</param>
                /// <param name="buffer">Event buffer</param>
                void Event(string name, string buffer);
            }
        }
    }
}