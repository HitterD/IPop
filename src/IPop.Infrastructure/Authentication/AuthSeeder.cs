using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using IPop.Infrastructure.Persistence;
using IPop.Modules.Auth.Application.Abstractions;
using IPop.Modules.Auth.Domain;

namespace IPop.Infrastructure.Authentication;

public static class AuthSeeder
{
    public static async Task SeedAdminAsync(
        AppDbContext db,
        IPasswordHasher passwordHasher,
        IConfiguration configuration,
        CancellationToken cancellationToken)
    {
        var adminNik = configuration["SeedAdmin:Nik"] ?? "devadmin";
        var adminPassword = configuration["SeedAdmin:Password"] ?? "admin123";
        var adminName = configuration["SeedAdmin:DisplayName"] ?? "Development Admin";

        var adminRole = await db.Roles.SingleOrDefaultAsync(x => x.Name == AuthConstants.AdminRole, cancellationToken);
        if (adminRole is null)
        {
            adminRole = Role.Create(AuthConstants.AdminRole);
            await db.Roles.AddAsync(adminRole, cancellationToken);
        }

        var userRole = await db.Roles.SingleOrDefaultAsync(x => x.Name == AuthConstants.UserRole, cancellationToken);
        if (userRole is null)
        {
            userRole = Role.Create(AuthConstants.UserRole);
            await db.Roles.AddAsync(userRole, cancellationToken);
        }

        var user = await db.Users.SingleOrDefaultAsync(x => x.Nik == adminNik, cancellationToken);
        if (user is null)
        {
            user = User.Create(adminNik, adminName, "IT", "Administrator", "HQ", passwordHasher.Hash(adminPassword));
            await db.Users.AddAsync(user, cancellationToken);
        }

        var hasAdminRole = await db.UserRoles.AnyAsync(x => x.UserId == user.Id && x.RoleId == adminRole.Id, cancellationToken);
        if (!hasAdminRole)
        {
            await db.UserRoles.AddAsync(UserRole.Create(user.Id, adminRole.Id), cancellationToken);
        }

        await db.SaveChangesAsync(cancellationToken);
    }
}
