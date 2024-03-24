using System;
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
            }
        }
    }
}