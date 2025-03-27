using System;

namespace Netly.Interfaces
{
    public interface IOn<out T>
    {
        /// <summary>
        ///     Use to handle connection open event
        /// </summary>
        /// <param name="callback">Callback</param>
        void Open(Action callback);

        /// <summary>
        ///     Use to handle connection error event
        /// </summary>
        /// <param name="callback">Callback</param>
        void Error(Action<Exception> callback);

        /// <summary>
        ///     Use to handle connection close event
        /// </summary>
        /// <param name="callback">Callback</param>
        void Close(Action callback);

        /// <summary>
        ///     Use to handle socket modification event
        /// </summary>
        /// <param name="callback">Callback</param>
        void Modify(Action<T> callback);
    }
}