#pragma warning disable IL2026
using System.Net;

namespace KerberosSidecar.Spn;

public class RestSpnClient : ISpnClient
{
    private readonly HttpClient _client;
    private readonly ILogger<RestSpnClient> _logger;

    public RestSpnClient(HttpClient client, ILogger<RestSpnClient> logger)
    {
        _client = client;
        _logger = logger;
        _client.BaseAddress = new Uri(_client.BaseAddress!, "spn/");
    }

    public async Task<List<string>> GetAllSpn(CancellationToken cancellationToken = default)
    {
        return (await _client.GetFromJsonAsync<List<string>>("", cancellationToken))!;
    }

    public async Task<bool> AddSpn(string spn, CancellationToken cancellationToken = default)
    {
        var responseMessage = await _client.PostAsync(spn, null, cancellationToken);
        responseMessage.EnsureSuccessStatusCode();
        _logger.LogInformation("SPN {Spn} created", spn);
        return responseMessage.StatusCode == HttpStatusCode.Created;
    }

    public async Task<bool> DeleteSpn(string spn, CancellationToken cancellationToken = default)
    {
        var responseMessage = await _client.DeleteAsync(spn, cancellationToken);
        responseMessage.EnsureSuccessStatusCode();
        _logger.LogInformation("SPN {Spn} deleted", spn);
        return true;
    }
}