using System;
using System.Collections.Generic;
using System.DirectoryServices.Protocols;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication.Negotiate;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Models;

namespace KerberosDemo
{
    public class Startup
    {
        private readonly IHostEnvironment _environment;

        public Startup(IConfiguration configuration, IHostEnvironment environment)
        {
            _environment = environment;
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddAuthentication(NegotiateDefaults.AuthenticationScheme)
                .AddNegotiate();
                // .AddNegotiate(c => c
                //     .EnableLdap(ldap =>
                //     {
                        // ldap.LdapConnection = new LdapConnection(new LdapDirectoryIdentifier(ldapAddress, true, false), new NetworkCredential(serviceAccount, password), AuthType.Basic);
                //         ldap.Domain = domain;
                //         ldap.LdapConnection.SessionOptions.ReferralChasing = ReferralChasingOptions.None;
                //         ldap.LdapConnection.SessionOptions.ProtocolVersion = 3; //Setting LDAP Protocol to latest version
                        // ldap.LdapConnection.Timeout = TimeSpan.FromMinutes(1);
                //         ldap.LdapConnection.AutoBind = true;
                        // ldap.LdapConnection.Bind();
                //     }));
            services.AddControllers().AddJsonOptions(options =>
            {
                options.JsonSerializerOptions.WriteIndented = _environment.IsDevelopment();
            });;;
            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo { Title = "KerberosDemo", Version = "v1" });
            });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
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
        }
    }
}
