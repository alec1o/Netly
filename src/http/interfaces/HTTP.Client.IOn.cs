using System;
using System.Net.Http;

namespace Netly
{
    public static partial class HTTP
    {
        public partial class Client
        {
            /// <summary>
            ///     HTTP.Client callbacks container
            /// </summary>
            public interface IOn
            {
                /// <summary>
                ///     Handle fetch response
                /// </summary>
                /// <param name="callback">Callback</param>
                void Open(Action<IResponse> callback);
                /// <summary>
                ///     Use to handle connection error event
                /// </summary>
                /// <param name="callback">Callback</param>
                void Error(Action<Exception> callback);

                /// <summary>
                ///     Use to handle connection close event
                /// </summary>
                /// <param name="callback">Callback</param>
                void Close(Action callback);

                /// <summary>
                ///     Use to handle socket modification event
                /// </summary>
                /// <param name="callback">Callback</param>
                void Modify(Action<HttpClient> callback);
            }
        }
    }
}