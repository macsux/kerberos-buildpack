using Kerberos.NET.Client;
using Kerberos.NET.Crypto;
using Kerberos.NET.Entities;
using KerberosSidecar.HealthChecks;
using KerberosSidecar.Spn;
using Microsoft.Extensions.Options;

namespace KerberosSidecar;

public class KerberosWorker : BackgroundService
{
    private readonly IOptionsMonitor<KerberosOptions> _options;
    private readonly ILogger<KerberosWorker> _logger;
    private readonly KerberosCredentialFactory _credentialFactory;
    private readonly SpnProvider _spnProvider;
    private readonly TgtHealthCheck _tgtHealthCheck;
    private readonly CancellationToken _cancellationToken;

    public KerberosWorker(
        IOptionsMonitor<KerberosOptions> options, 
        KerberosCredentialFactory credentialFactory, 
        SpnProvider spnProvider,
        TgtHealthCheck tgtHealthCheck,
        IHostApplicationLifetime lifetime,
        ILogger<KerberosWorker> logger)
    {
        _options = options;
        _logger = logger;
        _credentialFactory = credentialFactory;
        _spnProvider = spnProvider;
        _tgtHealthCheck = tgtHealthCheck;
        _cancellationToken = lifetime.ApplicationStopping;
    }

    private void OnOptionsChange(KerberosOptions options)
    {
        Task.Run(async () =>
        {
            try
            {
                await SetupMitKerberos();
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Error trying to authenticate after options change");
            }
        }, _cancellationToken);
    }

    private async Task SetupMitKerberos()
    {
        try
        {
            await CreateMitKerberosKrb5Config();
            await CreateMitKerberosKeytab();
            await EnsureTgt();
            await _spnProvider.EnsureSpns(_cancellationToken);
            _tgtHealthCheck.LastException = null;
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error initializing MIT Kerberos");
            _tgtHealthCheck.LastException = e;
        }
        finally
        {
            _tgtHealthCheck.IsReady = true;
        }
    }


    private async Task CreateMitKerberosKeytab()
    {
        var keytab = await GenerateKeytab();
        await using var fs = new FileStream(_options.CurrentValue.KeytabFile, FileMode.OpenOrCreate);
        await using var bw = new BinaryWriter(fs);
        keytab.Write(bw);
    }

    private async Task CreateMitKerberosKrb5Config()
    {
        await File.WriteAllTextAsync(_options.CurrentValue.Kerb5ConfigFile,  _options.CurrentValue.KerberosClient.Configuration.Serialize(), _cancellationToken);
    }

    /// <summary>
    /// Authenticates the principal and populates ticket cache
    /// </summary>
    private async Task EnsureTgt()
    {
        var credentials = await _credentialFactory.Get(_options.CurrentValue, _cancellationToken);
        await _options.CurrentValue.KerberosClient.Authenticate(credentials);
        _tgtHealthCheck.LastException = null;
        _logger.LogInformation("Service authenticated successfully as '{Principal}'", credentials.UserName);
        
    }

    /// <summary>
    /// Generates keytab from user credentials for each SPN associated with the app
    /// </summary>
    /// <remarks>
    /// Keytab is made up of Kerberos key entries. Each entry is made up of:
    /// - Kerberos principal name, which is service account + all aliases (SPNs)
    /// - Encryption key that derived from password
    /// - Salt to go with the key
    /// </remarks>
    private  async Task<KeyTable> GenerateKeytab()
    {
        var credentials = await _credentialFactory.Get(_options.CurrentValue, _cancellationToken);
        var spns = await _spnProvider.GetSpnsForAppRoutes(_cancellationToken);
        
        var realm = credentials.Domain;
        List<KerberosKey> kerberosKeys = new();
        foreach (var spn in spns)
        {
            foreach (var (encryptionType, salt) in credentials.Salts)
            {
                var key = new KerberosKey(_options.CurrentValue.Password, new PrincipalName(PrincipalNameType.NT_SRV_HST, realm, new[] { spn }), salt: salt, etype: encryptionType);
                kerberosKeys.Add(key);
            }
        }
        var keyTable = new KeyTable(kerberosKeys.ToArray());
        return keyTable;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _options.OnChange(OnOptionsChange);
        await SetupMitKerberos();
        while (!stoppingToken.IsCancellationRequested)
        {
            await RefreshTicketIfExpiring();
            await Task.Delay(1000, stoppingToken);
        }
    }

    private async Task RefreshTicketIfExpiring()
    {
        var ticketCache = (Krb5TicketCache)_options.CurrentValue.KerberosClient.Cache;
        var tgt = ticketCache.Krb5Cache.Credentials.FirstOrDefault(x => x.Server.Name.Contains("krbtgt"));
        if(tgt == null)
            return;
        if (DateTimeOffset.UtcNow.AddMinutes(15) > tgt.RenewTill)
        {
            await _options.CurrentValue.KerberosClient.RenewTicket();
        }
    }
}