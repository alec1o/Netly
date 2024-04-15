using System;
using System.Net.Sockets;

namespace Netly
{
    public static partial class UDP
    {
        public interface IClientOn : IOn<Socket>
        {
            void Data(Action<byte[]> callback);
            void Event(Action<string, byte[]> callback);
        }
    }
}