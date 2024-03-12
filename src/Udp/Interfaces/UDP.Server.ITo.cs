using Netly.Core;

namespace Netly
{
    public static partial class UDP
    {
        public partial class Server
        {
            public interface ITo
            {
                void Open(Host host);
                void Close();
            }
        }
    }
}