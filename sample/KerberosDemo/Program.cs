using System;
using System.Collections.Generic;
using System.DirectoryServices.Protocols;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using KerberosDemo.Models;
using Microsoft.AspNetCore.Authentication.Negotiate;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi.Models;
using Steeltoe.Configuration.Kubernetes;
using Steeltoe.Configuration.Kubernetes.ServiceBinding;

var builder = WebApplication.CreateBuilder(args);
if (Environment.GetEnvironmentVariable("SERVICE_BINDING_ROOT") != null)
{
    builder.Configuration.AddKubernetesServiceBindings();
}
var services = builder.Services;
builder.Services.AddOptions<SqlServerBindingInfo>().Configure(c =>
{
    var config = builder.Configuration;
    var bindings = config.GetSection("k8s:bindings").GetChildren().ToList();
    var sqlServerBinding = bindings.FirstOrDefault(x => x.GetValue<string>("type") == "mssql");
    sqlServerBinding?.Bind(c);
});


services.AddAuthentication(NegotiateDefaults.AuthenticationScheme)
    .AddNegotiate();
// .AddNegotiate(c => c
//     .EnableLdap(ldap =>
//     {
// ldap.LdapConnection = new LdapConnection(new LdapDirectoryIdentifier("ad.steeltoe.io", true, false), new NetworkCredential("krbservice", Environment.GetEnvironmentVariable("AD_PASSWORD")), AuthType.Basic);
//         ldap.Domain = "STEELTOE.IO";
//         ldap.LdapConnection.SessionOptions.ReferralChasing = ReferralChasingOptions.None;
//         ldap.LdapConnection.SessionOptions.ProtocolVersion = 3; //Setting LDAP Protocol to latest version
// ldap.LdapConnection.Timeout = TimeSpan.FromMinutes(1);
//         ldap.LdapConnection.AutoBind = true;
// ldap.LdapConnection.Bind();
//     }));
                
services.AddControllers().AddJsonOptions(options =>
{
    options.JsonSerializerOptions.WriteIndented = builder.Environment.IsDevelopment();
});;;
services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "KerberosDemo", Version = "v1" });
});


var app = builder.Build();

app.UseDeveloperExceptionPage();
app.UseSwagger();
app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "KerberosDemo v1"));

app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.UseEndpoints(endpoints =>
{
    endpoints.MapControllers();
});

app.Run();
//
// namespace KerberosDemo
// {
//     public class Program
//     {
//         public static void Main(string[] args)
//         {
//             CreateHostBuilder(args).Build().Run();
//         }
//
//         public static IHostBuilder CreateHostBuilder(string[] args) =>
//             Host.CreateDefaultBuilder(args)
//                 .ConfigureWebHostDefaults(webBuilder =>
//                 {
//                     webBuilder.UseStartup<Startup>();
//                 });
//     }
// }
