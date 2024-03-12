using System;
using Netly.Core;

namespace Netly
{
    public partial class NETLY
    {
        private class _Logger : ILogger
        {
            private struct Handler<T>
            {
                public Handler(ref EventHandler<T> refHandler, ref Action<T> refCallback, bool useMainThread)
                {
                    Action<T> callback = refCallback;

                    refHandler += (_, param) =>
                    {
                        if (useMainThread)
                            MainThread.Add(() => callback?.Invoke(param));
                        else
                            callback?.Invoke(param);
                    };
                }
            }

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
                _ = new Handler<string>(ref _regularLogEvent, ref logCallback, useMainThread);
            }

            public void HandleWarning(Action<string> warningCallback, bool useMainThread)
            {
                _ = new Handler<string>(ref _warningLogEvent, ref warningCallback, useMainThread);
            }

            public void HandleError(Action<Exception> errorCallback, bool useMainThread)
            {
                _ = new Handler<Exception>(ref _errorLogEvent, ref errorCallback, useMainThread);
            }

            #endregion
        }
    }
}