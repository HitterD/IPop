using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using SJAConnect.Infrastructure.Persistence;
using SJAConnect.Modules.Auth.Domain;
using Xunit;

namespace SJAConnect.UnitTests.Modules.Auth;

public class AuthModelConfigurationTests
{
    [Fact]
    public void AppDbContext_Model_ContainsAuthEntities()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>().UseInMemoryDatabase("auth-model-test").Options;
        using var db = new AppDbContext(options);
        db.Model.FindEntityType(typeof(User)).Should().NotBeNull();
        db.Model.FindEntityType(typeof(Role)).Should().NotBeNull();
        db.Model.FindEntityType(typeof(UserRole)).Should().NotBeNull();
        db.Model.FindEntityType(typeof(AuditLog)).Should().NotBeNull();
    }
}
