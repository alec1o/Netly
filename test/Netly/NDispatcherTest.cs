namespace NetlyTest.Netly;

public class NDispatcherTest
{
    [Fact]
    public void Submit_WhenImmediateMode_ExecutesOnCallerThread()
    {
        // Arrange
        var dispatcher = new NDispatcher(immediateMode: true);
        var callerThreadId = Environment.CurrentManagedThreadId;
        var actionThreadId = -1;

        // Act
        dispatcher.Submit(() => actionThreadId = Environment.CurrentManagedThreadId);

        // Assert
        Assert.Equal(callerThreadId, actionThreadId);
    }

    [Fact]
    public async Task Submit_WhenNonImmediateMode_ExecutesOnDispatchThread()
    {
        // Arrange
        var dispatcher = new NDispatcher(immediateMode: false);
        var callerThreadId = Environment.CurrentManagedThreadId;
        var actionThreadId = -1;

        // Act
        dispatcher.Submit(() => actionThreadId = Environment.CurrentManagedThreadId);

        // Dispatch using a separate thread
        await Task.Run(() => dispatcher.Dispatch());

        // Assert
        Assert.NotEqual(callerThreadId, actionThreadId);
    }

    [Fact]
    public void Dispatch_WhenMultipleActions_AllActionsExecuted()
    {
        // Arrange
        var dispatcher = new NDispatcher(immediateMode: false);
        var count = 0;

        dispatcher.Submit(() => Interlocked.Increment(ref count));
        dispatcher.Submit(() => Interlocked.Increment(ref count));
        dispatcher.Submit(() => Interlocked.Increment(ref count));

        // Act
        dispatcher.Dispatch();

        // Assert
        Assert.Equal(3, count);
    }

    [Fact]
    public void Clear_WhenCalled_EmptiesPendingActions()
    {
        // Arrange
        var dispatcher = new NDispatcher(immediateMode: false);
        var ran = false;
        dispatcher.Submit(() => ran = true);

        // Act
        dispatcher.Clear();
        dispatcher.Dispatch();

        // Assert
        Assert.False(ran);
    }

    [Fact]
    public async Task Submit_ImmediateThenNonImmediateMode_ProcessesQueuedActions()
    {
        // Arrange
        var dispatcher = new NDispatcher(immediateMode: true);
        var callerThreadId = Environment.CurrentManagedThreadId;
        var immediateActionThreadId = -1;
        var queuedActionThreadId = -1;

        // Act
        dispatcher.Submit(() => immediateActionThreadId = Environment.CurrentManagedThreadId);

        dispatcher.ImmediateMode = false;
        dispatcher.Submit(() => queuedActionThreadId = Environment.CurrentManagedThreadId);

        await Task.Run(() => dispatcher.Dispatch());

        // Assert
        Assert.Equal(callerThreadId, immediateActionThreadId);  // Immediate action ran on caller thread
        Assert.NotEqual(callerThreadId, queuedActionThreadId); // Queued action ran on dispatch thread
    }
}
