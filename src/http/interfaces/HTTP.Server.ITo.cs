using System;
using System.Text;
using System.Threading.Tasks;

namespace Netly
{
    public static partial class HTTP
    {
        public partial class Server
        {
            /// <summary>
            ///     HTTP.Server action creator container
            /// </summary>
            public interface ITo
            {
                /// <summary>
                ///     Open Server Connection
                /// </summary>
                /// <param name="host">Server Uri</param>
                Task Open(Uri host);


                /// <summary>
                ///     Close Server Connection
                /// </summary>
                Task Close();

                void WebsocketDataBroadcast(byte[] data, bool isText);
                void WebsocketDataBroadcast(string data, bool isText);
                void WebsocketDataBroadcast(string data, bool isText, Encoding encoding);

                void WebsocketEventBroadcast(string name, byte[] data);
                void WebsocketEventBroadcast(string name, string data);
                void WebsocketEventBroadcast(string name, string data, Encoding encoding);
            }
        }
    }
}