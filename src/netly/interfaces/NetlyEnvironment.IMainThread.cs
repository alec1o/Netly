using System;

namespace Netly
{
    public static partial class NetlyEnvironment
    {
        /// <summary>
        ///     Netly. Internal actions logger.
        /// </summary>
        public interface IMainThread
        {
            /// <summary>
            ///     Execute action automatically or on custom MainThread.
            ///     <br />True: The actions will automatic executed with same thread that add action.
            ///     <br />False: The actions will be executed with custom dispatch thread.
            /// </summary>
            bool IsAutomatic { get; set; }

            /// <summary>
            ///     Add action to be execution queue in main thread.
            /// </summary>
            /// <param name="action"></param>
            void Add(Action action);

            /// <summary>
            ///     Dispatch actions in queue.
            ///     <br /> Note: Only works if (IsAutomatic is false), otherwise the actions will be dispatched automatically with same
            ///     thread that add that in queue.
            /// </summary>
            void Dispatch();
        }
    }
}