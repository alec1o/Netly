using System;

namespace Netly
{
    public static partial class NETLY
    {
        /// <summary>
        /// Netly. Internal actions logger.
        /// </summary>
        public interface ILogger
        {
            /// <summary>
            /// Push a log (regular)
            /// </summary>
            /// <param name="log">Message</param>
            void PushLog(string log);

            /// <summary>
            /// Push a log (warning)
            /// </summary>
            /// <param name="warning">Warning message</param>
            void PushWarning(string warning);

            /// <summary>
            /// Push a log (exception)
            /// </summary>
            /// <param name="exception">Exception object</param>
            void PushError(Exception exception);

            /// <summary>
            /// Handle (regular log) callback
            /// </summary>
            /// <param name="logCallback">Callback</param>
            /// <param name="enableMainThread">Run callback on (Main Thread)?</param>
            void HandleLog(Action<string> logCallback, bool enableMainThread = false);

            /// <summary>
            /// Handle (warning log) callback 
            /// </summary>
            /// <param name="warningCallback">Callback</param>
            /// <param name="enableMainThread">Run callback on (Main Thread)?</param>
            void HandleWarning(Action<string> warningCallback, bool enableMainThread = false);

            /// <summary>
            /// Handle (exception log) callback
            /// </summary>
            /// <param name="errorCallback">Callback</param>
            /// <param name="enableMainThread">Run callback on (Main Thread)?</param>
            void HandleError(Action<Exception> errorCallback, bool enableMainThread = false);
        }
    }
}