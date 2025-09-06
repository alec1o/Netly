using System;

namespace Netly
{
    public interface IFraming
    {
        bool IsActive { get; set; }
        long Prefix { get; set; }
        long MaxSize { get; set; }
        long MinSize { get; set; }
        Blob Stream { get; }
        void Clear();
        void Write(Blob blob);
        void OnRead(Action<Blob> callback);
        void OnFail(Action<Exception> callback);
    }
}