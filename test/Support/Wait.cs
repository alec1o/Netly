namespace NetlyTest.Support;

/// <summary>
///     Deterministic waiting helpers, replaces Thread.Sleep.
/// </summary>
internal static class Wait
{
    public static readonly TimeSpan Default = TimeSpan.FromSeconds(10);

    public static async Task Until(Func<bool> condition, TimeSpan? timeout = null, string? what = null)
    {
        var limit = DateTime.UtcNow + (timeout ?? Default);

        while (!condition())
        {
            if (DateTime.UtcNow > limit)
                throw new TimeoutException($"Timeout waiting for: {what ?? "condition"}");

            await Task.Delay(5);
        }
    }

    public static async Task<T> For<T>(TaskCompletionSource<T> source, TimeSpan? timeout = null, string? what = null)
    {
        var finished = await Task.WhenAny(source.Task, Task.Delay(timeout ?? Default));

        if (finished != source.Task)
            throw new TimeoutException($"Timeout waiting for: {what ?? typeof(T).Name}");

        return await source.Task;
    }

    public static TaskCompletionSource<T> Signal<T>() =>
        new(TaskCreationOptions.RunContinuationsAsynchronously);

    /// <summary>
    ///     Asserts that nothing happens during <paramref name="duration" />.
    /// </summary>
    public static async Task Never(Func<bool> happened, TimeSpan duration, string what)
    {
        var limit = DateTime.UtcNow + duration;

        while (DateTime.UtcNow < limit)
        {
            if (happened()) throw new Exception($"Unexpected: {what}");
            await Task.Delay(10);
        }
    }
}
