using Kerberos.NET.Credentials;
using Microsoft.Extensions.Options;

namespace KerberosSidecar;

public class KerberosCredentialFactory
{
    private readonly IOptionsMonitor<KerberosOptions> _options;

    public KerberosCredentialFactory(IOptionsMonitor<KerberosOptions> options)
    {
        _options = options;
    }

    public async Task<KerberosPasswordCredential> Get(KerberosOptions options, CancellationToken cancellationToken = default) 
        => await Get(options.ServiceAccount, options.Password, cancellationToken: cancellationToken);
    public async Task<KerberosPasswordCredential> Get(string username, string password, string? domain = null, CancellationToken cancellationToken = default)
    {
        var credentials = new KerberosPasswordCredential(username, password, domain)
        {
            Configuration = _options.CurrentValue.KerberosClient.Configuration
        };
        await credentials.LoadSalts(cancellationToken);
        return credentials;
    }
}