using System.Collections.Generic;
using System.Net;
using System.Net.WebSockets;
using System.Text;

namespace Netly.Interfaces
{
    public static partial class IHTTP
    {
        public interface ServerResponse
        {
            /// <summary>
            ///     Native Response Object
            /// </summary>
            HttpListenerResponse NativeResponse { get; }
            
            /// <summary>
            ///     WebSocket Context Object (valid only when 'ServerRequest.IsWebSocket is <i>true</i>', otherwise is 'null')
            /// </summary>
            HttpListenerWebSocketContext WebSocketContext { get; }

            /// <summary>
            ///     Response Headers
            /// </summary>
            Dictionary<string, string> Headers { get; }

            /// <summary>
            ///     Response Cookies
            /// </summary>
            Cookie[] Cookies { get; }

            /// <summary>
            ///     Response encoding
            /// </summary>
            Encoding Encoding { get; set; }

            /// <summary>
            ///     Return true if response connection is opened
            /// </summary>
            bool IsOpened { get; }

            /// <summary>
            ///     Send response data (empty)
            /// </summary>
            /// <param name="statusCode">http status code</param>
            void Send(int statusCode);

            /// <summary>
            ///     Write data on final buffer
            /// </summary>
            /// <param name="byteBuffer">data</param>
            void Write(byte[] byteBuffer);

            /// <summary>
            ///     Write data on final buffer
            /// </summary>
            /// <param name="textBuffer">data</param>
            void Write(string textBuffer);

            /// <summary>
            ///     Send response data (string)
            /// </summary>
            /// <param name="statusCode">http status code</param>
            /// <param name="textBuffer">response data</param>
            void Send(int statusCode, string textBuffer);

            /// <summary>
            ///     Send response data (bytes)
            /// </summary>
            /// <param name="statusCode">http status code</param>
            /// <param name="byteBuffer">response data</param>
            void Send(int statusCode, byte[] byteBuffer);

            /// <summary>
            ///     Redirect connection for an url.<br />Using
            /// </summary>
            /// <param name="url">redirect location</param>
            void Redirect(string url);

            /// <summary>
            ///     Redirect connection for an url.<br />Using
            /// </summary>
            /// <param name="redirectCode">redirect http code</param>
            /// <param name="url">redirect location</param>
            void Redirect(int redirectCode, string url);

            /// <summary>
            ///     Close connection
            /// </summary>
            void Close();
        }
    }
}