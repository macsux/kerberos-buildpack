using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using File = System.IO.File;
using System.Net.Sockets;
using System.Security.Claims;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Dapper;
using KerberosDemo.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Net.Http.Headers;

namespace KerberosDemo.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class HomeController : ControllerBase
    {
        private readonly ILogger<HomeController> _logger;
        private readonly IConfiguration _configuration;
        private readonly HttpClient _sidecarClient;

        public HomeController(ILogger<HomeController> logger, IConfiguration configuration)
        {
            //SqlServerBindingInfo
            _logger = logger;
            _configuration = configuration;
            _sidecarClient =  new HttpClient()
            {
                BaseAddress = new Uri(_configuration.GetValue<string>("SidecarUrl"))
            };
        }
        //
        // [HttpGet("/binding")]
        // public ActionResult<ActiveDirectoryBindingInfo> Binding([FromServices]IOptionsSnapshot<ActiveDirectoryBindingInfo> bindingInfo)
        // {
        //     return bindingInfo.Value;
        // }

        [HttpGet("/user")]
        public ActionResult<UserDetails> AuthenticateUser(bool forceAuth)
        {
            
            var identity = (ClaimsIdentity)User.Identity!;
            if (!identity.IsAuthenticated)
            {
                if(forceAuth)
                    return Challenge();
                
                if (!Request.Headers.TryGetValue("Authorization", out var authorizationHeader))
                {
                    return base.Unauthorized("Authorization header not included. Call with '?forceAuth=true' to force SPNEGO exchange by the browser");
                }

                return Unauthorized("Not logged in.");
            }

            var user = new UserDetails()
            {
                Name = identity.Name,
                Claims = identity.Claims.Select(x => new ClaimSummary { Type = x.Type, Value = x.Value }).ToList()
            };
            return user;
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
        public ActionResult<SqlServerInfo> SqlTest([FromServices]IOptionsSnapshot<SqlServerBindingInfo> binding, string connectionString)
        {
            connectionString ??=  binding.Value.ConnectionString ?? _configuration.GetConnectionString("SqlServer");
            if (connectionString == null)
            {
                return StatusCode(500, "Connection string not set. Set 'ConnectionStrings__SqlServer' environmental variable");
            }

            var sqlClient = new SqlConnection(connectionString);
            try
            {
                var serverInfo = sqlClient.QuerySingle<SqlServerInfo>("SELECT @@servername AS Server, @@version as Version, DB_NAME() as [Database]");
                serverInfo.ConnectionString = connectionString;
                return Ok(serverInfo);
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.ToString());
            }
        }
        
        [HttpGet("/getfile")]
        public ActionResult<byte[]> ReadFile(string file)
        {
            if (!System.IO.File.Exists(file))
                return NotFound($"{file} not found");
            return File(System.IO.File.OpenRead(file), "application/octet-stream", Path.GetFileName(file));

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

        [HttpGet("/sidecarhealth")]
        public async Task<string> SidecarHealth()
        {
            var client = new HttpClient();
            var healthResult = await _sidecarClient.GetStringAsync("health/ready");
            return healthResult;
        }
        
        [HttpGet("/diag")]
        public async Task<string> Diagnostics()
        {
            var sb = new StringBuilder();
            sb.AppendLine("==== KRB5 files ====");
            VerifyEnvVar("KRB5_CONFIG");
            VerifyEnvVar("KRB5CCNAME");
            VerifyEnvVar("KRB5_KTNAME");
            
            var krb5Conf =  Environment.GetEnvironmentVariable("KRB5_CONFIG");
            if (!string.IsNullOrEmpty(krb5Conf))
            {
                sb.AppendLine($"==== {krb5Conf} content====");
                sb.AppendLine(System.IO.File.ReadAllText(krb5Conf));
            }

            sb.AppendLine("=== klist ===");
            sb.AppendLine(await Run("klist"));

            sb.AppendLine("==== KRB5_CLIENT_KTNAME keytab contents ===");
            var readKtScript = @"read_kt %KRB5_CLIENT_KTNAME%
list
q";
            sb.AppendLine(await Run("ktutil", Environment.ExpandEnvironmentVariables(readKtScript)));
            
            
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

        [HttpGet("/env")]
        public Dictionary<string,string> EnvVars()
        {
            return Environment.GetEnvironmentVariables().Cast<DictionaryEntry>().ToDictionary(x => x.Key.ToString(), x => x.Value.ToString());
        }



        
        [HttpGet("/run")]
        public async Task<string> Run(string command, string input = null)
        {
            var commandSegments = command.Split(" ");
            var processName = commandSegments[0];
            var args = commandSegments[1..];
            // Start the child process.
            try
            {


                Process p = new Process();
                // Redirect the output stream of the child process.
                p.StartInfo.UseShellExecute = false;
                
                p.StartInfo.RedirectStandardInput = true;
                p.StartInfo.RedirectStandardOutput = true;
                p.StartInfo.RedirectStandardError = true;
                p.StartInfo.FileName = processName;
                p.StartInfo.Arguments = string.Join(" ", args);
                foreach (var (key, value) in Environment.GetEnvironmentVariables().Cast<DictionaryEntry>().Select(x => ((string)x.Key, (string)x.Value)))
                {
                    p.StartInfo.EnvironmentVariables[key] = value;
                }
                p.Start();
                // Do not wait for the child process to exit before
                // reading to the end of its redirected stream.
                // p.WaitForExit();
                // Read the output stream first and then wait.
                if (input != null)
                {
                    p.StandardInput.WriteLine(Environment.ExpandEnvironmentVariables(input));
                }
                var output = p.StandardOutput.ReadToEnd();
                var error = p.StandardError.ReadToEnd();
                var cts = new CancellationTokenSource(TimeSpan.FromSeconds(15));
                await p.WaitForExitAsync(cts.Token);
                if (!p.HasExited)
                {
                    p.Kill();
                }
                return $"{output}\n{error}";
            }
            catch (Exception e)
            {
                return e.ToString();
            }
        }
        

        [HttpGet("/ticket")]
        public async Task<ActionResult<string>> GetTicket(string spn)
        {
            return await _sidecarClient.GetStringAsync($"ticket?spn={spn}");
        }
    }
}
