using System;

namespace Netly
{
    public static partial class HTTP
    {
        public partial class Client
        {
            /// <summary>
            ///     HTTP.Client callbacks container
            /// </summary>
            public interface IOn : IOn<System.Net.Http.HttpClient>
            {
                /// <summary>
                ///     Handle fetch response
                /// </summary>
                /// <param name="callback">Callback</param>
                void Fetch(Action<IRequest> callback);
            }
        }
    }
}