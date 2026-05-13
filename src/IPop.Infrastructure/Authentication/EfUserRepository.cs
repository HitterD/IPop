using Microsoft.EntityFrameworkCore;
using IPop.Infrastructure.Persistence;
using IPop.Modules.Auth.Application.Abstractions;
using IPop.Modules.Auth.Domain;

namespace IPop.Infrastructure.Authentication;

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

    public async Task<IReadOnlyList<UserListItem>> ListAsync(CancellationToken cancellationToken)
    {
        var rows = await db.Users
            .AsNoTracking()
            .OrderBy(x => x.Nik)
            .Select(x => new
            {
                x.Id,
                x.Nik,
                x.DisplayName,
                x.Department,
                x.Position,
                x.Location,
                x.IsActive,
                x.LastLoginAt,
                Roles = x.Roles
                    .Where(r => r.Role != null)
                    .Select(r => r.Role!.Name)
                    .ToList()
            })
            .ToListAsync(cancellationToken);

        return rows
            .Select(r => new UserListItem(
                r.Id,
                r.Nik,
                r.DisplayName,
                r.Department,
                r.Position,
                r.Location,
                r.IsActive,
                r.LastLoginAt,
                r.Roles))
            .ToList();
    }
}
