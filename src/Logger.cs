using System;

namespace Netly
{
    /// <summary>
    ///     <see cref="Logger" /> provides a simple logging system that supports generic messages and exceptions.
    ///     Logs can be dispatched via <see cref="OnLog" />, and can be executed immediately or enqueued
    ///     via a <see cref="Dispatcher" /> for later processing.
    ///     Handlers registered with <see cref="OnLog" /> can listen to multiple log events.
    /// </summary>
    public class Logger
    {
        /// <summary>
        ///     Singleton instance of the logger for global usage.
        /// </summary>
        public static readonly Logger Singleton = new Logger(true);

        /// <summary>
        ///     Indicates whether this instance is the singleton.
        /// </summary>
        public readonly bool IsSingleton;

        private EventHandler<string> _handler;

        private Logger(bool isSingleton)
        {
            IsSingleton = isSingleton;
        }

        /// <summary>
        ///     Default public constructor for non-singleton instances.
        /// </summary>
        public Logger() : this(false)
        {
        }

        /// <summary>
        ///     Optional dispatcher for deferred log execution.
        ///     If null, <see cref="Dispatcher.Singleton" /> will be used.
        /// </summary>
        internal Dispatcher Dispatcher { get; set; }

        private Dispatcher CurrentDispatcher => Dispatcher ?? Dispatcher.Singleton;

        /// <summary>
        ///     Submits a log message or exception to any type.
        ///     If the value is null, the log is ignored.
        ///     Exceptions occurring during logging are automatically captured and submitted.
        /// </summary>
        /// <typeparam name="T">The type of the log message or exception.</typeparam>
        /// <param name="value">The value to log.</param>
        public void Submit<T>(T value)
        {
            try
            {
                if (ReferenceEquals(default(T), value)) return;

                _handler?.Invoke(this, Format(typeof(T), value.ToString()));
            }
            catch (Exception exception)
            {
                _handler?.Invoke(this, Format(exception.GetType(), exception.ToString()));
            }
        }

        /// <summary>
        ///     Registers a callback to be invoked whenever a log message is submitted.
        ///     Multiple callbacks can be registered via repeated calls.
        /// </summary>
        /// <param name="callback">The action to call with each log message.</param>
        /// <param name="forceImmediateDispatcherMode">
        ///     If true, the callback is executed immediately on the submitting thread.<br />
        ///     If false, the callback is enqueued using the available <see cref="Dispatcher" /> instance.
        /// </param>
        public void OnLog(Action<string> callback, bool forceImmediateDispatcherMode = false)
        {
            if (callback == null) return;

            _handler += (_, message) =>
            {
                if (forceImmediateDispatcherMode)
                    callback(message);
                else
                    CurrentDispatcher.Submit(() => callback(message));
            };
        }

        private static string Format(Type type, string message)
        {
            return $"[{nameof(Dispatcher)} > {type.Name}] {message}";
        }
    }
}