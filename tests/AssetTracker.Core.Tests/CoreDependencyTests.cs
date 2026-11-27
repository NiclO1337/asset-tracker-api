using System.Reflection;

namespace AssetTracker.Core.Tests;

public sealed class CoreDependencyTests
{
    [Fact]
    public void Core_ReferencesOnlyTheFramework()
    {
        var core = Assembly.Load("AssetTracker.Core");

        var nonFrameworkReferences = core.GetReferencedAssemblies()
            .Select(reference => reference.Name)
            .Where(name => name is null || !name.StartsWith("System", StringComparison.Ordinal))
            .ToList();

        Assert.Empty(nonFrameworkReferences);
    }
}
