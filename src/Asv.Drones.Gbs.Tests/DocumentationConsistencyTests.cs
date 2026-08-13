using System.Text.Json;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace Asv.Drones.Gbs.Tests;

public partial class DocumentationConsistencyTests
{
    [Fact]
    public void LocalMarkdownLinks_ShouldResolve()
    {
        var markdownFiles = new[]
        {
            RepositoryPaths.Get("README.md"),
            RepositoryPaths.Get(".ai-factory", "DESCRIPTION.md"),
            RepositoryPaths.Get(".ai-factory", "ARCHITECTURE.md"),
        }
            .Concat(Directory.EnumerateFiles(RepositoryPaths.Get("docs"), "*.md"))
            .Order(StringComparer.Ordinal)
            .ToArray();

        var missing = new List<string>();
        foreach (var markdownFile in markdownFiles)
        {
            var content = File.ReadAllText(markdownFile);
            foreach (Match match in MarkdownLink().Matches(content))
            {
                var target = match.Groups[1].Value.Trim();
                if (IsExternalOrAnchor(target))
                {
                    continue;
                }

                var pathPart = target.Split(['#', '?'], 2)[0];
                var decodedPath = Uri.UnescapeDataString(pathPart);
                var resolvedPath = Path.GetFullPath(
                    Path.Combine(Path.GetDirectoryName(markdownFile)!, decodedPath)
                );
                if (File.Exists(resolvedPath) == false && Directory.Exists(resolvedPath) == false)
                {
                    missing.Add(
                        $"{Path.GetRelativePath(RepositoryPaths.Root, markdownFile)} -> {target}"
                    );
                }
            }
        }

        Assert.True(
            missing.Count == 0,
            $"Missing local Markdown targets:{Environment.NewLine}{string.Join(Environment.NewLine, missing)}"
        );
    }

    [Fact]
    public void DocumentedFrameworkProjectsAndCommands_ShouldMatchBuildFiles()
    {
        var props = XDocument.Load(RepositoryPaths.Get("src", "Directory.Build.props"));
        var framework = props.Descendants("TargetFrameworkValue").Single().Value;
        var readme = File.ReadAllText(RepositoryPaths.Get("README.md"));
        var solution = File.ReadAllText(RepositoryPaths.Get("src", "Asv.Drones.Gbs.sln"));
        var projectPaths = new[]
        {
            "src/Asv.Drones.Gbs/Asv.Drones.Gbs.csproj",
            "src/Asv.Drones.Gbs.Contracts/Asv.Drones.Gbs.Contracts.csproj",
            "src/Asv.Drones.Plugin.Gbs/Asv.Drones.Plugin.Gbs.csproj",
            "src/Asv.Drones.Plugin.Gbs.App/Asv.Drones.Plugin.Gbs.App.csproj",
            "src/Asv.Drones.Gbs.Tests/Asv.Drones.Gbs.Tests.csproj",
        };

        Assert.Equal("net10.0", framework);
        Assert.Contains(framework, readme, StringComparison.Ordinal);
        Assert.Contains("dotnet restore src/Asv.Drones.Gbs.sln", readme, StringComparison.Ordinal);
        Assert.Contains("dotnet build src/Asv.Drones.Gbs.sln", readme, StringComparison.Ordinal);
        Assert.Contains("dotnet test src/Asv.Drones.Gbs.sln", readme, StringComparison.Ordinal);

        foreach (var projectPath in projectPaths)
        {
            Assert.True(File.Exists(RepositoryPaths.Get(projectPath.Split('/'))), projectPath);
            var projectName = Path.GetFileNameWithoutExtension(projectPath);
            Assert.Contains(projectName, solution, StringComparison.Ordinal);
            Assert.Contains(projectName, readme, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void DocumentedProductionEndpoints_ShouldMatchSettings()
    {
        using var settings = JsonDocument.Parse(
            File.ReadAllText(
                RepositoryPaths.Get("src", "Asv.Drones.Gbs", "appsettings.Production.json")
            )
        );
        var root = settings.RootElement;
        var connections = root.GetProperty("Mavlink")
            .GetProperty("Connections")
            .EnumerateArray()
            .Select(x => x.GetString()!)
            .ToArray();
        var rtkConnection = root.GetProperty("Rtk").GetProperty("ConnectionString").GetString()!;
        var configurationDoc = File.ReadAllText(RepositoryPaths.Get("docs", "configuration.md"));
        var deploymentDoc = File.ReadAllText(RepositoryPaths.Get("docs", "deployment-linux.md"));

        Assert.Equal(
            ["tcps://0.0.0.0:7341?reconnect=0", "serial:/dev/ttyS1?br=115200"],
            connections
        );
        Assert.Equal("serial:/dev/ttyS2?br=115200", rtkConnection);

        foreach (var endpoint in connections.Append(rtkConnection))
        {
            Assert.Contains(endpoint, configurationDoc, StringComparison.Ordinal);
            Assert.Contains(endpoint, deploymentDoc, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void DocumentedReleaseTags_ShouldMatchWorkflowTriggers()
    {
        var devWorkflow = File.ReadAllText(
            RepositoryPaths.Get(".github", "workflows", "deploy-plugin-dev-nuget.yml")
        );
        var releaseWorkflow = File.ReadAllText(
            RepositoryPaths.Get(".github", "workflows", "deploy-plugin-nuget.yml")
        );
        var releaseDoc = File.ReadAllText(RepositoryPaths.Get("docs", "releases.md"));

        AssertTag(
            devWorkflow,
            releaseDoc,
            "plugin-v[0-9]+.[0-9]+.[0-9]+-dev.[0-9]+",
            "plugin-vX.Y.Z-dev.N"
        );
        AssertTag(devWorkflow, releaseDoc, "plugin-v[0-9]+.[0-9]+.[0-9]+-dev", "plugin-vX.Y.Z-dev");
        AssertTag(
            releaseWorkflow,
            releaseDoc,
            "plugin-v[0-9]+.[0-9]+.[0-9]+-rc.[0-9]+",
            "plugin-vX.Y.Z-rc.N"
        );
        AssertTag(
            releaseWorkflow,
            releaseDoc,
            "plugin-v[0-9]+.[0-9]+.[0-9]+-rc",
            "plugin-vX.Y.Z-rc"
        );
        AssertTag(releaseWorkflow, releaseDoc, "plugin-v[0-9]+.[0-9]+.[0-9]+", "plugin-vX.Y.Z");
    }

    private static bool IsExternalOrAnchor(string target)
    {
        return target.StartsWith('#')
            || target.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
            || target.StartsWith("https://", StringComparison.OrdinalIgnoreCase)
            || target.StartsWith("mailto:", StringComparison.OrdinalIgnoreCase);
    }

    private static void AssertTag(
        string workflow,
        string documentation,
        string workflowPattern,
        string documentedPattern
    )
    {
        Assert.Contains(workflowPattern, workflow, StringComparison.Ordinal);
        Assert.Contains(documentedPattern, documentation, StringComparison.Ordinal);
    }

    [GeneratedRegex(@"\[[^\]]+\]\(([^)]+)\)", RegexOptions.CultureInvariant)]
    private static partial Regex MarkdownLink();
}
