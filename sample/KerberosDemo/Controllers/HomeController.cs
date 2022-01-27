using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using File = System.IO.File;
using System.Net.Sockets;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using Dapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace KerberosDemo.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class HomeController : ControllerBase
    {
        private readonly ILogger<HomeController> _logger;
        private readonly IConfiguration _configuration;

        public HomeController(ILogger<HomeController> logger, IConfiguration configuration)
        {
            _logger = logger;
            _configuration = configuration;
        }

        [HttpGet("/user")]
        public ActionResult<string> AuthenticateUser(bool forceAuth)
        {
            
            var identity = (ClaimsIdentity)User.Identity!;
            var sb = new StringBuilder();

            if (!identity.IsAuthenticated)
            {
                if(forceAuth)
                    return Challenge();
                sb.AppendLine("Not logged in");
                if (!Request.Headers.TryGetValue("Authorization", out var authorizationHeader))
                {
                    sb.AppendLine("Authorization header not included. Call with '?forceAuth=true' to force SPNEGO exchange by the browser");
                }

                return sb.ToString();
            }

            sb.AppendLine($"User: {identity.Name}");
            sb.AppendLine("Claims: ");
            foreach (var claim in identity.Claims)
            {
                sb.AppendLine($"  {claim.Type}: {claim.Value}");
            }

            return sb.ToString();
        }
        /// <summary>
        /// Test IWA connection
        /// Things needed to get to work on Linux:
        /// 1. Package krb5-user installed (MIT Kerberos)
        /// 2. SQL Server is running under AD principal
        /// 3. SQL server principal account has SPN assigned in for of MSSQLSvc/<FQDN> where FQDN is the result of reverse DNS lookup of server address. THIS MAY BE DIFFERENT FROM SERVER HOST NAME. Use the following to obtain real FQDN
        ///     a) obtain ip from server address: nslookup <serveraddress>
        ///     b) obtain fqdn from ip: nslookup <serverip>
        /// 4. MIT kerberos configuration file is present that at minimum looks similar to this:
        /// <code>
        /// [libdefaults]
        ///         default_realm = ALMIREX.DC
        /// [realms]
        ///         ALMIREX.DC = {
        ///                 kdc = AD.ALMIREX.COM
        ///         }
        /// </code>
        /// 5. Kerberos credential cache is populated with TGT (session ticket needed to obtain authentication tickets). Use kinit to obtain TGT and populate ticket cache
        /// 6. Configure MIT kerberos environmental variables
        ///   a) KRB5_CONFIG - path to config file from step 4
        ///   b) KRB5CCNAME - path to credential cache if different from default
        /// 7. SQL Server is configured to use SSL (required by kerberos authentication)
        ///   a) If using cert that is not trusted on client, append TrustServerCertificate=True to connection string
        /// Additional valuable Kerberos issue diagnostics can be acquired by setting KRB5_TRACE=/dev/stdout env var before running this app
        /// </summary>
        /// <returns></returns>
        [HttpGet("/sql")]
        public ActionResult<string> SqlTest()
        {
            var sb = new StringBuilder();
            var connectionString = _configuration.GetConnectionString("SqlServer");
            if (connectionString == null)
            {
                return @"Connection string not set. Set 'ConnectionStrings__SqlServer' environmental variable";
            }

            sb.AppendLine($"Connection String: {connectionString}");
            var sqlClient = new SqlConnection(connectionString);
            try
            {
                var serverInfo = sqlClient.QuerySingle<SqlServerInfo>("SELECT @@servername AS Server, @@version as Version, DB_NAME() as [Database]");
                sb.AppendLine($"Successfully connected to {serverInfo.Server}");
                sb.AppendLine(serverInfo.Version);
            }
            catch (Exception ex)
            {
                sb.AppendLine("Cannot connect to SQL Server");
                sb.AppendLine(ex.ToString());
                
            }
            
            return sb.ToString();
        }

        [HttpGet("/testkdc")]
        public async Task<string> TestKDC(string kdc)
        {
            
            if (string.IsNullOrEmpty(kdc))
            {
                kdc = Environment.GetEnvironmentVariable("KRB5_KDC");
                if (string.IsNullOrEmpty(kdc))
                    return "KRB5_KDC env var is not configured";
            }
            using var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            try
            {
                await socket.ConnectAsync(kdc, 88);
                return $"Successfully connected to {kdc} on port 88";
            }
            catch (Exception e)
            {
                return $"Failed connection test to {kdc} on port 88\n{e}";
            }
        }

        [HttpGet("/diag")]
        public async Task<string> Diagnostics()
        {
            var sb = new StringBuilder();
            VerifyEnvVar("KRB5_CONFIG");
            VerifyEnvVar("KRB5CCNAME");
            VerifyEnvVar("KRB5_KTNAME");
            
            var krb5Conf =  Environment.GetEnvironmentVariable("KRB5_CONFIG");
            if (!string.IsNullOrEmpty(krb5Conf))
            {
                sb.AppendLine($"[{krb5Conf} content]");
                sb.AppendLine(System.IO.File.ReadAllText(krb5Conf));
            }
            
            void VerifyEnvVar(string var)
            {
                var varValue = Environment.GetEnvironmentVariable(var);
                sb.AppendLine($"{var}={varValue}");
                if (!string.IsNullOrEmpty(varValue))
                {
                    var fileExists = System.IO.File.Exists(varValue);
                    sb.AppendLine($"{varValue} = {(fileExists ? "exists" : "missing")}");
                }
            }

            return sb.ToString();
        }

        [HttpGet("/run")]
        private string Run(string command)
        {
            var commandSegments = command.Split(" ");
            var processName = commandSegments[0];
            var args = command[1..];
            // Start the child process.
            try
            {


                Process p = new Process();
                // Redirect the output stream of the child process.
                p.StartInfo.UseShellExecute = false;
                p.StartInfo.RedirectStandardOutput = true;
                p.StartInfo.FileName = processName;
                p.StartInfo.Arguments = string.Join(" ", args);
                p.Start();
                // Do not wait for the child process to exit before
                // reading to the end of its redirected stream.
                // p.WaitForExit();
                // Read the output stream first and then wait.
                var output = p.StandardOutput.ReadToEnd();
                var error = p.StandardError.ReadToEnd();
                p.WaitForExit();
                return $"{output}\n{error}";
            }
            catch (Exception e)
            {
                return e.ToString();
            }
        }
    }

    public class SqlServerInfo
    {
        public string Server { get; set; }
        public string Database { get; set; }
        public string Version { get; set; }
    }
}
