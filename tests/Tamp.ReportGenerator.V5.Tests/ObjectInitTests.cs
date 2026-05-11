using System.IO;
using Xunit;

namespace Tamp.ReportGenerator.V5.Tests;

public sealed class ObjectInitTests
{
    private static Tool FakeTool() =>
        new(AbsolutePath.Create(Path.Combine(Path.GetTempPath(), "reportgenerator")));

    // ---- Object-init overloads (TAM-161, 0.2.0+) ----

    [Fact]
    public void Run_ObjectInit_Emits_Identical_Plan_To_Fluent()
    {
        var tool = FakeTool();

        var fluent = ReportGenerator.Run(tool, s => s
            .AddReport("coverage.cobertura.xml")
            .SetTargetDir("artifacts/coverage-report")
            .AddReportType(ReportGeneratorReportType.Html)
            .AddReportType(ReportGeneratorReportType.Cobertura)
            .AddSourceDir("/src/A")
            .SetHistoryDir("/history")
            .AddAssemblyFilter("+MyApp*")
            .AddAssemblyFilter("-Tests*")
            .AddClassFilter("+MyApp.Core.*")
            .AddFileFilter("-**/Generated/*.cs")
            .SetVerbosity(ReportGeneratorVerbosity.Warning)
            .SetTitle("My App Coverage")
            .SetTag("abc1234"));

        var objectInit = ReportGenerator.Run(tool, new ReportGeneratorSettings
        {
            Reports = { "coverage.cobertura.xml" },
            TargetDir = "artifacts/coverage-report",
            ReportTypes = { ReportGeneratorReportType.Html, ReportGeneratorReportType.Cobertura },
            SourceDirs = { "/src/A" },
            HistoryDir = "/history",
            AssemblyFilters = { "+MyApp*", "-Tests*" },
            ClassFilters = { "+MyApp.Core.*" },
            FileFilters = { "-**/Generated/*.cs" },
            Verbosity = ReportGeneratorVerbosity.Warning,
            Title = "My App Coverage",
            Tag = "abc1234",
        });

        Assert.Equal(fluent.Executable, objectInit.Executable);
        Assert.Equal(fluent.Arguments, objectInit.Arguments);
    }

    [Fact]
    public void Run_ObjectInit_Round_Trips_Required_Args_Only()
    {
        var plan = ReportGenerator.Run(FakeTool(), new ReportGeneratorSettings
        {
            Reports = { "a.xml" },
            TargetDir = "/out",
        });
        Assert.Equal(["-reports:a.xml", "-targetdir:/out"], plan.Arguments);
    }

    [Fact]
    public void Run_ObjectInit_Throws_On_Null_Tool()
        => Assert.Throws<ArgumentNullException>(() =>
            ReportGenerator.Run(null!, new ReportGeneratorSettings { Reports = { "a.xml" }, TargetDir = "/out" }));

    [Fact]
    public void Run_ObjectInit_Throws_On_Null_Settings()
        => Assert.Throws<ArgumentNullException>(() => ReportGenerator.Run(FakeTool(), (ReportGeneratorSettings)null!));

    [Fact]
    public void Run_ObjectInit_Throws_When_No_Reports_Specified()
        => Assert.Throws<InvalidOperationException>(() =>
            ReportGenerator.Run(FakeTool(), new ReportGeneratorSettings { TargetDir = "/out" }));

    [Fact]
    public void Run_ObjectInit_Throws_When_TargetDir_Not_Set()
        => Assert.Throws<InvalidOperationException>(() =>
            ReportGenerator.Run(FakeTool(), new ReportGeneratorSettings { Reports = { "a.xml" } }));

    [Fact]
    public void Run_ObjectInit_License_Emits_Revealed_Value_And_Registers_Secret()
    {
        var lic = new Secret("RGProLicense", "AAAA-BBBB-CCCC-DDDD");
        var plan = ReportGenerator.Run(FakeTool(), new ReportGeneratorSettings
        {
            Reports = { "a.xml" },
            TargetDir = "/out",
            License = lic,
        });
        Assert.Contains("-license:AAAA-BBBB-CCCC-DDDD", plan.Arguments);
        Assert.Same(lic, Assert.Single(plan.Secrets));
    }

    [Fact]
    public void Every_ObjectInit_Overload_Returns_NonNull_CommandPlan()
    {
        // Smoke: surface compiles and returns a non-null plan for the canonical happy-path settings.
        Assert.NotNull(ReportGenerator.Run(FakeTool(), new ReportGeneratorSettings
        {
            Reports = { "a.xml" },
            TargetDir = "/out",
        }));
    }
}
