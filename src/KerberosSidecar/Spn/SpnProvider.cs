namespace KerberosSidecar.Spn;

public class SpnProvider
{
    private readonly IRouteProvider _routeProvider;
    private readonly ISpnClient _spnClient;
    private readonly ILogger<SpnProvider> _logger;

    public SpnProvider(IRouteProvider routeProvider, ISpnClient spnClient, ILogger<SpnProvider> logger)
    {
        _routeProvider = routeProvider;
        _spnClient = spnClient;
        _logger = logger;
    }

    public Task<List<string>> GetSpnsForAppRoutes(CancellationToken cancellationToken = default) => GetSpnsForAppRoutes("http", cancellationToken);
    public async Task<List<string>> GetSpnsForAppRoutes(string serviceType, CancellationToken cancellationToken = default)
    {
        var routes = await _routeProvider.GetRoutes(cancellationToken);
        var spns = routes.Select(route => $"{serviceType}/{route.Host}").ToList();
        return spns;
    }

    public async Task EnsureSpns(CancellationToken cancellationToken)
    {
        try
        {
            var expectedSpns = await GetSpnsForAppRoutes(cancellationToken);
            var existingSpns = (await _spnClient.GetAllSpn(cancellationToken)).ToHashSet();
            var spnsToAdd = new HashSet<string>(expectedSpns);
            spnsToAdd.ExceptWith(existingSpns);

            foreach (var spn in spnsToAdd)
            {
                await _spnClient.AddSpn(spn, cancellationToken);
            }

        }
        catch (Exception e)
        {
            _logger.LogError(e, "Failed to manage SPNs");
        }
    }
}



