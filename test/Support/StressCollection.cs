namespace NetlyTest.Support;

// tests that flood the loopback or measure the whole process must not run beside the others
[CollectionDefinition(Name, DisableParallelization = true)]
public class StressCollection
{
    public const string Name = "Stress";
}
