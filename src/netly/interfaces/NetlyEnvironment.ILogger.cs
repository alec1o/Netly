using System;

namespace Netly
{
    public static partial class NetlyEnvironment
    {
        /// <summary>
        /// Netly. Internal actions logger.
        /// </summary>
        public interface ILogger
        {

            /// <summary>
            /// Create a log
            /// </summary>
            /// <param name="message">Log message</param>
            void Create(string message);

            /// <summary>
            /// Create a error log
            /// </summary>
            /// <param name="exception">Exception object</param>
            void Create(Exception exception);

            /// <summary>
            /// Handle (regular log) callback
            /// </summary>
            /// <param name="logCallback">Callback</param>
            /// <param name="useMainThread">Run callback on (Main Thread)?</param>
            void On(Action<string> logCallback, bool useMainThread = false);
            

            /// <summary>
            /// Handle (exception log) callback
            /// </summary>
            /// <param name="callback">Callback</param>
            /// <param name="useMainThread">Run callback on (Main Thread)?</param>
            void On(Action<Exception> callback, bool useMainThread = false);
        }
    }
}