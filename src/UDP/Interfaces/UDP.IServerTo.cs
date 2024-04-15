using System.Threading.Tasks;
using Netly.Core;

namespace Netly
{
    public static partial class UDP
    {
        public interface IServerTo
        {
            Task Open(Host host);
            Task Close();
        }
    }
}