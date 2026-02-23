using System;
using System.Collections.Concurrent;

namespace Netly
{
    /// <summary>
    ///     NDispatcher is a lightweight action dispatcher that allows actions to be executed either immediately
    ///     on the calling thread or enqueued for later execution by a dedicated dispatching thread.
    ///     <para>
    ///         It supports both singleton usage via <see cref="Singleton" /> or independent instances with custom
    ///         ImmediateMode settings. The dispatcher is thread-safe when queuing actions and can safely handle
    ///         multiple threads submitting actions concurrently.
    ///     </para>
    /// </summary>
    public class NDispatcher
    {
        /// <summary>
        ///     Singleton instance of <see cref="NDispatcher" /> with <see cref="ImmediateMode" /> default set to true
        ///     Use this instance for global execution scenarios.
        /// </summary>
        public static readonly NDispatcher Singleton = new NDispatcher(true);

        private readonly ConcurrentQueue<Action> _actions = new ConcurrentQueue<Action>();

        /// <summary>
        ///     Indicates whether this <see cref="NDispatcher" /> instance is the <see cref="Singleton" /> instance.
        /// </summary>
        public readonly bool IsSingleton;

        /// <summary>
        ///     Initializes a new NDispatcher instance.
        /// </summary>
        /// <param name="immediateMode">
        ///     If true, submitted actions execute immediately on the caller thread.
        ///     If false, actions are enqueued for later execution via <see cref="Dispatch" />.
        /// </param>
        public NDispatcher(bool immediateMode = false)
        {
            ImmediateMode = immediateMode;
            IsSingleton = false;
        }

        private NDispatcher()
        {
            ImmediateMode = true;
            IsSingleton = true;
        }

        /// <summary>
        ///     Gets or sets the mode of action execution.
        ///     <para>
        ///         When true, actions submitted via <see cref="Submit" /> run immediately on the caller thread.
        ///         When false, actions are queued internally and executed later using <see cref="Dispatch" />.
        ///     </para>
        ///     <para>
        ///         Changing this property at runtime affects subsequent calls to <see cref="Submit" /> only.
        ///         Actions already queued will remain in the queue until dispatched.
        ///     </para>
        /// </summary>
        public bool ImmediateMode { get; set; }

        /// <summary>
        ///     Submits an action to the dispatcher.
        ///     <para>
        ///         If <see cref="ImmediateMode" /> is true, the action executes immediately on the caller thread.
        ///         Otherwise, it is enqueued for later execution via <see cref="Dispatch" />.
        ///     </para>
        ///     <para>
        ///         Thread Safety: This method is safe to call from multiple threads concurrently when actions are enqueued.
        ///     </para>
        /// </summary>
        /// <param name="action">The action to execute or enqueue. Null values are ignored.</param>
        public void Submit(Action action)
        {
            if (action == null) return;

            if (ImmediateMode)
                action();
            else
                _actions.Enqueue(action);
        }

        /// <summary>
        ///     Executes all actions currently queued in the dispatcher.
        ///     <para>
        ///         Actions are executed in the order they were submitted (FIFO).
        ///     </para>
        ///     <para>
        ///         Thread Safety: Safe to call from a single dispatching thread. Avoid calling concurrently from multiple threads.
        ///     </para>
        /// </summary>
        public void Dispatch()
        {
            while (_actions.TryDequeue(out var action)) action();
        }

        /// <summary>
        ///     Clears all actions currently queued without executing them.
        ///     <para>
        ///         Thread Safety: Safe to call from multiple threads concurrently.
        ///         Use this method when you want to discard pending actions.
        ///     </para>
        /// </summary>
        public void Clear()
        {
            while (_actions.TryDequeue(out _))
            {
            }
        }
    }
}