using JetBrains.Annotations;

namespace KerberosSidecar.Spn;

[PublicAPI]
public class SimpleRouteProviderOptions
{
    public List<string> Routes { get; set; } = new();
}