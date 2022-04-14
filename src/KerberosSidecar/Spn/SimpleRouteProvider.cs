using Microsoft.Extensions.Options;

namespace KerberosSidecar.Spn;

public class SimpleRouteProvider : IRouteProvider
{
    private readonly IOptionsMonitor<SimpleRouteProviderOptions> _options;

    public SimpleRouteProvider(IOptionsMonitor<SimpleRouteProviderOptions> options)
    {
        _options = options;
    }

    public Task<IReadOnlyCollection<Uri>> GetRoutes(CancellationToken cancellationToken = default) => 
        Task.FromResult((IReadOnlyCollection<Uri>)_options.CurrentValue.Routes.Select(x => new Uri(x)).ToList().AsReadOnly());
}