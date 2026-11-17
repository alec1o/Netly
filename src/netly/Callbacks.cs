namespace Netly
{
    /// <summary>
    ///     Chooses the thread where <c>OnOpen</c>, <c>OnClose</c>, <c>OnError</c>, <c>OnAccept</c> and the other
    ///     lifecycle callbacks run.
    ///     <br />
    ///     <c>OnData</c> and <c>OnEvent</c> always run on the receiving thread, their stream is valid only during the callback.
    /// </summary>
    public static class Callbacks
    {
        /// <summary>
        ///     True (default) runs the callbacks on the network thread, right when the event happens.
        ///     False keeps them in a queue until <see cref="Update" /> is called, e.g. to run them on the main thread of a game.
        /// </summary>
        public static bool IsImmediate
        {
            get => Dispatcher.Singleton.ImmediateMode;
            set => Dispatcher.Singleton.ImmediateMode = value;
        }

        /// <summary>
        ///     Runs the queued callbacks on the calling thread. Does nothing when <see cref="IsImmediate" /> is true and the queue is empty.
        /// </summary>
        public static void Update()
        {
            Dispatcher.Singleton.Dispatch();
        }

        /// <summary>
        ///     Drops the queued callbacks without running them.
        /// </summary>
        public static void Clear()
        {
            Dispatcher.Singleton.Clear();
        }
    }
}
