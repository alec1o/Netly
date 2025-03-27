﻿using System.Text;
using System.Threading.Tasks;

namespace Netly.Interfaces
{
    public static partial class ITCP
    {
        public interface ClientTo
        {
            Task Open(Host host);
            Task Close();
            void Data(byte[] data);
            void Encryption(bool enable);
            void Data(string data);
            void Data(string data, Encoding encoding);
            void Event(string name, byte[] data);
            void Event(string name, string data);
            void Event(string name, string data, Encoding encoding);
        }
    }
}