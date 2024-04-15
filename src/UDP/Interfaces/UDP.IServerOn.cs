using System;
using System.Net.Sockets;

namespace Netly
{
    public static partial class UDP
    {
        public interface IServerOn : IOn<Socket>
        {
            void Accept(Action<IClient> callback);
        }
    }
}