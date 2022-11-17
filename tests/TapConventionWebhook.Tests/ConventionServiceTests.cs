using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using FluentAssertions;
using k8s.Models;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using TapConventionWebhook.Controllers;
using TapConventionWebhook.Models;
using Xunit;
using Xunit.Abstractions;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace TapConventionWebhook.Tests;


public class ConventionServiceTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly JsonSerializerOptions _serializerOptions = new JsonSerializerOptions()
    {
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };
    private readonly WebApplicationFactory<Program> _factory;

    private readonly ITestOutputHelper _output;

    // private readonly TestServer _server;
    private readonly HttpClient _client;
    private readonly ITestOutputHelper output;


    public ConventionServiceTests(WebApplicationFactory<Program> factory, ITestOutputHelper output)
    {
        _factory = factory;
        _output = output;
        _client = factory.CreateClient();
    }



    // [Fact]
    // public async Task Webhook_PostAsObject()
    // {
    //     var input = new PodConventionContext()
    //     {
    //         Kind = "blah",
    //         Metadata = new()
    //         {
    //             Name = "myobj"
    //         }
    //     };
    //
    //     var result = await _client.PostAsJsonAsync("/webhook", input);
    //     result.StatusCode.Should().Be(HttpStatusCode.OK);
    //     result.IsSuccessStatusCode.Should().BeTrue();
    //     var response  = await result.Content.ReadFromJsonAsync<PodConventionContext>();
    //     response?.Kind.Should().Be("blah");
    // }
    //
    [Fact]
    public async Task Webhook_WhenKerberosLabel_ApplyConvention()
    {
        
        var content = new StringContent(EmbeddedResources.Webhook_Post_json);
        content.Headers.ContentType = MediaTypeHeaderValue.Parse("application/json");
        var result = await _client.PostAsync("/webhook", content);
        result.StatusCode.Should().Be(HttpStatusCode.OK);
        var response  = (await result.Content.ReadFromJsonAsync<PodConventionContext>())!;
        response.Should().NotBeNull();
        response.Kind.Should().Be("PodConventionContext");
        response.Status.Should().NotBeNull();
        response.Status.AppliedConventions.Should().Contain("kerberos-sidecar-convention");
        response.Status.Template.Spec.Containers.Should().HaveCount(2);
        var appContainer = response.Status.Template.Spec.Containers[0];
        appContainer.Env.Should().ContainEquivalentOf(new V1EnvVar("KRB5_CONFIG", "/krb/krb5.conf"));
        appContainer.Env.Should().ContainEquivalentOf(new V1EnvVar("KRB5_KTNAME", "/krb/service.keytab"));
        appContainer.Env.Should().ContainEquivalentOf(new V1EnvVar("KRB5_CLIENT_KTNAME", "/krb/service.keytab"));
        appContainer.VolumeMounts.Should().NotBeEmpty();
        appContainer.VolumeMounts[0].MountPath.Should().Be("/krb");
        
        var sidecarContainer = response.Status.Template.Spec.Containers[1];
        sidecarContainer.Name.Should().Be("kdc-sidecar");
        sidecarContainer.Env.Should().Contain(x => x.Name == "KRB_KDC");
        sidecarContainer.Env.Should().Contain(x => x.Name == "KRB_SERVICE_ACCOUNT");
        sidecarContainer.Env.Should().Contain(x => x.Name == "KRB_PASSWORD");
        sidecarContainer.Env.Should().Contain(x => x.Name == "KRB5_CONFIG");
        sidecarContainer.Env.Should().Contain(x => x.Name == "KRB5CCNAME");
        sidecarContainer.Env.Should().Contain(x => x.Name == "KRB5_KTNAME");
        sidecarContainer.Env.Should().Contain(x => x.Name == "KRB5_CLIENT_KTNAME");
        sidecarContainer.VolumeMounts.Should().NotBeEmpty();
        sidecarContainer.VolumeMounts[0].MountPath.Should().Be("/krb");
        _output.WriteLine(JsonSerializer.Serialize(response, _serializerOptions));
    }
}