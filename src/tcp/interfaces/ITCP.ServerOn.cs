using System;
using System.Net.Sockets;

namespace Netly.Interfaces
{
    public static partial class ITCP
    {
        public interface ServerOn : IOn<Socket>
        {
            void Accept(Action<Client> callback);
        }
    }
}