using System;
using System.Net.Sockets;

namespace Netly
{
    public static partial class UDP
    {
        public partial class Client
        {
            public interface IOn : IOn<Socket>
            {
                void Data(Action<byte[]> callback);
                void Event(Action<string, byte[]> callback);
            }
        }
    }
}