using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;

namespace KerberosSidecar.HealthChecks;

public class OptionsHealthCheck  : IHealthCheck
{
    private readonly IOptionsMonitor<KerberosOptions> _options;

    public OptionsHealthCheck(IOptionsMonitor<KerberosOptions> options)
    {
        _options = options;
    }

    public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = new CancellationToken())
    {
        try
        {
            var _ = _options.CurrentValue;
            return Task.FromResult(HealthCheckResult.Healthy("Options are valid"));
        }
        catch (OptionsValidationException e)
        {
            return Task.FromResult(HealthCheckResult.Unhealthy(string.Join("\n",e.Failures)));
        }
    }
}