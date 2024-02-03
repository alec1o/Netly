﻿using Netly.Core;

namespace Netly.Interfaces
{
    public partial class HTTP
    {
        public interface IResponse
        {
            /// <summary>
            /// Response encoding
            /// </summary>
            NE.Encoding Encoding { get; set; }

            /// <summary>
            /// Return true if response connection is opened
            /// </summary>
            bool IsOpened { get; }
            
            /// <summary>
            /// Send response data (string)
            /// </summary>
            /// <param name="statusCode">http status code</param>
            /// <param name="textBuffer">response data</param>
            void Send(int statusCode, string textBuffer);

            /// <summary>
            /// Send response data (bytes)
            /// </summary>
            /// <param name="statusCode">http status code</param>
            /// <param name="byteBuffer">response data</param>
            void Send(int statusCode, byte[] byteBuffer);

            /// <summary>
            /// Redirect connection for a url.<br/>Using 
            /// </summary>
            /// <param name="url">redirect location</param>
            void Redirect(string url);

            /// <summary>
            /// Redirect connection for a url.<br/>Using 
            /// </summary>
            /// <param name="redirectCode">redirect http code</param>
            /// <param name="url">redirect location</param>
            void Redirect(int redirectCode, string url);

            /// <summary>
            /// Close connection
            /// </summary>
            void Close();
        }
    }
}