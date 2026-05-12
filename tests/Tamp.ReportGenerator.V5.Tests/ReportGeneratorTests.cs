using Xunit;

namespace Tamp.ReportGenerator.V5.Tests;

public sealed class ReportGeneratorTests
{
    private static Tool FakeTool() => new(AbsolutePath.Create("/fake/reportgenerator"));

    private static string FlagValue(IReadOnlyList<string> args, string flag)
    {
        var prefix = $"-{flag}:";
        var match = args.FirstOrDefault(a => a.StartsWith(prefix, StringComparison.Ordinal));
        return match is null
            ? throw new InvalidOperationException($"Flag '{flag}' not found in: {string.Join(' ', args)}")
            : match.Substring(prefix.Length);
    }

    [Fact]
    public void Run_Throws_On_Null_Tool()
        => Assert.Throws<ArgumentNullException>(() => ReportGenerator.Run(null!, _ => { }));

    [Fact]
    public void Run_Throws_On_Null_Configurer()
        => Assert.Throws<ArgumentNullException>(() => ReportGenerator.Run(FakeTool(), (Action<ReportGeneratorSettings>)null!));

    [Fact]
    public void Run_Throws_When_No_Reports_Specified()
        => Assert.Throws<InvalidOperationException>(() =>
            ReportGenerator.Run(FakeTool(), s => s.SetTargetDir("/out")));

    [Fact]
    public void Run_Throws_When_TargetDir_Not_Set()
        => Assert.Throws<InvalidOperationException>(() =>
            ReportGenerator.Run(FakeTool(), s => s.AddReport("a.xml")));

    [Fact]
    public void Run_Executable_Is_The_Tool_Path()
    {
        var plan = ReportGenerator.Run(FakeTool(), s => s.AddReport("a.xml").SetTargetDir("/out"));
        Assert.Equal("/fake/reportgenerator", plan.Executable);
    }

    [Fact]
    public void Reports_Single_Maps_To_Dash_Reports_Colon_Path()
    {
        var args = ReportGenerator.Run(FakeTool(), s => s.AddReport("a.xml").SetTargetDir("/out")).Arguments;
        Assert.Equal("a.xml", FlagValue(args, "reports"));
    }

    [Fact]
    public void Reports_Multiple_Are_Semicolon_Joined_In_One_Arg()
    {
        // ReportGenerator's flag grammar is `-reports:a;b;c` as a single
        // CLI argument. The wrapper joins on semicolons inside one arg
        // rather than emitting multiple `-reports:` flags.
        var args = ReportGenerator.Run(FakeTool(), s => s
            .AddReport("a.xml")
            .AddReport("b.xml")
            .AddReport("c.xml")
            .SetTargetDir("/out")).Arguments;
        Assert.Equal("a.xml;b.xml;c.xml", FlagValue(args, "reports"));
        // Exactly one occurrence of the flag.
        Assert.Single(args, a => a.StartsWith("-reports:"));
    }

    [Fact]
    public void Reports_AddReports_From_AbsolutePath_Sequence_Round_Trips()
    {
        // AbsolutePath normalizes per-OS (drive letter on Windows), so compare against
        // post-normalization values rather than the hardcoded POSIX shape.
        var a = AbsolutePath.Create("/abs/a.xml");
        var b = AbsolutePath.Create("/abs/b.xml");
        var args = ReportGenerator.Run(FakeTool(), s => s.AddReports(new[] { a, b }).SetTargetDir("/out")).Arguments;
        Assert.Equal($"{a.Value};{b.Value}", FlagValue(args, "reports"));
    }

    [Fact]
    public void TargetDir_Maps_To_Dash_TargetDir_Colon_Path()
    {
        var args = ReportGenerator.Run(FakeTool(), s => s.AddReport("a.xml").SetTargetDir("/out/html")).Arguments;
        Assert.Equal("/out/html", FlagValue(args, "targetdir"));
    }

    [Fact]
    public void ReportTypes_Multiple_Are_Semicolon_Joined()
    {
        var args = ReportGenerator.Run(FakeTool(), s => s
            .AddReport("a.xml").SetTargetDir("/out")
            .AddReportType(ReportGeneratorReportType.Html)
            .AddReportType(ReportGeneratorReportType.Cobertura)
            .AddReportType(ReportGeneratorReportType.Badges)).Arguments;
        Assert.Equal("Html;Cobertura;Badges", FlagValue(args, "reporttypes"));
    }

    [Fact]
    public void Common_Report_Type_Constants_Match_CLI_Names()
    {
        // Canonical CLI casing — these must NOT be lowercased.
        Assert.Equal("Html", ReportGeneratorReportType.Html);
        Assert.Equal("Cobertura", ReportGeneratorReportType.Cobertura);
        Assert.Equal("MarkdownSummaryGithub", ReportGeneratorReportType.MarkdownSummaryGithub);
        // Note: the lcov type IS lowercase in the CLI surface.
        Assert.Equal("lcov", ReportGeneratorReportType.Lcov);
    }

    [Fact]
    public void SourceDirs_Plural_Are_Semicolon_Joined()
    {
        var args = ReportGenerator.Run(FakeTool(), s => s
            .AddReport("a.xml").SetTargetDir("/out")
            .AddSourceDir("/src/A").AddSourceDir("/src/B")).Arguments;
        Assert.Equal("/src/A;/src/B", FlagValue(args, "sourcedirs"));
    }

    [Fact]
    public void HistoryDir_Round_Trips()
    {
        var args = ReportGenerator.Run(FakeTool(), s => s
            .AddReport("a.xml").SetTargetDir("/out")
            .SetHistoryDir("/history")).Arguments;
        Assert.Equal("/history", FlagValue(args, "historydir"));
    }

    [Fact]
    public void AssemblyFilters_Preserve_Plus_Minus_Grammar_Verbatim()
    {
        // ReportGenerator's filter language is "+include;-exclude" — the
        // wrapper passes entries through unchanged.
        var args = ReportGenerator.Run(FakeTool(), s => s
            .AddReport("a.xml").SetTargetDir("/out")
            .AddAssemblyFilter("+MyApp*")
            .AddAssemblyFilter("-Tests*")
            .AddAssemblyFilter("-*.Generated")).Arguments;
        Assert.Equal("+MyApp*;-Tests*;-*.Generated", FlagValue(args, "assemblyfilters"));
    }

    [Fact]
    public void ClassFilters_FileFilters_Round_Trip()
    {
        var args = ReportGenerator.Run(FakeTool(), s => s
            .AddReport("a.xml").SetTargetDir("/out")
            .AddClassFilter("+MyApp.Core.*")
            .AddFileFilter("-**/Generated/*.cs")).Arguments;
        Assert.Equal("+MyApp.Core.*", FlagValue(args, "classfilters"));
        Assert.Equal("-**/Generated/*.cs", FlagValue(args, "filefilters"));
    }

    [Fact]
    public void RiskHotspot_Filters_Use_Correct_Flag_Names()
    {
        var args = ReportGenerator.Run(FakeTool(), s => s
            .AddReport("a.xml").SetTargetDir("/out")
            .AddRiskHotspotAssemblyFilter("+MyApp*")
            .AddRiskHotspotClassFilter("-MyApp.Internal.*")).Arguments;
        Assert.Equal("+MyApp*", FlagValue(args, "riskhotspotassemblyfilters"));
        Assert.Equal("-MyApp.Internal.*", FlagValue(args, "riskhotspotclassfilters"));
    }

    [Theory]
    [InlineData(ReportGeneratorVerbosity.Verbose, "Verbose")]
    [InlineData(ReportGeneratorVerbosity.Info, "Info")]
    [InlineData(ReportGeneratorVerbosity.Warning, "Warning")]
    [InlineData(ReportGeneratorVerbosity.Error, "Error")]
    [InlineData(ReportGeneratorVerbosity.Off, "Off")]
    public void Verbosity_Maps_To_Title_Case_Token(ReportGeneratorVerbosity v, string expected)
    {
        var args = ReportGenerator.Run(FakeTool(), s => s
            .AddReport("a.xml").SetTargetDir("/out")
            .SetVerbosity(v)).Arguments;
        Assert.Equal(expected, FlagValue(args, "verbosity"));
    }

    [Fact]
    public void Title_And_Tag_Round_Trip()
    {
        var args = ReportGenerator.Run(FakeTool(), s => s
            .AddReport("a.xml").SetTargetDir("/out")
            .SetTitle("My App Coverage")
            .SetTag("abc1234")).Arguments;
        Assert.Equal("My App Coverage", FlagValue(args, "title"));
        Assert.Equal("abc1234", FlagValue(args, "tag"));
    }

    [Fact]
    public void License_Emits_Revealed_Value_And_Registers_Secret()
    {
        var lic = new Secret("RGProLicense", "AAAA-BBBB-CCCC-DDDD");
        var plan = ReportGenerator.Run(FakeTool(), s => s
            .AddReport("a.xml").SetTargetDir("/out")
            .SetLicense(lic));
        Assert.Equal("AAAA-BBBB-CCCC-DDDD", FlagValue(plan.Arguments, "license"));
        Assert.Same(lic, Assert.Single(plan.Secrets));
    }

    [Fact]
    public void No_License_Means_Empty_Secrets()
    {
        var plan = ReportGenerator.Run(FakeTool(), s => s.AddReport("a.xml").SetTargetDir("/out"));
        Assert.Empty(plan.Secrets);
    }

    [Fact]
    public void Plugins_Multiple_Are_Semicolon_Joined()
    {
        var args = ReportGenerator.Run(FakeTool(), s => s
            .AddReport("a.xml").SetTargetDir("/out")
            .AddPlugin("/plugins/A.dll")
            .AddPlugin("/plugins/B.dll")).Arguments;
        Assert.Equal("/plugins/A.dll;/plugins/B.dll", FlagValue(args, "plugins"));
    }

    [Fact]
    public void Bare_Required_Args_Produce_Just_Two_Flags()
    {
        var args = ReportGenerator.Run(FakeTool(), s => s.AddReport("a.xml").SetTargetDir("/out")).Arguments;
        Assert.Equal(["-reports:a.xml", "-targetdir:/out"], args);
    }

    [Fact]
    public void EnvironmentVariables_Pass_Through_To_Plan()
    {
        var plan = ReportGenerator.Run(FakeTool(), s => s
            .AddReport("a.xml").SetTargetDir("/out")
            .SetEnvironmentVariable("RG_LICENSE", "x"));
        Assert.Equal("x", plan.Environment["RG_LICENSE"]);
    }

    [Fact]
    public void WorkingDirectory_From_Settings_Wins_Over_Tool()
    {
        var tool = new Tool(AbsolutePath.Create("/fake/reportgenerator"), workingDirectory: "/from-tool");
        var plan = ReportGenerator.Run(tool, s => s
            .AddReport("a.xml").SetTargetDir("/out")
            .SetWorkingDirectory("/from-settings"));
        Assert.Equal("/from-settings", plan.WorkingDirectory);
    }
}
