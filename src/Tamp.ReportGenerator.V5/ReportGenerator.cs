namespace Tamp.ReportGenerator.V5;

/// <summary>
/// Wrapper for the ReportGenerator 5.x CLI (<c>reportgenerator</c> /
/// <c>dotnet-reportgenerator-globaltool</c>).
/// </summary>
/// <remarks>
/// <para>Resolve the tool via <c>[NuGetPackage]</c>:</para>
/// <code>
/// [NuGetPackage("dotnet-reportgenerator-globaltool", Version = "5.5.10")]
/// readonly Tool ReportGenerator;
/// </code>
/// <para>
/// Pairs naturally with <c>Tamp.DotNetCoverage.V18</c> (or any other
/// coverage producer that emits Cobertura) — Coverage produces the
/// raw report, ReportGenerator turns it into an HTML / Markdown /
/// Sonar-friendly format.
/// </para>
/// </remarks>
public static class ReportGenerator
{
    /// <summary>
    /// Build a <c>reportgenerator</c> command. Reports + TargetDir are
    /// required; everything else is optional.
    /// </summary>
    public static CommandPlan Run(Tool tool, Action<ReportGeneratorSettings> configure)
    {
        if (tool is null) throw new ArgumentNullException(nameof(tool));
        if (configure is null) throw new ArgumentNullException(nameof(configure));
        var s = new ReportGeneratorSettings();
        configure(s);

        if (s.Reports.Count == 0)
            throw new InvalidOperationException("ReportGenerator: at least one report is required (set via AddReport / AddReports).");
        if (string.IsNullOrEmpty(s.TargetDir))
            throw new InvalidOperationException("ReportGenerator: TargetDir is required (set via SetTargetDir).");

        var args = new List<string>
        {
            $"-reports:{string.Join(';', s.Reports)}",
            $"-targetdir:{s.TargetDir}",
        };

        if (s.ReportTypes.Count > 0) args.Add($"-reporttypes:{string.Join(';', s.ReportTypes)}");
        if (s.SourceDirs.Count > 0) args.Add($"-sourcedirs:{string.Join(';', s.SourceDirs)}");
        if (!string.IsNullOrEmpty(s.HistoryDir)) args.Add($"-historydir:{s.HistoryDir}");
        if (s.Plugins.Count > 0) args.Add($"-plugins:{string.Join(';', s.Plugins)}");
        if (s.AssemblyFilters.Count > 0) args.Add($"-assemblyfilters:{string.Join(';', s.AssemblyFilters)}");
        if (s.ClassFilters.Count > 0) args.Add($"-classfilters:{string.Join(';', s.ClassFilters)}");
        if (s.FileFilters.Count > 0) args.Add($"-filefilters:{string.Join(';', s.FileFilters)}");
        if (s.RiskHotspotAssemblyFilters.Count > 0) args.Add($"-riskhotspotassemblyfilters:{string.Join(';', s.RiskHotspotAssemblyFilters)}");
        if (s.RiskHotspotClassFilters.Count > 0) args.Add($"-riskhotspotclassfilters:{string.Join(';', s.RiskHotspotClassFilters)}");
        if (s.Verbosity is { } v) args.Add($"-verbosity:{v}");
        if (!string.IsNullOrEmpty(s.Title)) args.Add($"-title:{s.Title}");
        if (!string.IsNullOrEmpty(s.Tag)) args.Add($"-tag:{s.Tag}");
        if (s.License is { } lic) args.Add($"-license:{lic.Reveal()}");

        return new CommandPlan
        {
            Executable = tool.Executable.Value,
            Arguments = args,
            Environment = new Dictionary<string, string>(s.EnvironmentVariables),
            WorkingDirectory = s.WorkingDirectory ?? tool.WorkingDirectory,
            Secrets = s.License is null ? Array.Empty<Secret>() : new[] { s.License },
        };
    }
}
