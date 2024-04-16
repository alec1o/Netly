using System;

namespace Netly
{
    public interface IOn<out TModifyType>
    {
        /// <summary>
        ///     Handle Connection Opened
        /// </summary>
        /// <param name="callback">Callback</param>
        void Open(Action callback);

        /// <summary>
        ///     Handle error, before connection Opened
        /// </summary>
        /// <param name="callback">Callback</param>
        void Error(Action<Exception> callback);

        /// <summary>
        ///     Handle Connection Close
        /// </summary>
        /// <param name="callback">Callback</param>
        void Close(Action callback);

        /// <summary>
        ///     Handle
        /// </summary>
        /// <param name="callback">Callback</param>
        void Modify(Action<TModifyType> callback);
    }
}