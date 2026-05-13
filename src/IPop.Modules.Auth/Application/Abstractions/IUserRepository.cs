using IPop.Modules.Auth.Domain;

namespace IPop.Modules.Auth.Application.Abstractions;

public interface IUserRepository
{
    Task<User?> FindByNikAsync(string nik, CancellationToken cancellationToken);
    Task<IReadOnlyCollection<string>> GetRoleNamesAsync(Guid userId, CancellationToken cancellationToken);
    Task AddAsync(User user, CancellationToken cancellationToken);
    Task SaveChangesAsync(CancellationToken cancellationToken);
    Task<IReadOnlyList<UserListItem>> ListAsync(CancellationToken cancellationToken);
}

public sealed record UserListItem(
    Guid Id,
    string Nik,
    string DisplayName,
    string? Department,
    string? Position,
    string? Location,
    bool IsActive,
    DateTimeOffset? LastLoginAt,
    IReadOnlyCollection<string> Roles);
