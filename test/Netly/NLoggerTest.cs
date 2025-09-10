namespace NetlyTest.Netly;

using System;
using System.Threading.Tasks;
using Xunit;

public class NLoggerTest
{
    [Fact]
    public void Submit_String_ImmediateMode_ExecutesOnCallerThread()
    {
        var logger = new NLogger();
        var callerThreadId = Environment.CurrentManagedThreadId;
        int? actionThreadId = null;
        string? capturedMessage = null;

        logger.OnSubmit(msg =>
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
    public async Task Submit_String_NonImmediateMode_DispatchesViaDispatcher()
    {
        var dispatcher = new NDispatcher(immediateMode: false);
        var logger = new NLogger { Dispatcher = dispatcher };
        int? actionThreadId = null;
        string? capturedMessage = null;
        var callerThreadId = Environment.CurrentManagedThreadId;

        logger.OnSubmit(msg =>
        {
            capturedMessage = msg;
            actionThreadId = Environment.CurrentManagedThreadId;
        });

        logger.Submit("QueuedMessage");

        await Task.Run(() => dispatcher.Dispatch());

        Assert.NotEqual(callerThreadId, actionThreadId);
        Assert.NotNull(capturedMessage);
        Assert.Contains("QueuedMessage", capturedMessage);
    }

    [Fact]
    public void Submit_Exception_ImmediateMode_ExecutesOnCallerThread()
    {
        var logger = new NLogger();
        var callerThreadId = Environment.CurrentManagedThreadId;
        int? actionThreadId = null;
        string? capturedMessage = null;

        logger.OnSubmit(msg =>
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
        var logger = new NLogger();
        int handler1Count = 0;
        int handler2Count = 0;

        logger.OnSubmit(_ => handler1Count++);
        logger.OnSubmit(_ => handler2Count++);

        logger.Submit("Test");

        Assert.Equal(1, handler1Count);
        Assert.Equal(1, handler2Count);
    }

    [Fact]
    public void Submit_NullValue_DoesNotInvokeHandler()
    {
        var logger = new NLogger();
        bool invoked = false;

        logger.OnSubmit(_ => invoked = true);

        logger.Submit<string>(null!);

        Assert.False(invoked);
    }
}
