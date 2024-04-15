using System;
using System.Net.Sockets;

namespace Netly
{
    public static partial class UDP
    {
        public partial class Server
        {
            public interface IOn : IOn<Socket>
            {
                void Accept(Action<IClient> callback);
            }
        }
    }
}