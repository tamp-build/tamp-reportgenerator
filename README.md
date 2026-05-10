# Tamp.ReportGenerator

ReportGenerator wrapper for [Tamp](https://github.com/tamp-build/tamp).

| Package | ReportGenerator | Status |
|---|---|---|
| [`Tamp.ReportGenerator.V5`](src/Tamp.ReportGenerator.V5) | 5.x | live |

Converts coverage reports (Cobertura, OpenCover, lcov, etc.) into HTML,
Markdown, Badges, SonarQube, and 30+ other formats.

## Why a separate repo

ReportGenerator is a third-party tool by Daniel Palme with its own
release cadence (5.x has been out for years; minor versions every couple
months). Per the Tamp satellite-repo convention, third-party tools live
outside main.

## Quick example — pair with Tamp.DotNetCoverage

```csharp
using Tamp;
using Tamp.DotNetCoverage.V18;
using Tamp.NetCli.V10;
using Tamp.ReportGenerator.V5;

class Build : TampBuild
{
    public static int Main(string[] args) => Execute<Build>(args);

    [NuGetPackage("dotnet-coverage", Version = "18.6.2")]
    readonly Tool DotNetCoverageTool = null!;

    [NuGetPackage("dotnet-reportgenerator-globaltool", Version = "5.5.10")]
    readonly Tool ReportGeneratorTool = null!;

    AbsolutePath CoverageDir => RootDirectory / "artifacts" / "coverage";

    Target Test => _ => _.Executes(() => DotNet.Test(s => s
        .AddDataCollector("Code Coverage")
        .SetResultsDirectory(CoverageDir)));

    Target Coverage => _ => _
        .DependsOn(nameof(Test))
        .Executes(() => DotNetCoverage.Merge(DotNetCoverageTool, m => m
            .AddInputs(CoverageDir.GlobFiles("**/*.coverage"))
            .SetOutput(CoverageDir / "coverage.cobertura.xml")
            .SetOutputFormat(CoverageFormat.Cobertura)));

    Target CoverageReport => _ => _
        .DependsOn(nameof(Coverage))
        .Executes(() => ReportGenerator.Run(ReportGeneratorTool, s => s
            .AddReport((CoverageDir / "coverage.cobertura.xml").Value)
            .SetTargetDir((CoverageDir / "html").Value)
            .AddReportType(ReportGeneratorReportType.Html)
            .AddReportType(ReportGeneratorReportType.MarkdownSummaryGithub)
            .AddReportType(ReportGeneratorReportType.Badges)
            .SetTitle("My App Coverage")
            .SetTag(Git.Commit[..7])));
}
```

## License

[MIT](LICENSE) — same as `tamp` core. (ReportGenerator itself is Apache 2.0.)
