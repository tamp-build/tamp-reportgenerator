using Xunit;

namespace Tamp.ReportGenerator.V5.Tests;

public sealed class SmokeTests
{
    [Fact]
    public void Assembly_Loads_And_ReportGenerator_Is_Reachable()
    {
        Assert.NotNull(typeof(ReportGenerator));
    }
}
