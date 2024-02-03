using System;

namespace Netly.Interfaces
{
    public partial class HTTP
    {
        public partial class Server
        {
            public interface IMap
            {
                /// <summary>
                /// Handle WebSocket from Path
                /// </summary>
                /// <param name="path">Request Path</param>
                /// <param name="callback">Response Callback</param>
                void WebSocket(string path, Action<IRequest, Features.HTTP.WebSocket> callback);

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
        }
    }
}