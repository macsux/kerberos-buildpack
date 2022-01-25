using KerberosSidecar.Spn;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace KerberosSidecar.HealthChecks;

public class SpnHealthCheck : IHealthCheck
{
    private readonly SpnProvider _spnProvider;
    private readonly ISpnClient _spnClient;

    public SpnHealthCheck(SpnProvider spnProvider, ISpnClient spnClient)
    {
        _spnProvider = spnProvider;
        _spnClient = spnClient;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        var expectedSpns = await _spnProvider.GetSpnsForAppRoutes(cancellationToken);
        var actualSpns = await _spnClient.GetAllSpn(cancellationToken);
        var missingSpns = new HashSet<string>(expectedSpns);
        missingSpns.ExceptWith(actualSpns);
        if (!missingSpns.Any())
        {
            return HealthCheckResult.Healthy("All required SPNs are registered");
        }
        else
        {
            return HealthCheckResult.Degraded($"The following required SPNs are missing:\n{string.Join("\n", missingSpns)}");
        }
    }
}