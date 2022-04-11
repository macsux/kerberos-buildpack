using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using FluentAssertions;
using Kerberos.NET.Configuration;
using Kerberos.NET.Crypto;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Xunit;
using Xunit.Abstractions;

namespace KerberosBuildpack.Tests;

public class Tests
{
    private readonly ITestOutputHelper _output;

    public Tests(ITestOutputHelper output)
    {
        _output = output;
        var config = new ConfigurationBuilder()
            .AddYamlFile("appsettings.yaml", true, true)
            .AddYamlFile("appsettings.User.yaml", true, true)
            .AddEnvironmentVariables()
            .Build();
        var integrationTestUrl = config.GetValue<string>("SampleAppUrl");
        var handler = new HttpClientHandler();
        handler.ClientCertificateOptions = ClientCertificateOption.Manual;
        handler.ServerCertificateCustomValidationCallback = 
            (httpRequestMessage, cert, cetChain, policyErrors) =>
            {
                return true;
            };
        _client = new HttpClient(handler)
        {
            BaseAddress = new Uri(integrationTestUrl.TrimEnd('/') + "/")
        };
    }

    private HttpClient _client;

    [Fact]
    public async Task HasCorrectEnvVars()
    {
        var envVars =  await _client.GetFromJsonAsync<Dictionary<string,string>>("env");
        envVars.Should().Contain("KRB5_CONFIG", "/home/vcap/app/.krb5/krb5.conf");
        envVars.Should().Contain("KRB5CCNAME", "/home/vcap/app/.krb5/krb5cc");
        envVars.Should().Contain("KRB5_KTNAME", "/home/vcap/app/.krb5/service.keytab");
    }
    
    [Fact]
    public async Task Krb5ConfFileIsValid()
    {
        var confFileLocation = "/home/vcap/app/.krb5/krb5.conf";
        var fileBytes = await ReadRemoteFile(confFileLocation);
        fileBytes.Should().NotBeEmpty();
        var confFile = Encoding.Default.GetString(fileBytes);
        Krb5Config krb5Config = Krb5Config.Default();
        Action act = () => krb5Config = Krb5Config.Parse(confFile);
        act.Should().NotThrow($"Failed to parse krb5.conf\n===\n{confFile}");

        krb5Config.Realms.Should().NotBeEmpty();
        foreach (var (realm, realmConfig) in krb5Config.Realms)
        {
            realmConfig.Kdc.Should().NotBeEmpty($"Realm {realm} does not have a KDC");
        }

        krb5Config.DomainRealm.Should().NotBeEmpty();
    }

    [Fact]
    public async Task TicketCacheExists()
    {
        var ticketCacheLocation = "/home/vcap/app/.krb5/krb5cc";
        await ReadRemoteFile(ticketCacheLocation);
    }
    
    [Fact]
    public async Task KeytabValid()
    {
        // download and parse keytab locally
        var keytabLocation = "/home/vcap/app/.krb5/service.keytab";
        var keytabBytes = await ReadRemoteFile(keytabLocation);
        keytabBytes.Should().NotBeEmpty();
        var keytab = new KeyTable(keytabBytes);
        keytab.Entries.Should().NotBeEmpty();
        
        
        // try to parse keytab using ktutil on remote server
        var ktutilResults = await RunRemote("ktutil",  $"read_kt {keytabLocation}\nlist\nq");
        var hasAtLeastOneKeyEntryRegex = new Regex(@"slot KVNO Principal[\s-]*([0-9]+\s+){2}[^\s]+$", RegexOptions.Multiline);
        ktutilResults.Should().MatchRegex(hasAtLeastOneKeyEntryRegex);
    }

    [Fact]
    public async Task KlistHasTgt()
    {
        var result = await RunRemote("klist");
        result.Should().Contain("krbtgt/", $"klist does not have a valid TGT ticket\n{result}");
    }
    
    [Fact]
    public async Task SidecarHealthCheck()
    {
        var jsonResponse = await _client.GetStringAsync("sidecarhealth");
        var sidecarHealth = JsonConvert.DeserializeObject<HealthReport>(jsonResponse);
        sidecarHealth.Status.Should().Be(HealthStatus.Healthy, because: $"Sidecar is unhealthy. Deatils:\n {jsonResponse}");
    }

    [Fact]
    public async Task SqlConnectionCheck()
    {
        var response = await _client.GetAsync("sql");
        var body = await response.Content.ReadAsStringAsync();
        response.IsSuccessStatusCode.Should().BeTrue(body);
        _output.WriteLine(body);
    }
    

    private async Task<string> RunRemote(string command, string? input = null)
    {
        return await _client.GetStringAsync($"run?command={command}&input={input}");
    }
    private async Task<byte[]> ReadRemoteFile(string file)
    {
        var httpResponse = await _client.GetAsync($"getfile?file={file}");
        httpResponse.StatusCode.Should().NotBe(HttpStatusCode.NotFound, $"File {file} does not exist");
        httpResponse.EnsureSuccessStatusCode();
        var fileBytes =  await httpResponse.Content.ReadAsByteArrayAsync();
        fileBytes.Should().NotBeEmpty();
        return fileBytes;
    }

        
    
}