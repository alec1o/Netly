namespace NetlyTest.Support;

/// <summary>
///     Turns a blocked test (deadlock inside the library) into a failure, so it can't stop the whole suite.
/// </summary>
internal static class Guard
{
    public static readonly TimeSpan Default = TimeSpan.FromSeconds(60);

    public static async Task Run(Func<Task> body, TimeSpan? timeout = null)
    {
        // body runs on pool thread, a blocking call inside it doesn't block the test thread
        var task = Task.Run(body);
        var limit = timeout ?? Default;

        if (await Task.WhenAny(task, Task.Delay(limit)) != task)
            throw new TimeoutException($"Test blocked for more than {limit.TotalSeconds}s (possible deadlock)");

        await task;
    }
}
