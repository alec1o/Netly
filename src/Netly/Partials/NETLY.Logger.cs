using System;
using Netly.Core;

namespace Netly
{
    public partial class NETLY
    {
        private class _Logger : ILogger
        {
            private EventHandler<string> _regularLogEvent;
            private EventHandler<string> _warningLogEvent;
            private EventHandler<Exception> _errorLogEvent;

            #region Push

            public void PushLog(string log) => _regularLogEvent?.Invoke(null, log);

            public void PushWarning(string warning) => _warningLogEvent?.Invoke(null, warning);

            public void PushError(Exception exception) => _errorLogEvent?.Invoke(null, exception);

            #endregion

            #region Handle

            public void HandleLog(Action<string> logCallback, bool useMainThread)
            {
                _regularLogEvent += (@object, @event) =>
                {
                    if (useMainThread)
                        MainThread.Add(() => logCallback?.Invoke(@event));
                    else
                        logCallback?.Invoke(@event);
                };
            }

            public void HandleWarning(Action<string> warningCallback, bool enableMainThread)
            {
                _warningLogEvent += (@object, @event) =>
                {
                    if (enableMainThread)
                        MainThread.Add(() => warningCallback?.Invoke(@event));
                    else
                        warningCallback?.Invoke(@event);
                };
            }

            public void HandleError(Action<Exception> errorCallback, bool useMainThread)
            {
                _errorLogEvent += (@object, @event) =>
                {
                    if (useMainThread)
                        MainThread.Add(() => errorCallback?.Invoke(@event));
                    else
                        errorCallback?.Invoke(@event);
                };
            }

            #endregion
        }
    }
}