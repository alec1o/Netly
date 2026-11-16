using NetlyTest.Support;

namespace NetlyTest.Netly;

// the setting is global, nothing else may run while these tests change it
[CollectionDefinition(nameof(CallbacksTest), DisableParallelization = true)]
public class CallbacksCollection;

[Collection(nameof(CallbacksTest))]
public class CallbacksTest : IDisposable
{
    public CallbacksTest()
    {
        Callbacks.IsImmediate = true;
        Callbacks.Clear();
    }

    public void Dispose()
    {
        Callbacks.IsImmediate = true;
        Callbacks.Clear();
    }

    [Fact]
    public void DefaultIsImmediate()
    {
        Assert.True(Callbacks.IsImmediate);
    }

    [Fact]
    public async Task QueuedCallbacks_RunOnlyOnUpdate()
    {
        var host = Net.UdpHost();
        var server = new UdpServer();
        var opened = 0;
        var thread = -1;

        server.OnOpen(() =>
        {
            thread = Environment.CurrentManagedThreadId;
            Interlocked.Increment(ref opened);
        });

        Callbacks.IsImmediate = false;
        server.Open(host);
        await Wait.Until(() => server.IsOpened, what: "server open");

        // the socket is open but the callback waits for the update
        await Wait.Never(() => opened > 0, TimeSpan.FromMilliseconds(200), "queued callback ran by itself");

        Callbacks.Update();

        Assert.Equal(1, opened);
        Assert.Equal(Environment.CurrentManagedThreadId, thread);

        Callbacks.IsImmediate = true;
        server.Close();
        await Wait.Until(() => !server.IsOpened, what: "server close");
    }

    [Fact]
    public async Task Clear_DropsQueuedCallbacks()
    {
        var server = new UdpServer();
        var opened = 0;
        server.OnOpen(() => Interlocked.Increment(ref opened));

        Callbacks.IsImmediate = false;
        server.Open(Net.UdpHost());
        await Wait.Until(() => server.IsOpened, what: "server open");

        Callbacks.Clear();
        Callbacks.Update();

        Assert.Equal(0, opened);

        Callbacks.IsImmediate = true;
        server.Close();
    }

    [Fact]
    public void Update_WithEmptyQueue_DoesNothing()
    {
        Callbacks.Update();
        Callbacks.IsImmediate = false;
        Callbacks.Update();
    }

    [Fact]
    public async Task ThrowingCallback_DoesNotBreakUpdate()
    {
        var errors = 0;
        var server = new UdpServer();
        server.OnError(_ => throw new InvalidOperationException("user callback"));
        server.OnError(_ => Interlocked.Increment(ref errors));

        Callbacks.IsImmediate = false;
        server.Open(new Host("1.1.1.1", 0));

        // the failed bind queues the error callbacks, the first one throws and the second one must still run
        await Wait.Until(() =>
        {
            Callbacks.Update();
            return errors == 1;
        }, what: "second error callback");

        Callbacks.IsImmediate = true;
    }
}
