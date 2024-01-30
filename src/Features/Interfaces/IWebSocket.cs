using System;

namespace Netly.Interfaces
{
    internal interface IWebSocket
    {
        /// <summary>
        /// Return true if connection is opened
        /// </summary>
        bool IsOpened { get; }

        /// <summary>
        /// Client Uri
        /// </summary>
        Uri Host { get; }

        /// <summary>
        /// Event Handler
        /// </summary>
        IOnWebSocket On { get; }

        /// <summary>
        /// Event Creator
        /// </summary>
        IToWebSocket To { get; }
    }
}