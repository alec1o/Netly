using System.Net.Security;
using System.Net.Sockets;

namespace Netly.Interfaces
{
    public static partial class ITCP
    {
        public interface Client
        {
            string Id { get; }
            NHost Host { get; }
            Socket Socket { get; }
            NetworkStream NetworkStream { get; }
            SslStream SslStream { get; }
            IFraming Framing { get; }
            bool IsOpened { get; }
            bool IsFraming { get; }
            bool IsEncrypted { get; }
            ClientTo To { get; }
            ClientOn On { get; }
            
        }
    }
}