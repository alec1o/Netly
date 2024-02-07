using System;

namespace Netly.Features
{
    internal interface IToHttpClient
    {
        /// <summary>
        /// Send Request Method
        /// </summary>
        /// <param name="method">Http Method</param>
        /// <param name="host">URI (Url Container)</param>
        void Open(string method, Uri host);

        /// <summary>
        /// Force connection close
        /// </summary>
        void Close();
    }
}