namespace IPop.Modules.Auth.Application.Abstractions;

public interface ILegacyHrRepository
{
    Task<LegacyEmployee?> FindByNikAsync(string nik, CancellationToken cancellationToken);
    Task<LegacyEmployee?> AuthenticateAsync(string nik, string md5Password, CancellationToken cancellationToken);
    IAsyncEnumerable<LegacyEmployee> StreamAllAsync(CancellationToken cancellationToken);
}
