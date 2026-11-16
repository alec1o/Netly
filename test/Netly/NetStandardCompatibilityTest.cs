using System.Reflection;
using System.Runtime.Versioning;

namespace NetlyTest.Netly;

public class NetStandardCompatibilityTest
{
    [Fact]
    public void LibraryTargetsNetStandard20()
    {
        var attribute = typeof(NHost).Assembly.GetCustomAttribute<TargetFrameworkAttribute>();

        Assert.NotNull(attribute);
        Assert.Equal(".NETStandard,Version=v2.0", attribute.FrameworkName);
    }

    [Fact]
    public void LibraryDoesNotReferenceNewerFrameworkAssemblies()
    {
        // netstandard2.0 libraries must only reference netstandard (and System.Memory/Byter packages).
        var references = typeof(NHost).Assembly.GetReferencedAssemblies().Select(x => x.Name).ToList();

        Assert.Contains("netstandard", references);
        Assert.DoesNotContain("System.Runtime.Experimental", references);
    }
}
