using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Threading.Tasks;
using FluentAssertions;
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

namespace TapConventionWebhook.Tests;


public class ConventionServiceTests : IClassFixture<WebApplicationFactory<Program>>
{
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
    public async Task Webhook_PostJson()
    {
        var json = @"
{
    'apiVersion': 'webhooks.conventions.carto.run/v1alpha1',
    'kind': 'PodConventionContext',
    'metadata': {
        'creationTimestamp': null,
        'name': 'spring-sample-dumper'
    },
    'spec': {
        'template': {
            'metadata': {
                'creationTimestamp': null,
                'labels': {
                    'kerberos': 'true'
                }
            },
            'spec': {
                'containers': [
                    {
                        'image': 'index.docker.io/scothis/petclinic@sha256:8b517f21f283229e855e316e2753396239884eb9c4009ab6c797bdf2a041140f',
                        'name': 'workload',
                        'resources': {}
                    }
                ]
            }
        }
    },
    'status': {
        'appliedConventions': null,
        'template': {
            'metadata': {
                'creationTimestamp': null
            },
            'spec': {
                'containers': null
            }
        }
    }
}
";
        var content = new StringContent(json);
        content.Headers.ContentType = MediaTypeHeaderValue.Parse("application/json");
        var result = await _client.PostAsync("/webhook", content);
        result.StatusCode.Should().Be(HttpStatusCode.OK);
        result.IsSuccessStatusCode.Should().BeTrue();
        var response  = await result.Content.ReadFromJsonAsync<PodConventionContext>();
        response?.Kind.Should().Be("PodConventionContext");
        var settings = new JsonSerializerSettings()
        {
            Formatting = Formatting.Indented,
            NullValueHandling = NullValueHandling.Ignore
        };
        _output.WriteLine(JsonConvert.SerializeObject(response, settings ));
    }
}