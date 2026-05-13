using FluentAssertions;
using SJAConnect.Modules.Auth.Domain;
using Xunit;

namespace SJAConnect.UnitTests.Modules.Auth;

public class UserTests
{
    [Fact]
    public void Create_ValidUser_SetsRequiredFields()
    {
        var user = User.Create("1001", "Alice", "IT", "Developer", "HQ", "hash");
        user.Nik.Should().Be("1001");
        user.DisplayName.Should().Be("Alice");
        user.IsActive.Should().BeTrue();
        user.PasswordHash.Should().Be("hash");
    }

    [Fact]
    public void Disable_SetsInactiveAndAuditReason()
    {
        var user = User.Create("1001", "Alice", "IT", "Developer", "HQ", "hash");
        user.Disable("Admin request");
        user.IsActive.Should().BeFalse();
        user.DisabledReason.Should().Be("Admin request");
    }
}
