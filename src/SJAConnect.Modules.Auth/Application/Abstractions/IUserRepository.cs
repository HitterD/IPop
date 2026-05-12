using SJAConnect.Modules.Auth.Domain;

namespace SJAConnect.Modules.Auth.Application.Abstractions;

public interface IUserRepository
{
    Task<User?> FindByNikAsync(string nik, CancellationToken cancellationToken);
    Task<IReadOnlyCollection<string>> GetRoleNamesAsync(Guid userId, CancellationToken cancellationToken);
    Task AddAsync(User user, CancellationToken cancellationToken);
    Task SaveChangesAsync(CancellationToken cancellationToken);
}
