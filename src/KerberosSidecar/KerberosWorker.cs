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
            await EnsureTgt(true);
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
        if (_options.CurrentValue.GenerateKrb5)
        {
            await File.WriteAllTextAsync(_options.CurrentValue.Kerb5ConfigFile, _options.CurrentValue.KerberosClient.Configuration.Serialize(), _cancellationToken);
        }
    }

    /// <summary>
    /// Authenticates the principal and populates ticket cache
    /// </summary>
    private async Task EnsureTgt(bool initial)
    {

        try
        {

            var ticketCache = (Krb5TicketCache)_options.CurrentValue.KerberosClient.Cache;
            var tgt = ticketCache.Krb5Cache.Credentials.FirstOrDefault(x => x.Server.Name.Contains("krbtgt"));
            var credentials = await _credentialFactory.Get(_options.CurrentValue, _cancellationToken);

            var hasTgt = tgt != null;
            var tgtNeedsRenewal = tgt != null && DateTimeOffset.UtcNow.AddMinutes(15) > tgt.RenewTill && tgt.EndTime < DateTimeOffset.UtcNow;
            if (tgt == null || tgt.EndTime < DateTimeOffset.UtcNow)
            {
                await _options.CurrentValue.KerberosClient.Authenticate(credentials);
            }
            else if (DateTimeOffset.UtcNow.AddMinutes(15) > tgt.RenewTill)
            {
                await _options.CurrentValue.KerberosClient.RenewTicket();
            }
            
            if (initial)
            {
                _logger.LogInformation("Service authenticated successfully as '{Principal}'", credentials.UserName);
            }
            else
            {
                _logger.LogDebug("Service successfully renewed TGT ticket");
            }

            _tgtHealthCheck.LastException = null;
            
        }
        catch (Exception e)
        {
            _tgtHealthCheck.LastException = e;
        }
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
        foreach (var (encryptionType, salt) in credentials.Salts)
        {
            var key = new KerberosKey(_options.CurrentValue.Password, new PrincipalName(PrincipalNameType.NT_PRINCIPAL, realm, new[] { $"{credentials.UserName}@{credentials.Domain.ToUpper()}" }), salt: salt, etype: encryptionType);
            kerberosKeys.Add(key);
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
            await EnsureTgt(false);
            await Task.Delay(1000, stoppingToken);
        }
    }
    
}