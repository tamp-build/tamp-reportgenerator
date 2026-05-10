using Tamp;
using Tamp.NetCli.V10;

/// <summary>
/// tamp-reportgenerator's self-hosted build script. Test target runs
/// the unit tests only — integration tests need reportgenerator on
/// the runner and are excluded from the standard CI pipeline.
/// </summary>
class Build : TampBuild
{
    public static int Main(string[] args) => Execute<Build>(args);

    [Parameter("Build configuration")]
    Configuration Configuration = IsLocalBuild ? Configuration.Debug : Configuration.Release;

    [Parameter("Package version override", EnvironmentVariable = "PACKAGE_VERSION")]
#pragma warning disable CS0649
    readonly string? Version;
#pragma warning restore CS0649

    [Solution] readonly Solution Solution = null!;
    [GitRepository] readonly GitRepository Git = null!;

    static readonly Secret? NuGetApiKey =
        Environment.GetEnvironmentVariable("NUGET_API_KEY") is { Length: > 0 } v
            ? new Secret("NuGet API key", v)
            : null;

    AbsolutePath Artifacts => RootDirectory / "artifacts";

    Target Info => _ => _.Executes(() =>
    {
        Console.WriteLine($"  Branch:        {Git.Branch ?? "<detached>"}");
        Console.WriteLine($"  Commit:        {Git.Commit[..7]}");
        Console.WriteLine($"  Configuration: {Configuration}");
    });

    Target Clean => _ => _
        .TopLevel()
        .Executes(() =>
        {
            foreach (var d in RootDirectory.GlobDirectories("**/bin", "**/obj")) d.Delete();
            Artifacts.Delete();
        });

    Target Restore => _ => _
        .Executes(() => DotNet.Restore(s => s.SetProject(Solution.Path)));

    Target Compile => _ => _
        .TopLevel()
        .DependsOn(nameof(Restore))
        .Executes(() => DotNet.Build(s => s
            .SetProject(Solution.Path)
            .SetConfiguration(Configuration)
            .SetNoRestore(true)));

    Target Test => _ => _
        .TopLevel()
        .DependsOn(nameof(Compile))
        .Description("Unit tests only — integration tests need reportgenerator on PATH and are run separately.")
        .Executes(() => DotNet.Test(s => s
            .SetProject(RootDirectory / "tests" / "Tamp.ReportGenerator.V5.Tests" / "Tamp.ReportGenerator.V5.Tests.csproj")
            .SetConfiguration(Configuration)
            .SetNoBuild(true)
            .AddLogger("trx;LogFileName=test-results.trx")
            .SetResultsDirectory(Artifacts / "test-results")));

    Target Pack => _ => _
        .TopLevel()
        .DependsOn(nameof(Test))
        .Executes(() => DotNet.Pack(s =>
        {
            s.SetProject(RootDirectory / "src" / "Tamp.ReportGenerator.V5" / "Tamp.ReportGenerator.V5.csproj");
            s.SetConfiguration(Configuration);
            s.SetNoBuild(true);
            s.SetOutput(Artifacts);
            if (!string.IsNullOrEmpty(Version)) s.SetProperty("Version", Version);
        }));

    Target Push => _ => _
        .TopLevel()
        .DependsOn(nameof(Pack))
        .Requires(() => NuGetApiKey != null)
        .Executes(() => Artifacts.GlobFiles("*.nupkg")
            .Select(p => DotNet.NuGetPush(s => s
                .SetPackagePath(p)
                .SetSource("https://api.nuget.org/v3/index.json")
                .SetApiKey(NuGetApiKey!)
                .SetSkipDuplicate(true))));

    Target Ci => _ => _
        .TopLevel()
        .DependsOn(nameof(Info), nameof(Clean), nameof(Pack));

    Target Default => _ => _.DependsOn(nameof(Compile));
}
