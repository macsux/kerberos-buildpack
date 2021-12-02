using Kerberos.NET.Client;
using Kerberos.NET.Credentials;
using Microsoft.Extensions.Options;

namespace KerberosTicketRefresher;

public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;
    private readonly KerberosClient _client;
    private readonly IHostApplicationLifetime _lifetime;
    private readonly KerberosOptions _options;

    public Worker(ILogger<Worker> logger, KerberosClient client, IOptions<KerberosOptions> options, IHostApplicationLifetime lifetime)
    {
        _logger = logger;
        _client = client;
        _lifetime = lifetime;
        _options = options.Value;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // KerberosClient has it's own background task to renew TGT, we just gotta keep the app alive after getting initial one
        var credentials = new KerberosPasswordCredential(_options.ServiceAccount, _options.Password);
        await _client.Authenticate(credentials);
        _logger.LogInformation("Initial TGT acquired");
        if (_options.RunOnce)
        {
            _lifetime.StopApplication();
        }
        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(10000, stoppingToken);
        }
    }
}
