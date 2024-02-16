using System;
using Netly.Core;

namespace Netly
{
    public static partial class HTTP
    {
        public partial class Client
        {
            /// <summary>
            ///     HTTP.Client action creator container
            /// </summary>
            public interface ITo
            {
                /// <summary>
                ///     Create http fetch <br/>
                ///     + Only if(IsOpened==false)
                /// </summary>
                /// <param name="method">Http method</param>
                /// <param name="url">Fetch url</param>
                /// <param name="body">Request body</param>
                void Fetch(string method, string url, byte[] body = null);

                /// <summary>
                ///     Create http fetch <br/>
                ///     + Only if(IsOpened==false)
                /// </summary>
                /// <param name="method">Http method</param>
                /// <param name="url">Fetch url</param>
                /// <param name="body">Request body</param>
                /// <param name="encode">Body encoding algorithm</param>
                void Fetch(string method, string url, string body = null,NE.Encoding encode = NE.Encoding.UTF8);
            }
        }
    }
}