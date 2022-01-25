namespace KerberosSidecar.Spn;

public interface ISpnClient
{
    Task<List<string>> GetAllSpn(CancellationToken cancellationToken = default);
    Task<bool> AddSpn(string spn, CancellationToken cancellationToken = default);
    Task<bool> DeleteSpn(string spn, CancellationToken cancellationToken = default);
}