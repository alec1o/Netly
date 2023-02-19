using Netly.Core;
using System;
using System.Net.Sockets;
using System.Threading;

namespace Netly.Abstract
{
    public class TcpServers : Server<TcpClient>, IServer<TcpClient>
    {
    }
}
