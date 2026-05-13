using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using SJAConnect.Infrastructure.Authentication;
using SJAConnect.Infrastructure.Persistence;
using SJAConnect.Modules.Auth.Domain;
using SJAConnect.Modules.Auth.Infrastructure;
using Xunit;

namespace SJAConnect.UnitTests.Modules.Auth;

public class AuthSeederTests
{
    [Fact]
    public async Task SeedAdminAsync_CreatesAdminUserWithAdminRole()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["SeedAdmin:Nik"] = "devadmin",
                ["SeedAdmin:Password"] = "admin123",
                ["SeedAdmin:DisplayName"] = "Development Admin"
            })
            .Build();

        await using var db = new AppDbContext(options);
        await AuthSeeder.SeedAdminAsync(db, new BCryptPasswordHasher(), configuration, default);

        var user = await db.Users.SingleAsync(x => x.Nik == "devadmin");
        var role = await db.Roles.SingleAsync(x => x.Name == AuthConstants.AdminRole);
        var userRole = await db.UserRoles.SingleAsync(x => x.UserId == user.Id && x.RoleId == role.Id);

        user.DisplayName.Should().Be("Development Admin");
        user.IsActive.Should().BeTrue();
        userRole.Should().NotBeNull();
    }

    [Fact]
    public async Task SeedAdminAsync_IsIdempotent()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var configuration = new ConfigurationBuilder().Build();

        await using var db = new AppDbContext(options);
        await AuthSeeder.SeedAdminAsync(db, new BCryptPasswordHasher(), configuration, default);
        await AuthSeeder.SeedAdminAsync(db, new BCryptPasswordHasher(), configuration, default);

        (await db.Users.CountAsync(x => x.Nik == "devadmin")).Should().Be(1);
        (await db.Roles.CountAsync(x => x.Name == AuthConstants.AdminRole)).Should().Be(1);
        (await db.UserRoles.CountAsync()).Should().Be(1);
    }
}
