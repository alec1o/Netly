using System.Threading.Tasks;
using Netly.Core;

namespace Netly
{
    public static partial class UDP
    {
        public partial class Server
        {
            public interface ITo
            {
                Task Open(Host host);
                Task Close();
            }
        }
    }
}