using System.Collections.Concurrent;
using System.Threading;

namespace Netly
{
    public static partial class RUDP
    {
        // one thread updates every connection (retransmission, delayed ack, keep alive, timeouts)
        internal static class Scheduler
        {
            private const int Interval = 2;
            private const int IdleInterval = 20;

            private static readonly ConcurrentDictionary<Connection, byte> Connections =
                new ConcurrentDictionary<Connection, byte>();

            private static readonly object Locker = new object();
            private static Thread _thread;

            public static void Add(Connection connection)
            {
                Connections[connection] = 0;

                lock (Locker)
                {
                    if (_thread != null) return;

                    _thread = new Thread(Update) { IsBackground = true, Name = "Netly.RUDP.Scheduler" };
                    _thread.Start();
                }
            }

            public static void Remove(Connection connection)
            {
                Connections.TryRemove(connection, out _);
            }

            private static void Update()
            {
                for (;;)
                {
                    if (Connections.IsEmpty)
                    {
                        Thread.Sleep(IdleInterval);
                        continue;
                    }

                    var now = Wire.Now();

                    foreach (var pair in Connections)
                        pair.Key.Tick(now);

                    Thread.Sleep(Interval);
                }
            }
        }
    }
}
