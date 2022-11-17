using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
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
    public async Task Webhook_PostJson()
    {
        
        var content = new StringContent(EmbeddedResources.Webhook_Post_json);
        content.Headers.ContentType = MediaTypeHeaderValue.Parse("application/json");
        var result = await _client.PostAsync("/webhook", content);
        result.StatusCode.Should().Be(HttpStatusCode.OK);
        result.IsSuccessStatusCode.Should().BeTrue();
        var response  = await result.Content.ReadFromJsonAsync<PodConventionContext>();
        response?.Kind.Should().Be("PodConventionContext");
        _output.WriteLine(JsonSerializer.Serialize(response, _serializerOptions));
    }
}