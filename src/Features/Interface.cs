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
        Dictionary<string, Func<IRequest, IResponse, bool>>[] Middlewares { get; }

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

    internal interface IMap
    {
        /// <summary>
        /// Handle WebSocket from Path
        /// </summary>
        /// <param name="path">Request Path</param>
        /// <param name="callback">Response Callback</param>
        void WebSocket(string path, Action<IRequest, IWebsocketClient> callback);

        /// <summary>
        /// Handle All Http Method from Path
        /// </summary>
        /// <param name="path">Request Path</param>
        /// <param name="callback">Response Callback</param>
        void All(string path, Action<IRequest, IResponse> callback);

        /// <summary>
        /// Handle (Get) Http Method
        /// </summary>
        /// <param name="path">Request Path</param>
        /// <param name="callback">Response Callback</param>
        void Get(string path, Action<IRequest, IResponse> callback);

        /// <summary>
        /// Handle (Put) Http Method
        /// </summary>
        /// <param name="path">Request Path</param>
        /// <param name="callback">Response Callback</param>
        void Put(string path, Action<IRequest, IResponse> callback);

        /// <summary>
        /// Handle (Head) Http Method
        /// </summary>
        /// <param name="path">Request Path</param>
        /// <param name="callback">Response Callback</param>
        void Head(string path, Action<IRequest, IResponse> callback);

        /// <summary>
        /// Handle (Post) Http Method
        /// </summary>
        /// <param name="path">Request Path</param>
        /// <param name="callback">Response Callback</param>
        void Post(string path, Action<IRequest, IResponse> callback);

        /// <summary>
        /// Handle (Patch) Http Method
        /// </summary>
        /// <param name="path">Request Path</param>
        /// <param name="callback">Response Callback</param>
        void Patch(string path, Action<IRequest, IResponse> callback);

        /// <summary>
        /// Handle (Delete) Http Method
        /// </summary>
        /// <param name="path">Request Path</param>
        /// <param name="callback">Response Callback</param>
        void Delete(string path, Action<IRequest, IResponse> callback);

        /// <summary>
        /// Handle (Trace) Http Method
        /// </summary>
        /// <param name="path">Request Path</param>
        /// <param name="callback">Response Callback</param>
        void Trace(string path, Action<IRequest, IResponse> callback);

        /// <summary>
        /// Handle (Options) Http Method
        /// </summary>
        /// <param name="path">Request Path</param>
        /// <param name="callback">Response Callback</param>
        void Options(string path, Action<IRequest, IResponse> callback);
    }

    internal interface IOn<out TModifyType>
    {
        /// <summary>
        /// Handle Connection Opened
        /// </summary>
        /// <param name="callback">Callback</param>
        void Open(Action callback);

        /// <summary>
        /// Handle error, before connection Opened
        /// </summary>
        /// <param name="callback">Callback</param>
        void Error(Action<Exception> callback);

        /// <summary>
        /// Handle Connection Close
        /// </summary>
        /// <param name="callback">Callback</param>
        void Close(Action callback);

        /// <summary>
        /// Handle 
        /// </summary>
        /// <param name="callback">Callback</param>
        void Modify(Action<TModifyType> callback);
    }

    internal interface IHttpServer
    {
        /// <summary>
        /// Return true if connection is opened
        /// </summary>
        bool IsOpened { get; }

        /// <summary>
        /// Server Uri
        /// </summary>
        Uri Host { get; }

        /// <summary>
        /// Middleware Object
        /// </summary>
        IMiddleware Middleware { get; }

        /// <summary>
        /// Map Object
        /// </summary>
        IMap Map { get; }

        /// <summary>
        /// Callback Handler
        /// </summary>
        IOn<HttpListener> On { get; }


        /// <summary>
        /// Open Server Connection
        /// </summary>
        /// <param name="host">Server Uri</param>
        void Open(Uri host);


        /// <summary>
        /// Close Server Connection
        /// </summary>
        void Close();
    }


    internal interface IOnHttpClient : IOn<System.Net.Http.HttpClient>
    {
        /// <summary>
        /// Handle Http Successful Request
        /// </summary>
        /// <param name="request"></param>
        void Data(Action<Request> request);
    }

    internal interface IHttpClient
    {
        /// <summary>
        /// Request Timeout
        /// </summary>
        int Timeout { get; set; }
        
        /// <summary>
        /// Request Method
        /// </summary>
        HttpMethod Method { get; }
        
        /// <summary>
        /// Request Headers
        /// </summary>
        Dictionary<string, string> Headers { get; }
        
        /// <summary>
        /// Request Queries
        /// </summary>
        Dictionary<string, string> Queries { get; }
        
        /// <summary>
        /// Request Body
        /// </summary>
        IHttpBody Body { get; set; }
        
        /// <summary>
        /// Event Handler
        /// </summary>
        IOnHttpClient On { get; }
        
        /// <summary>
        /// Send Request Method
        /// </summary>
        /// <param name="method">Http Method</param>
        /// <param name="host">URI (Url Container)</param>
        void Send(string method, Uri host);

    internal interface IOnWebSocket : IOn<ClientWebSocket>
    {
        /// <summary>
        /// Handle data received
        /// </summary>
        /// <param name="callback">Callback</param>
        void Data(Action<byte[], bool> callback);

        /// <summary>
        /// Handle (netly event) received
        /// </summary>
        /// <param name="callback">Callback</param>
        void Event(Action<string, byte[], bool> callback);

        /// <summary>
        /// Handle connection closed
        /// </summary>
        /// <param name="callback">Callback</param>
        void Close(Action<WebSocketCloseStatus> callback);
    }

    interface IToWebSocket
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

    internal interface IWebSocket
    {
        /// <summary>
        /// Return true if connection is opened
        /// </summary>
        bool IsOpened { get; }

        /// <summary>
        /// Client Uri
        /// </summary>
        Uri Host { get; }

        /// <summary>
        /// Event Handler
        /// </summary>
        IOnWebSocket On { get; }

        /// <summary>
        /// Event Creator
        /// </summary>
        IToWebSocket To { get; }
    }
}