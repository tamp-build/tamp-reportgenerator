namespace Tamp.ReportGenerator.V5;

/// <summary>
/// Verbosity for ReportGenerator. Maps to <c>-verbosity:</c>.
/// </summary>
public enum ReportGeneratorVerbosity
{
    Verbose,
    Info,
    Warning,
    Error,
    Off,
}

/// <summary>
/// Common ReportGenerator output types as constants. ReportGenerator
/// accepts ~35 named types in V5 (and adds more in patch releases) so
/// the wrapper takes types as <see cref="string"/> rather than an enum
/// — these constants exist for IntelliSense ergonomics.
/// </summary>
public static class ReportGeneratorReportType
{
    public const string Badges = "Badges";
    public const string Cobertura = "Cobertura";
    public const string CodeClimate = "CodeClimate";
    public const string CsvSummary = "CsvSummary";
    public const string Html = "Html";
    public const string HtmlSummary = "HtmlSummary";
    public const string HtmlInline = "HtmlInline";
    public const string HtmlInline_AzurePipelines = "HtmlInline_AzurePipelines";
    public const string HtmlInline_AzurePipelines_Dark = "HtmlInline_AzurePipelines_Dark";
    public const string HtmlInline_AzurePipelines_Light = "HtmlInline_AzurePipelines_Light";
    public const string Html_BlueRed = "Html_BlueRed";
    public const string Html_Dark = "Html_Dark";
    public const string Html_Light = "Html_Light";
    public const string HtmlChart = "HtmlChart";
    public const string JsonSummary = "JsonSummary";
    public const string Lcov = "lcov";
    public const string Markdown = "Markdown";
    public const string MarkdownSummary = "MarkdownSummary";
    public const string MarkdownSummaryGithub = "MarkdownSummaryGithub";
    public const string MarkdownDeltaSummary = "MarkdownDeltaSummary";
    public const string MarkdownAssembliesSummary = "MarkdownAssembliesSummary";
    public const string OpenCover = "OpenCover";
    public const string PngChart = "PngChart";
    public const string SonarQube = "SonarQube";
    public const string SvgChart = "SvgChart";
    public const string TeamCitySummary = "TeamCitySummary";
    public const string TextSummary = "TextSummary";
    public const string TextDeltaSummary = "TextDeltaSummary";
    public const string Xml = "Xml";
    public const string XmlSummary = "XmlSummary";
}

/// <summary>
/// Settings for a <c>reportgenerator</c> invocation.
/// </summary>
/// <remarks>
/// <para>
/// ReportGenerator's flags use the unusual <c>-flag:value</c> form
/// (single dash, colon separator, value combined in the same arg).
/// List-typed values are joined with semicolons inside that single arg.
/// The wrapper handles all of this.
/// </para>
/// <para>
/// Filters use a <c>+include;-exclude</c> grammar where each entry
/// starts with <c>+</c> or <c>-</c>. Pass them as raw strings — the
/// wrapper doesn't try to parse the filter language.
/// </para>
/// </remarks>
public sealed class ReportGeneratorSettings
{
    /// <summary>Coverage report files to read. Maps to <c>-reports:</c>. Globs supported.</summary>
    public List<string> Reports { get; } = [];

    /// <summary>Output directory for generated reports. Required. Maps to <c>-targetdir:</c>.</summary>
    public string? TargetDir { get; set; }

    /// <summary>Output report types (e.g. <c>Html</c>, <c>Cobertura</c>, <c>Badges</c>). Maps to <c>-reporttypes:</c>. See <see cref="ReportGeneratorReportType"/> for common names.</summary>
    public List<string> ReportTypes { get; } = [];

    /// <summary>Source directories — used when coverage data lacks path info. Maps to <c>-sourcedirs:</c>.</summary>
    public List<string> SourceDirs { get; } = [];

    /// <summary>Directory to store historical data for trend reporting. Maps to <c>-historydir:</c>.</summary>
    public string? HistoryDir { get; set; }

    /// <summary>Plugin assemblies for custom report types. Maps to <c>-plugins:</c>.</summary>
    public List<string> Plugins { get; } = [];

    /// <summary>Assembly include / exclude filters. Each entry starts with <c>+</c> or <c>-</c> (e.g. <c>+MyApp*</c>, <c>-Tests*</c>). Maps to <c>-assemblyfilters:</c>.</summary>
    public List<string> AssemblyFilters { get; } = [];

    /// <summary>Class include / exclude filters. Maps to <c>-classfilters:</c>.</summary>
    public List<string> ClassFilters { get; } = [];

    /// <summary>File include / exclude filters. Maps to <c>-filefilters:</c>.</summary>
    public List<string> FileFilters { get; } = [];

    /// <summary>Risk-hotspot assembly filters. Maps to <c>-riskhotspotassemblyfilters:</c>.</summary>
    public List<string> RiskHotspotAssemblyFilters { get; } = [];

    /// <summary>Risk-hotspot class filters. Maps to <c>-riskhotspotclassfilters:</c>.</summary>
    public List<string> RiskHotspotClassFilters { get; } = [];

    /// <summary>Verbosity. Maps to <c>-verbosity:</c>.</summary>
    public ReportGeneratorVerbosity? Verbosity { get; set; }

    /// <summary>Custom title shown in HTML / Markdown reports. Maps to <c>-title:</c>.</summary>
    public string? Title { get; set; }

    /// <summary>Tag for the report (commit / branch / build number). Maps to <c>-tag:</c>.</summary>
    public string? Tag { get; set; }

    /// <summary>ReportGenerator Pro license key. Maps to <c>-license:</c>. Pass as <see cref="Secret"/> so it's redacted.</summary>
    public Secret? License { get; set; }

    /// <summary>Working directory of the spawned process.</summary>
    public string? WorkingDirectory { get; set; }

    /// <summary>Per-invocation environment variables.</summary>
    public Dictionary<string, string> EnvironmentVariables { get; } = new();

    public ReportGeneratorSettings AddReport(string path) { Reports.Add(path); return this; }
    public ReportGeneratorSettings AddReports(IEnumerable<string> paths) { Reports.AddRange(paths); return this; }
    public ReportGeneratorSettings AddReports(IEnumerable<AbsolutePath> paths) { foreach (var p in paths) Reports.Add(p.Value); return this; }
    public ReportGeneratorSettings SetTargetDir(string? path) { TargetDir = path; return this; }
    public ReportGeneratorSettings AddReportType(string type) { ReportTypes.Add(type); return this; }
    public ReportGeneratorSettings AddReportTypes(IEnumerable<string> types) { ReportTypes.AddRange(types); return this; }
    public ReportGeneratorSettings AddSourceDir(string path) { SourceDirs.Add(path); return this; }
    public ReportGeneratorSettings SetHistoryDir(string? path) { HistoryDir = path; return this; }
    public ReportGeneratorSettings AddPlugin(string path) { Plugins.Add(path); return this; }
    public ReportGeneratorSettings AddAssemblyFilter(string filter) { AssemblyFilters.Add(filter); return this; }
    public ReportGeneratorSettings AddClassFilter(string filter) { ClassFilters.Add(filter); return this; }
    public ReportGeneratorSettings AddFileFilter(string filter) { FileFilters.Add(filter); return this; }
    public ReportGeneratorSettings AddRiskHotspotAssemblyFilter(string filter) { RiskHotspotAssemblyFilters.Add(filter); return this; }
    public ReportGeneratorSettings AddRiskHotspotClassFilter(string filter) { RiskHotspotClassFilters.Add(filter); return this; }
    public ReportGeneratorSettings SetVerbosity(ReportGeneratorVerbosity v) { Verbosity = v; return this; }
    public ReportGeneratorSettings SetTitle(string? title) { Title = title; return this; }
    public ReportGeneratorSettings SetTag(string? tag) { Tag = tag; return this; }
    public ReportGeneratorSettings SetLicense(Secret license) { License = license; return this; }
    public ReportGeneratorSettings SetWorkingDirectory(string? cwd) { WorkingDirectory = cwd; return this; }
    public ReportGeneratorSettings SetEnvironmentVariable(string name, string value) { EnvironmentVariables[name] = value; return this; }
}
