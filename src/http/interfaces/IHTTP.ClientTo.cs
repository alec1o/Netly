using System.Text;
using System.Threading.Tasks;

namespace Netly.Interfaces
{
    public static partial class IHTTP
    {
        /// <summary>
        ///     HTTP.Client action creator container
        /// </summary>
        public interface ClientTo
        {
            /// <summary>
            ///     Create http fetch <br />
            ///     + Only if(IsOpened==false)
            /// </summary>
            /// <param name="method">Http method</param>
            /// <param name="url">Fetch url</param>
            Task Open(string method, string url);
            
            /// <summary>
            ///     Create http fetch <br />
            ///     + Only if(IsOpened==false)
            /// </summary>
            /// <param name="method">Http method</param>
            /// <param name="url">Fetch url</param>
            /// <param name="body">Request body</param>
            Task Open(string method, string url, byte[] body);

            /// <summary>
            ///     Create http fetch <br />
            ///     + Only if(IsOpened==false)
            /// </summary>
            /// <param name="method">Http method</param>
            /// <param name="url">Fetch url</param>
            /// <param name="body">Request body</param>
            Task Open(string method, string url, string body);

            /// <summary>
            ///     Create http fetch <br />
            ///     + Only if(IsOpened==false)
            /// </summary>
            /// <param name="method">Http method</param>
            /// <param name="url">Fetch url</param>
            /// <param name="body">Request body</param>
            /// <param name="encoding">Body encoding algorithm</param>
            Task Open(string method, string url, string body, Encoding encoding);

            /// <summary>
            ///     Cancel opened operation
            /// </summary>
            /// <returns></returns>
            Task Close();
        }
    }
}