using System.Collections.Concurrent;
using System.Threading;

namespace Netly
{
    // one thread updates every connection (retransmission, delayed ack, keep alive, timeouts)
    internal static class RudpScheduler
    {
        private const int Interval = 2;
        private const int IdleInterval = 20;

        private static readonly ConcurrentDictionary<RudpConnection, byte> Connections =
            new ConcurrentDictionary<RudpConnection, byte>();

        private static readonly object Locker = new object();
        private static Thread _thread;
        private static long _ticks;

        // how many connections were updated, to measure the cost of idle connections
        public static long Ticks => Interlocked.Read(ref _ticks);

        public static void Add(RudpConnection connection)
        {
            Connections[connection] = 0;

            lock (Locker)
            {
                if (_thread != null) return;

                _thread = new Thread(Update) { IsBackground = true, Name = "Netly.Rudp.Scheduler" };
                _thread.Start();
            }
        }

        public static void Remove(RudpConnection connection)
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

                var now = RudpWire.Now();

                foreach (var pair in Connections)
                {
                    // an idle connection waits for its deadline, a connection with work asks for a tick every pass
                    if (now < pair.Key.NextTickAt) continue;

                    Interlocked.Increment(ref _ticks);
                    pair.Key.Tick(now);
                }

                Thread.Sleep(Interval);
            }
        }
    }
}
