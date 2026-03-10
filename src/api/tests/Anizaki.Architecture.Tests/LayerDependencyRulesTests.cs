using System.Reflection;
using System.Xml.Linq;
using Anizaki.Application.Features.SystemStatus.Contracts;
using Anizaki.Domain.Abstractions;
using Anizaki.Infrastructure.SystemStatus;

namespace Anizaki.Architecture.Tests;

public class LayerDependencyRulesTests
{
    private static readonly Assembly DomainAssembly = typeof(Entity<>).Assembly;
    private static readonly Assembly ApplicationAssembly = typeof(GetSystemStatusQuery).Assembly;
    private static readonly Assembly InfrastructureAssembly = typeof(SystemStatusProbe).Assembly;
    private static readonly Assembly ApiAssembly = Assembly.Load("Anizaki.Api");

    [Fact]
    public void Domain_ShouldNotReferenceOuterLayers()
    {
        var referenced = DomainAssembly.GetReferencedAssemblies().Select(a => a.Name).ToHashSet(StringComparer.Ordinal);

        Assert.DoesNotContain("Anizaki.Application", referenced);
        Assert.DoesNotContain("Anizaki.Infrastructure", referenced);
        Assert.DoesNotContain("Anizaki.Api", referenced);
    }

    [Fact]
    public void Application_ShouldNotReferenceApi()
    {
        var referenced = ApplicationAssembly.GetReferencedAssemblies().Select(a => a.Name).ToHashSet(StringComparer.Ordinal);

        Assert.DoesNotContain("Anizaki.Api", referenced);
    }

    [Fact]
    public void Infrastructure_ShouldNotReferenceApi()
    {
        var referenced = InfrastructureAssembly.GetReferencedAssemblies().Select(a => a.Name).ToHashSet(StringComparer.Ordinal);

        Assert.DoesNotContain("Anizaki.Api", referenced);
    }

    [Fact]
    public void Api_ShouldReferenceApplicationAndInfrastructure()
    {
        var referenced = ApiAssembly.GetReferencedAssemblies().Select(a => a.Name).ToHashSet(StringComparer.Ordinal);

        Assert.Contains("Anizaki.Application", referenced);
        Assert.Contains("Anizaki.Infrastructure", referenced);
    }

    [Fact]
    public void ProjectReferences_ShouldFollowDependencyDirection()
    {
        var repoRoot = ResolveRepositoryRoot();
        var graph = new Dictionary<string, IReadOnlyCollection<string>>
        {
            ["Anizaki.Domain"] = ReadProjectReferences(Path.Combine(repoRoot, "src/api/src/Anizaki.Domain/Anizaki.Domain.csproj")),
            ["Anizaki.Application"] = ReadProjectReferences(Path.Combine(repoRoot, "src/api/src/Anizaki.Application/Anizaki.Application.csproj")),
            ["Anizaki.Infrastructure"] = ReadProjectReferences(Path.Combine(repoRoot, "src/api/src/Anizaki.Infrastructure/Anizaki.Infrastructure.csproj")),
            ["Anizaki.Api"] = ReadProjectReferences(Path.Combine(repoRoot, "src/api/src/Anizaki.Api/Anizaki.Api.csproj")),
        };

        Assert.Empty(FindForbiddenReferenceViolations(graph));
    }

    [Fact]
    public void GuardrailEvaluator_ShouldDetectForbiddenReferencesInDeliberateViolationGraph()
    {
        var simulatedGraph = new Dictionary<string, IReadOnlyCollection<string>>
        {
            ["Anizaki.Domain"] = new[] { "Anizaki.Application" },
            ["Anizaki.Application"] = new[] { "Anizaki.Domain" },
            ["Anizaki.Infrastructure"] = new[] { "Anizaki.Application", "Anizaki.Domain" },
            ["Anizaki.Api"] = new[] { "Anizaki.Application", "Anizaki.Infrastructure" },
        };

        var violations = FindForbiddenReferenceViolations(simulatedGraph);

        Assert.Contains(violations, v => v.Contains("Anizaki.Domain must not reference Anizaki.Application", StringComparison.Ordinal));
    }

    private static string ResolveRepositoryRoot()
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);
        while (current is not null && !File.Exists(Path.Combine(current.FullName, "AGENTS.md")))
        {
            current = current.Parent;
        }

        return current?.FullName
               ?? throw new InvalidOperationException("Could not resolve repository root.");
    }

    private static IReadOnlyCollection<string> ReadProjectReferences(string projectFilePath)
    {
        var document = XDocument.Load(projectFilePath);
        return document
            .Descendants()
            .Where(node => node.Name.LocalName == "ProjectReference")
            .Select(node => node.Attribute("Include")?.Value)
            .Where(include => !string.IsNullOrWhiteSpace(include))
            .Select(include => Path.GetFileNameWithoutExtension(include!))
            .ToHashSet(StringComparer.Ordinal);
    }

    private static List<string> FindForbiddenReferenceViolations(
        IReadOnlyDictionary<string, IReadOnlyCollection<string>> graph)
    {
        var violations = new List<string>();
        AddForbiddenReferenceViolations(graph, "Anizaki.Domain", ["Anizaki.Application", "Anizaki.Infrastructure", "Anizaki.Api"], violations);
        AddForbiddenReferenceViolations(graph, "Anizaki.Application", ["Anizaki.Api"], violations);
        AddForbiddenReferenceViolations(graph, "Anizaki.Infrastructure", ["Anizaki.Api"], violations);
        return violations;
    }

    private static void AddForbiddenReferenceViolations(
        IReadOnlyDictionary<string, IReadOnlyCollection<string>> graph,
        string project,
        IReadOnlyCollection<string> forbiddenReferences,
        ICollection<string> violations)
    {
        if (!graph.TryGetValue(project, out var references))
        {
            return;
        }

        foreach (var forbidden in forbiddenReferences)
        {
            if (references.Contains(forbidden, StringComparer.Ordinal))
            {
                violations.Add($"{project} must not reference {forbidden}.");
            }
        }
    }
}
