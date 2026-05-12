namespace SJAConnect.Infrastructure.Legacy;

public interface ILegacyHrRepository
{
    Task<LegacyEmployee?> FindByNikAsync(string nik, CancellationToken ct);
    Task<LegacyEmployee?> AuthenticateAsync(string nik, string md5Password, CancellationToken ct);
    IAsyncEnumerable<LegacyEmployee> StreamAllAsync(CancellationToken ct);
}
