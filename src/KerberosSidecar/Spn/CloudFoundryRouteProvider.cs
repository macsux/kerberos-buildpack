#pragma warning disable IL2026
using System.Text.Json.Nodes;

namespace KerberosSidecar.Spn;

public class CloudFoundryRouteProvider : IRouteProvider
{
    private readonly IConfiguration _configuration;

    public CloudFoundryRouteProvider(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public Task<IReadOnlyCollection<Uri>> GetRoutes(CancellationToken cancellationToken = default)
    {
        var vcapApplication = _configuration.GetValue<string>("VCAP_APPLICATION");
        if (vcapApplication == null)
        {
            throw new InvalidOperationException("VCAP_APPLICATION env var is not set");
        }
        List<Uri> routes = JsonNode.Parse(vcapApplication)?["application_uris"]?
            .AsArray()
            .Select(x => x?.GetValue<string>())
            .Where(x => x != null)
            .Select(x => new Uri($"https://{x}"))
            .ToList() ?? new List<Uri>();

        return Task.FromResult((IReadOnlyCollection<Uri>)routes.AsReadOnly());
    }
}