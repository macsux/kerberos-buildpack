using JetBrains.Annotations;

namespace KerberosBuildpack;

[PublicAPI]
public class BuildpackContext
{
    public string ApplicationDirectory { get; init; } = null!;
    public string? DependenciesDirectory { get; init; }
    public int BuildpackIndex { get; init; }
    public string? CurrentDependencyDirectory => DependenciesDirectory != null ? Path.Combine(DependenciesDirectory, BuildpackIndex.ToString()) : null;
    protected Dictionary<string,string> EnvironmentalVariables { get; } = new Dictionary<string, string>();
}