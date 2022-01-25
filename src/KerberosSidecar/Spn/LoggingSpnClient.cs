namespace KerberosSidecar.Spn;

public class LoggingSpnClient : ISpnClient
{
    private readonly ILogger<LoggingSpnClient> _logger;

    public LoggingSpnClient(ILogger<LoggingSpnClient> logger)
    {
        _logger = logger;
    }


    public Task<List<string>> GetAllSpn(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(new List<string>());
    }

    public Task<bool> AddSpn(string spn, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Ensure that the following SPN for the service exists: {ServicePrincipalName}", spn);
        return Task.FromResult(true);
    }

    public Task<bool> DeleteSpn(string spn, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("The following SPN should be removed: {ServicePrincipalName}", spn);
        return Task.FromResult(true);
    }
}