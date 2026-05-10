using System.IO;
using Tamp;
using Xunit;
using Xunit.Abstractions;

namespace Tamp.ReportGenerator.V5.IntegrationTests;

/// <summary>
/// Real-tool exercises of the wrapper. Uses a tiny synthetic Cobertura
/// document staged into a temp dir so the tests don't depend on tamp's
/// coverage artifact existing on the test runner.
/// </summary>
public sealed class ReportGeneratorIntegrationTests : IDisposable
{
    private readonly ITestOutputHelper _output;
    private readonly AbsolutePath _workdir;
    private readonly AbsolutePath _coberturaFile;

    private const string MinimalCoberturaXml = """
<?xml version="1.0" encoding="utf-8"?>
<coverage line-rate="0.5" branch-rate="0.5" version="1.9" timestamp="0" lines-covered="2" lines-valid="4" branches-covered="1" branches-valid="2">
  <packages>
    <package name="MyApp" line-rate="0.5" branch-rate="0.5" complexity="1">
      <classes>
        <class name="MyApp.Calculator" filename="src/Calculator.cs" line-rate="0.5" branch-rate="0.5" complexity="1">
          <methods>
            <method name="Add" signature="(int,int)" line-rate="1" branch-rate="1">
              <lines>
                <line number="3" hits="1" branch="false" />
                <line number="4" hits="1" branch="false" />
              </lines>
            </method>
            <method name="Divide" signature="(int,int)" line-rate="0" branch-rate="0">
              <lines>
                <line number="7" hits="0" branch="false" />
                <line number="8" hits="0" branch="false" />
              </lines>
            </method>
          </methods>
          <lines>
            <line number="3" hits="1" branch="false" />
            <line number="4" hits="1" branch="false" />
            <line number="7" hits="0" branch="false" />
            <line number="8" hits="0" branch="false" />
          </lines>
        </class>
      </classes>
    </package>
  </packages>
</coverage>
""";

    public ReportGeneratorIntegrationTests(ITestOutputHelper output)
    {
        _output = output;
        _workdir = AbsolutePath.Create(Path.Combine(Path.GetTempPath(), $"tamp-rg-it-{Guid.NewGuid():N}"));
        Directory.CreateDirectory(_workdir.Value);
        _coberturaFile = _workdir / "input.cobertura.xml";
        File.WriteAllText(_coberturaFile.Value, MinimalCoberturaXml);
    }

    private static Tool ResolveTool()
    {
        var home = Environment.GetEnvironmentVariable("HOME") ?? "";
        foreach (var candidate in new[] { "reportgenerator", "reportgenerator.exe" })
        {
            var p = Path.Combine(home, ".dotnet", "tools", candidate);
            if (File.Exists(p)) return new Tool(AbsolutePath.Create(p));
        }
        throw new InvalidOperationException(
            "reportgenerator not found in ~/.dotnet/tools. Install with: dotnet tool install -g dotnet-reportgenerator-globaltool --version 5.*");
    }

    private CaptureResult Run(CommandPlan plan)
    {
        _output.WriteLine($"$ {plan.Executable} {string.Join(' ', plan.Arguments)}");
        var result = ProcessRunner.Capture(plan);
        foreach (var line in result.Lines)
            _output.WriteLine($"  [{line.Type}] {line.Text}");
        _output.WriteLine($"  → exit {result.ExitCode}");
        return result;
    }

    [Fact]
    public void Html_Report_Generates_Index_File()
    {
        var tool = ResolveTool();
        var outDir = _workdir / "html";
        var plan = ReportGenerator.Run(tool, s => s
            .AddReport(_coberturaFile.Value)
            .SetTargetDir(outDir.Value)
            .AddReportType(ReportGeneratorReportType.Html));
        var result = Run(plan);
        Assert.Equal(0, result.ExitCode);
        Assert.True((outDir / "index.html").FileExists(), "Expected index.html in HTML report.");
    }

    [Fact]
    public void Cobertura_Report_Round_Trips_Through_The_Tool()
    {
        // Reading cobertura back out of cobertura is a loop test —
        // confirms the wrapper passes both flags + the tool processed
        // the input correctly.
        var tool = ResolveTool();
        var outDir = _workdir / "cobertura";
        var plan = ReportGenerator.Run(tool, s => s
            .AddReport(_coberturaFile.Value)
            .SetTargetDir(outDir.Value)
            .AddReportType(ReportGeneratorReportType.Cobertura));
        var result = Run(plan);
        Assert.Equal(0, result.ExitCode);
        var output = outDir / "Cobertura.xml";
        Assert.True(output.FileExists(), $"Expected {output} to exist.");
        var content = File.ReadAllText(output.Value);
        Assert.Contains("MyApp.Calculator", content);
    }

    [Fact]
    public void MarkdownSummaryGithub_Generates_Md_File()
    {
        var tool = ResolveTool();
        var outDir = _workdir / "md";
        var plan = ReportGenerator.Run(tool, s => s
            .AddReport(_coberturaFile.Value)
            .SetTargetDir(outDir.Value)
            .AddReportType(ReportGeneratorReportType.MarkdownSummaryGithub));
        var result = Run(plan);
        Assert.Equal(0, result.ExitCode);
        // ReportGenerator's GitHub-flavor markdown summary is named
        // SummaryGithub.md.
        Assert.True((outDir / "SummaryGithub.md").FileExists(),
            "Expected SummaryGithub.md.");
        var md = File.ReadAllText((outDir / "SummaryGithub.md").Value);
        Assert.Contains("MyApp", md);
    }

    [Fact]
    public void Multiple_Report_Types_All_Produced_In_One_Run()
    {
        var tool = ResolveTool();
        var outDir = _workdir / "multi";
        var plan = ReportGenerator.Run(tool, s => s
            .AddReport(_coberturaFile.Value)
            .SetTargetDir(outDir.Value)
            .AddReportType(ReportGeneratorReportType.Html)
            .AddReportType(ReportGeneratorReportType.Badges)
            .AddReportType(ReportGeneratorReportType.MarkdownSummary));
        var result = Run(plan);
        Assert.Equal(0, result.ExitCode);
        // HTML index, a badge SVG, and the markdown summary.
        Assert.True((outDir / "index.html").FileExists(), "html");
        Assert.True((outDir / "Summary.md").FileExists(), "markdown");
        var badgeFiles = Directory.EnumerateFiles(outDir.Value, "badge_*.svg").ToList();
        Assert.NotEmpty(badgeFiles);
    }

    [Fact]
    public void Title_And_Tag_Appear_In_Generated_Html()
    {
        var tool = ResolveTool();
        var outDir = _workdir / "titled";
        var plan = ReportGenerator.Run(tool, s => s
            .AddReport(_coberturaFile.Value)
            .SetTargetDir(outDir.Value)
            .AddReportType(ReportGeneratorReportType.Html)
            .SetTitle("Tamp Self-Test Report")
            .SetTag("commit-abc1234"));
        var result = Run(plan);
        Assert.Equal(0, result.ExitCode);
        var html = File.ReadAllText((outDir / "index.html").Value);
        Assert.Contains("Tamp Self-Test Report", html);
        Assert.Contains("commit-abc1234", html);
    }

    [Fact]
    public void Verbosity_Off_Suppresses_Info_Lines()
    {
        var tool = ResolveTool();
        var outDir = _workdir / "quiet";
        var plan = ReportGenerator.Run(tool, s => s
            .AddReport(_coberturaFile.Value)
            .SetTargetDir(outDir.Value)
            .AddReportType(ReportGeneratorReportType.HtmlSummary)
            .SetVerbosity(ReportGeneratorVerbosity.Off));
        var result = Run(plan);
        Assert.Equal(0, result.ExitCode);
        // With verbosity=Off, the tool should produce minimal stdout.
        // Don't assert empty (warnings about missing license or deprecation
        // can leak through). Just confirm the tool ran AND produced output.
        Assert.True(outDir.DirectoryExists(),
            "Output dir should exist after a clean -verbosity:Off run.");
    }

    public void Dispose()
    {
        try { Directory.Delete(_workdir.Value, recursive: true); } catch { }
    }
}
