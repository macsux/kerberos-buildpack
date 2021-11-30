using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Kerberos.NET.Client;
using Kerberos.NET.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace KerberosTicketRefresher;

public class Program
{
    public static async Task Main(string[] args)
    {
        IHost host = Host.CreateDefaultBuilder(args)
            .ConfigureServices((context, services) =>
            {
                services.AddOptions<KerberosOptions>()
                    .Configure(c =>
                    {
                        var config = context.Configuration;
                        c.CacheFile = config.GetValue<string>("KRB5CCNAME");
                        c.Kerb5ConfigFile = config.GetValue<string>("KRB5_CONFIG");
                        c.ServiceAccount = config.GetValue<string>("KRB_SERVICE_ACCOUNT");
                        c.Password = config.GetValue<string>("KRB_PASSWORD");
                        c.RunOnce = config.GetValue<bool>("KRB_RunOnce");
                    })
                    .Validate(c => c.Kerb5ConfigFile != "null", "Required KRB5_CONFIG environmental variable is not set")
                    .Validate(c => File.Exists(c.Kerb5ConfigFile), "KRB5_CONFIG points to file that doesn't exist")
                    .Validate(c => c.CacheFile != "null", "Required KRB5CCNAME environmental variable is not set")
                    .Validate(c => c.ServiceAccount != null && Regex.IsMatch(c.ServiceAccount, "^.+?@.+$"), "KRB_SERVICE_ACCOUNT must be set in user@domain.com format")
                    .Validate(c => c.Password != null, "Required KRB_PASSWORD must be set");

                services.AddSingleton<Krb5Config>(svc =>
                {
                    var options = svc.GetRequiredService<IOptions<KerberosOptions>>().Value;
                    var config = Krb5Config.Parse(File.ReadAllText(options.Kerb5ConfigFile));
                    config.Defaults.DefaultCCacheName = options.CacheFile;
                    return config;
                });

                services.AddSingleton<KerberosClient>(svc =>
                {
                    var config = svc.GetRequiredService<Krb5Config>();
                    var loggerFactory = svc.GetRequiredService<ILoggerFactory>();
                    var options = svc.GetRequiredService<IOptions<KerberosOptions>>().Value;

                    var client = new KerberosClient(config, loggerFactory);
                    client.CacheInMemory = false;
                    client.Cache = new Krb5TicketCache(options.CacheFile);
                    client.RenewTickets = true;
                    return client;
                });
                services.AddHostedService<Worker>();
            })
            .Build();

        await host.RunAsync();
    }
}