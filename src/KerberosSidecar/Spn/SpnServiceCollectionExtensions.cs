// using Kerberos.NET.Credentials;
// using Microsoft.Extensions.DependencyInjection.Extensions;
// using Microsoft.Extensions.Options;
//
// namespace KerberosTicketRefresher.Spn;
//
// public static class SpnServiceCollectionExtensions
// {
//     public static IServiceCollection AddSpnManagement(this IServiceCollection services)
//     {
//         if (Platform.IsCloudFoundry)
//         {
//             services.TryAddSingleton<IRouteProvider, CloudFoundryRouteProvider>();
//         }
//         else
//         {
//             services.AddOptions<SimpleRouteProviderOptions>().BindConfiguration("SpnManagement");
//             services.TryAddSingleton<IRouteProvider, SimpleRouteProvider>();
//         }
//         services.AddOptions<SpnManagerOptions>()
//             .BindConfiguration("SpnManagement")
//             .PostConfigure(opt =>
//             {
//                 opt.Enabled ??= opt.ServiceUrl != null;
//                 opt.ServiceUrl ??= "https://localhost:7167";
//             })
//             .Validate(options => !(options.Enabled.HasValue && options.Enabled.Value && options.ServiceUrl == null), "Spn Management is enabled but management service URL is not set");
//         services.AddTransient<KerberosMessageHandler>();
//         services.AddHttpClient<ISpnClient, RestSpnClient>((ctx, client) =>
//             {
//                 var options = ctx.GetRequiredService<IOptionsMonitor<SpnManagerOptions>>().CurrentValue;
//                 client.BaseAddress = new Uri(options.ServiceUrl!);
//             })
//             .AddHttpMessageHandler<KerberosMessageHandler>();
//         services.AddHostedService<SpnManagerHostedService>();
//         return services;
//     }
// }