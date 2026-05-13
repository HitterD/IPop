using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using IPop.Infrastructure.Persistence;
using IPop.Modules.Auth.Domain;
using Xunit;

namespace IPop.UnitTests.Modules.Auth;

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
