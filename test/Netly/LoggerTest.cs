namespace NetlyTest.Netly;

using System;
using System.Threading.Tasks;
using Xunit;

public class LoggerTest
{
    [Fact]
    public void Submit_String_ImmediateMode_ExecutesOnCallerThread()
    {
        var logger = new Logger();
        var callerThreadId = Environment.CurrentManagedThreadId;
        int? actionThreadId = null;
        string? capturedMessage = null;

        logger.OnLog(msg =>
        {
            capturedMessage = msg;
            actionThreadId = Environment.CurrentManagedThreadId;
        }, forceImmediateDispatcherMode: true);

        logger.Submit("TestMessage");

        Assert.Equal(callerThreadId, actionThreadId);
        Assert.NotNull(capturedMessage);
        Assert.Contains("TestMessage", capturedMessage);
    }

    [Fact]
    public void Submit_String_NonImmediateMode_DispatchesViaDispatcher()
    {
        var dispatcher = new Dispatcher(immediateMode: false);
        var logger = new Logger { Dispatcher = dispatcher };
        int? actionThreadId = null;
        string? capturedMessage = null;
        var callerThreadId = Environment.CurrentManagedThreadId;

        logger.OnLog(msg =>
        {
            capturedMessage = msg;
            actionThreadId = Environment.CurrentManagedThreadId;
        });

        logger.Submit("QueuedMessage");

        var thread = new Thread(() => dispatcher.Dispatch());
        thread.Start();
        thread.Join();

        Assert.NotEqual(callerThreadId, actionThreadId);
        Assert.NotNull(capturedMessage);
        Assert.Contains("QueuedMessage", capturedMessage);
    }

    [Fact]
    public void Submit_Exception_ImmediateMode_ExecutesOnCallerThread()
    {
        var logger = new Logger();
        var callerThreadId = Environment.CurrentManagedThreadId;
        int? actionThreadId = null;
        string? capturedMessage = null;

        logger.OnLog(msg =>
        {
            capturedMessage = msg;
            actionThreadId = Environment.CurrentManagedThreadId;
        }, forceImmediateDispatcherMode: true);

        var ex = new InvalidOperationException("Oops");
        logger.Submit(ex);

        Assert.Equal(callerThreadId, actionThreadId);
        Assert.NotNull(capturedMessage);
        Assert.Contains("Oops", capturedMessage);
    }

    [Fact]
    public void MultipleHandlers_AllReceiveLogs()
    {
        var logger = new Logger();
        int handler1Count = 0;
        int handler2Count = 0;

        logger.OnLog(_ => handler1Count++);
        logger.OnLog(_ => handler2Count++);

        logger.Submit("Test");

        Assert.Equal(1, handler1Count);
        Assert.Equal(1, handler2Count);
    }

    [Fact]
    public void Submit_NullValue_DoesNotInvokeHandler()
    {
        var logger = new Logger();
        bool invoked = false;

        logger.OnLog(_ => invoked = true);

        logger.Submit<string>(null!);

        Assert.False(invoked);
    }
}
