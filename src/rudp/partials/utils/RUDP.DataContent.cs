using System;

namespace Netly
{
    public static partial class RUDP
    {
        private class DataContent
        {
            public readonly byte[] Data;
            public readonly uint Id;

            public DataContent(uint id, byte[] data)
            {
                Id = id;
                Data = data;
                UpdateTimeout();
            }

            public DateTime TimeoutAt { get; private set; }

            public void UpdateTimeout()
            {
                TimeoutAt = DateTime.UtcNow.AddMilliseconds(Channel.ResendReliablePackageDelay);
            }
        }
    }
}