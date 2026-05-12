using Microsoft.EntityFrameworkCore;
using SJAConnect.Infrastructure.Persistence;
using SJAConnect.Modules.Auth.Application.Abstractions;
using SJAConnect.Modules.Auth.Domain;

namespace SJAConnect.Infrastructure.Authentication;

public sealed class EfUserRepository(AppDbContext db) : IUserRepository
{
    public Task<User?> FindByNikAsync(string nik, CancellationToken cancellationToken)
    {
        return db.Users.Include(x => x.Roles).ThenInclude(x => x.Role)
            .FirstOrDefaultAsync(x => x.Nik == nik, cancellationToken);
    }

    public async Task<IReadOnlyCollection<string>> GetRoleNamesAsync(Guid userId, CancellationToken cancellationToken)
    {
        return await db.UserRoles
            .Where(x => x.UserId == userId && x.Role != null)
            .Select(x => x.Role!.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(User user, CancellationToken cancellationToken)
    {
        await db.Users.AddAsync(user, cancellationToken);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        return db.SaveChangesAsync(cancellationToken);
    }
}
