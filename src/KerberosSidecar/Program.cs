#pragma warning disable IL2026
using Kerberos.NET.Client;
using Kerberos.NET.Configuration;
using Kerberos.NET.Credentials;
using KerberosSidecar;
using KerberosSidecar.HealthChecks;
using KerberosSidecar.Spn;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;


var webHostBuilder = WebApplication.CreateBuilder(args);

webHostBuilder.Configuration.AddYamlFile("appsettings.yaml", optional: true, reloadOnChange: true);
webHostBuilder.Configuration.AddYamlFile($"appsettings.{webHostBuilder.Environment.EnvironmentName}.yaml", optional: true, reloadOnChange: true);

var services = webHostBuilder.Services;
services.AddOptions<KerberosOptions>()
    .Configure(c =>
    {
        var config = webHostBuilder.Configuration;
        c.CacheFile = config.GetValue<string>("KRB5CCNAME");
        c.Kerb5ConfigFile = config.GetValue<string>("KRB5_CONFIG");
        c.ServiceAccount = config.GetValue<string>("KRB_SERVICE_ACCOUNT");
        c.Password = config.GetValue<string>("KRB_PASSWORD");
        c.Kdc = config.GetValue<string>("KRB_KDC");
        c.RunOnce = config.GetValue<bool>("KRB_RunOnce");
    })
    .PostConfigure<ILoggerFactory>((options, loggerFactory) =>
    {
        var homeDir = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        var userKerbDir = Path.Combine(homeDir, ".krb5");

        // default files to user's ~/.krb/ folder if not set
        options.Kerb5ConfigFile ??= Path.Combine(userKerbDir, "krb5.conf");
        options.KeytabFile ??= Path.Combine(userKerbDir, "krb5.keytab");
        options.CacheFile ??= Path.Combine(userKerbDir, "krb5cc");
        Directory.CreateDirectory(Path.GetDirectoryName(options.Kerb5ConfigFile)!);
        Directory.CreateDirectory(Path.GetDirectoryName(options.KeytabFile)!);
        Directory.CreateDirectory(Path.GetDirectoryName(options.CacheFile)!);

        // var config = File.Exists(options.Kerb5ConfigFile) ? Krb5Config.Parse(File.ReadAllText(options.Kerb5ConfigFile)) : Krb5Config.Default();
        var config = Krb5Config.Default();
        config.Defaults.DefaultCCacheName = options.CacheFile;
        string realm;
        try
        {
            realm = new KerberosPasswordCredential(options.ServiceAccount, options.Password).Domain;
        }
        catch (Exception)
        {
            return; // we're gonna handle this case during validation
        }
        options.Kdc ??= realm;
        if (realm != null)
        {
            config.Realms[realm].Kdc.Add(options.Kdc);
        }

        var client = new KerberosClient(config, loggerFactory);
        client.CacheInMemory = false;
        client.Cache = new Krb5TicketCache(options.CacheFile);
        client.RenewTickets = true;
        options.KerberosClient = client;
    });
services.AddSingleton<IValidateOptions<KerberosOptions>, KerberosOptions.Validator>();

services.AddSingleton<KerberosCredentialFactory>();
services.AddSingleton<SpnProvider>();
if (Platform.IsCloudFoundry)
{
    services.TryAddSingleton<IRouteProvider, CloudFoundryRouteProvider>();
}
else
{
    services.AddOptions<SimpleRouteProviderOptions>().BindConfiguration("");
    services.TryAddSingleton<IRouteProvider, SimpleRouteProvider>();
}


var spnServiceUrl = webHostBuilder.Configuration.GetValue<string?>("SpnManagement:ServiceUrl");
if (spnServiceUrl != null)
{
    services.AddHttpClient<ISpnClient, RestSpnClient>();
}
else
{
    services.AddSingleton<ISpnClient, LoggingSpnClient>();
}

services.AddSingleton<TgtHealthCheck>();
var healthChecks = services.AddHealthChecks();
healthChecks.AddCheck<TgtHealthCheck>("kerberos-tgt");
if (services.FirstOrDefault(x => x.ServiceType == typeof(ISpnClient))?.ImplementationType != typeof(LoggingSpnClient))
{
    healthChecks.AddCheck<SpnHealthCheck>("spn", HealthStatus.Degraded);
}

services.AddHostedService<KerberosWorker>();

var app = webHostBuilder.Build();
app.UseRouting();
app.UseEndpoints(endpoints =>
{
    endpoints.MapHealthChecks("/health/ready", new HealthCheckOptions().WithJsonDetails());
});
app.Logger.LogInformation("Kerberos sidecar started....");
await app.RunAsync();
