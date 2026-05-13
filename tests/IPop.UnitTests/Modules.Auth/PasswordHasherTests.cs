using FluentAssertions;
using IPop.Modules.Auth.Infrastructure;
using Xunit;

namespace IPop.UnitTests.Modules.Auth;

public class PasswordHasherTests
{
    [Fact]
    public void Verify_ReturnsTrue_ForCorrectPassword()
    {
        var hasher = new BCryptPasswordHasher();
        var hash = hasher.Hash("P@ssw0rd");
        hasher.Verify("P@ssw0rd", hash).Should().BeTrue();
        hasher.Verify("wrong", hash).Should().BeFalse();
    }
}
