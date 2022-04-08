#pragma warning disable CS1998
using Kerberos.NET.Client;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;

namespace KerberosSidecar.HealthChecks;

public class TgtHealthCheck : IHealthCheck
{
    private readonly IOptionsMonitor<KerberosOptions> _options;

    public TgtHealthCheck(IOptionsMonitor<KerberosOptions> options)
    {
        _options = options;
    }

    public Exception? LastException { get; set; }
    public bool IsReady { get; set; }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        if (!IsReady)
        {
            return HealthCheckResult.Unhealthy($"Not finished starting up");
        }

        try
        {

            var ticketCache = (Krb5TicketCache)_options.CurrentValue.KerberosClient.Cache;
            var tgt = ticketCache.Krb5Cache.Credentials.FirstOrDefault(x => x.Server.Name.Contains("krbtgt"));
            if (tgt != null)
            {
                if (tgt.EndTime > DateTimeOffset.UtcNow)
                {
                    if (LastException == null)
                    {
                        return HealthCheckResult.Healthy($"TGT successfully acquired for {tgt!.Client.FullyQualifiedName} until {tgt.EndTime}", data: new Dictionary<string, object>()
                        {
                            { "StartTime", tgt.StartTime },
                            { "AuthTime", tgt.AuthTime },
                            { "EndTime", tgt.EndTime },
                            { "Principal", tgt.Client.FullyQualifiedName },
                        });
                    }

                    return HealthCheckResult.Degraded("A valid TGT exists in ticket cache, but attempt to reacquire new TGT failed", LastException);
                }

                return HealthCheckResult.Unhealthy($"TGT is expired and has not been renewed", LastException);
            }

            return HealthCheckResult.Unhealthy($"TGT not found in cache", LastException);
        }
        catch (OptionsValidationException)
        {
            return HealthCheckResult.Unhealthy($"App not properly configured");
        }
    }
}