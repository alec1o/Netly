using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using Netly.Core;

namespace Netly.Features
{
    internal interface IHttpBody
    {
        /// <summary>
        /// Enctype type
        /// </summary>
        Enctype Enctype { get; }
        
        /// <summary>
        /// Text buffer
        /// </summary>
        string Text { get; }
        
        /// <summary>
        /// Binary buffer
        /// </summary>
        byte[] Binary { get; }
        
        /// <summary>
        /// Get value from Enctype content (return string)
        /// </summary>
        Dictionary<string, string> TextQueries { get; }
        
        /// <summary>
        /// Get value from Enctype content (return bytes)
        /// </summary>
        Dictionary<string, byte[]> BinaryQueries { get; }
    }

    internal interface IRequest
    {
        /// <summary>
        /// Request encoding
        /// </summary>
        NE.Mode Encoding { get; }

        /// <summary>
        /// Request Headers
        /// </summary>
        Dictionary<string, string> Headers { get; }
        
        /// <summary>
        /// Request Queries
        /// </summary>
        Dictionary<string, string> Queries { get; }
        
        /// <summary>
        /// Request Params
        /// </summary>
        Dictionary<string, string> Params { get; }
        
        /// <summary>
        /// Request Cookies
        /// </summary>
        Cookie[] Cookies { get; }
        
        /// <summary>
        /// Request Http Method
        /// </summary>
        HttpMethod Method { get; }
        
        /// <summary>
        /// Request Url
        /// </summary>
        string Url { get; }
        
        /// <summary>
        /// Request Path
        /// </summary>
        string Path { get; }
        
        /// <summary>
        /// Request (local end point)
        /// </summary>
        Host LocalEndPoint { get; }
        
        /// <summary>
        /// Request (remote end point)
        /// </summary>
        Host RemoteEndPoint { get; }
        
        /// <summary>
        /// Return true if request is websocket
        /// </summary>
        bool IsWebSocket { get; }
        
        /// <summary>
        /// Return true if request is same host
        /// </summary>
        bool IsLocalRequest { get; }
        
        /// <summary>
        /// Return true if connection is encrypted e.g SSL or TLS protocol
        /// </summary>
        bool IsEncrypted { get; }
        
        /// <summary>
        /// Request Body
        /// </summary>
        IHttpBody Body { get; }
    }


    internal interface IResponse
    {
        /// <summary>
        /// Response encoding
        /// </summary>
        NE.Mode Encoding { get; }

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

    internal interface IMiddleware
    {
        /// <summary>
        /// Middleware array
        /// </summary>
        Func<IRequest, IResponse, bool>[] Middlewares { get; }

        /// <summary>
        /// Add global middleware handler
        /// </summary>
        /// <param name="middleware">Middleware handler</param>
        /// <returns>true if callback added successful</returns>
        bool Add(Func<IRequest, IResponse, bool> middleware);

        /// <summary>
        /// Add local middleware handler
        /// </summary>
        /// <param name="path">Route path</param>
        /// <param name="middleware">Middleware handler</param>
        /// <returns>true if callback added successful</returns>
        /// <returns></returns>
        bool Add(string path, Func<IRequest, IResponse, bool> middleware);
    }
}