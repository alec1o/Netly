using System.Threading.Tasks;
using Netly.Core;

namespace Netly
{
    public static partial class UDP
    {
        public interface IClientTo
        {
            Task Open(Host host);
            Task Close();
            void Data(byte[] data);
            void Data(string data, NE.Encoding encoding = NE.Encoding.UTF8);
            void Event(string name, byte[] data);
            void Event(string name, string data, NE.Encoding encoding = NE.Encoding.UTF8);
        }
    }
}