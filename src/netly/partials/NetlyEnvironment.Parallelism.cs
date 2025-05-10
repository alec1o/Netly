using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Netly
{
    public partial class NetlyEnvironment
    {
        internal class Parallelism : TaskScheduler
        {
            private readonly List<Thread> MyThreads = new List<Thread>();
            private readonly BlockingCollection<Task> Tasks = new BlockingCollection<Task>();
            private CancellationTokenSource CancelToken = new CancellationTokenSource();
            private bool IsStarted { get; set; }
            private TaskScheduler MyScheduler => this;


            public void Start(int threads)
            {
                if (IsStarted) return;

                CancelToken = new CancellationTokenSource();

                if (threads <= 1) threads = 1;
                for (var i = 0; i < threads; i++)
                    MyThreads.Add(new Thread(() => Update(CancelToken.Token))
                    {
                        IsBackground = true,
                        Name = $"{GetType().Namespace}.{GetType().Name} #{Id} ({i})",
                        Priority = ThreadPriority.Normal
                    });

                MyThreads.ForEach(x => x.Start());

                IsStarted = true;
            }

            public void Stop()
            {
                if (!IsStarted) return;

                CancelToken?.Cancel();
                Tasks.CompleteAdding();
                MyThreads.Clear();
                IsStarted = false;
            }

            public Task Register(Func<Task> function)
            {
                var task = new Task(async _ => await function(), null, CancelToken.Token);
                task.Start(MyScheduler);
                return task;
            }

            private void Update(CancellationToken token)
            {
                try
                {
                    foreach (var task in Tasks.GetConsumingEnumerable(token)) TryExecuteTask(task);
                }
                catch (Exception e)
                {
                    Logger.Create(e);
                }
            }

            protected override IEnumerable<Task> GetScheduledTasks()
            {
                return Tasks.ToArray();
            }

            protected override void QueueTask(Task task)
            {
                Tasks.Add(task);
            }

            protected override bool TryExecuteTaskInline(Task task, bool taskWasPreviouslyQueued)
            {
                return false;
            }
        }
    }
}