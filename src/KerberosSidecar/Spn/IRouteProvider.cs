namespace KerberosSidecar.Spn;

public interface IRouteProvider
{
    public Task<IReadOnlyCollection<Uri>> GetRoutes(CancellationToken cancellationToken);
}