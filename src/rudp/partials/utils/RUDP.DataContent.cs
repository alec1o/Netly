using System;

namespace Netly
{
    public static partial class RUDP
    {
        private class DataContent
        {
            public readonly uint Id;
            public readonly byte[] Data;
            public readonly Host Host;
            public DateTime TimeoutAt { get; private set; }

            public DataContent(uint id, byte[] data, Host host)
            {
                Id = id;
                Data = data;
                Host = host;
                UpdateTimeout();
            }

            public void UpdateTimeout()
            {
                TimeoutAt = DateTime.UtcNow.AddMilliseconds(Channel.ResentTimeout);
            }
        }
    }
}