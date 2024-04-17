using System;

namespace Netly
{
    public interface IOn<out TModifyType>
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
        void Modify(Action<TModifyType> callback);
    }
}