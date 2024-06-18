using System;

namespace Netly.Interfaces
{
    public static partial class IHTTP
    {
        public interface Map
        {
            /// <summary>
            ///     Handle WebSocket from Path
            /// </summary>
            /// <param name="path">Request Path</param>
            /// <param name="callback">Response Callback</param>
            void WebSocket(string path, Action<Request, WebSocket> callback);

            /// <summary>
            ///     Handle All Http Method from Path
            /// </summary>
            /// <param name="path">Request Path</param>
            /// <param name="callback">Response Callback</param>
            void All(string path, Action<Request, ServerResponse> callback);

            /// <summary>
            ///     Handle (Get) Http Method
            /// </summary>
            /// <param name="path">Request Path</param>
            /// <param name="callback">Response Callback</param>
            void Get(string path, Action<Request, ServerResponse> callback);

            /// <summary>
            ///     Handle (Put) Http Method
            /// </summary>
            /// <param name="path">Request Path</param>
            /// <param name="callback">Response Callback</param>
            void Put(string path, Action<Request, ServerResponse> callback);

            /// <summary>
            ///     Handle (Head) Http Method
            /// </summary>
            /// <param name="path">Request Path</param>
            /// <param name="callback">Response Callback</param>
            void Head(string path, Action<Request, ServerResponse> callback);

            /// <summary>
            ///     Handle (Post) Http Method
            /// </summary>
            /// <param name="path">Request Path</param>
            /// <param name="callback">Response Callback</param>
            void Post(string path, Action<Request, ServerResponse> callback);

            /// <summary>
            ///     Handle (Patch) Http Method
            /// </summary>
            /// <param name="path">Request Path</param>
            /// <param name="callback">Response Callback</param>
            void Patch(string path, Action<Request, ServerResponse> callback);

            /// <summary>
            ///     Handle (Delete) Http Method
            /// </summary>
            /// <param name="path">Request Path</param>
            /// <param name="callback">Response Callback</param>
            void Delete(string path, Action<Request, ServerResponse> callback);

            /// <summary>
            ///     Handle (Trace) Http Method
            /// </summary>
            /// <param name="path">Request Path</param>
            /// <param name="callback">Response Callback</param>
            void Trace(string path, Action<Request, ServerResponse> callback);

            /// <summary>
            ///     Handle (Options) Http Method
            /// </summary>
            /// <param name="path">Request Path</param>
            /// <param name="callback">Response Callback</param>
            void Options(string path, Action<Request, ServerResponse> callback);
        }
    }
}