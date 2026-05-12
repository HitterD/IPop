using System.Runtime.CompilerServices;
using FluentAssertions;
using SJAConnect.Modules.Auth.Application.Abstractions;
using SJAConnect.Modules.Auth.Application.Commands;
using SJAConnect.Modules.Auth.Domain;
using Xunit;

namespace SJAConnect.UnitTests.Modules.Auth;

public class LoginCommandHandlerTests
{
    [Fact]
    public async Task Handle_LocalUserWithCorrectPassword_ReturnsToken()
    {
        var user = User.Create("1001", "Alice", "IT", "Developer", "HQ", "hash");
        var users = new FakeUserRepository(user, new[] { AuthConstants.UserRole });
        var handler = new LoginCommandHandler(users, new FakeLegacyHrRepository(null), new FakePasswordHasher(true), new FakeJwtTokenService(), new FakeAuditWriter());

        var result = await handler.Handle(new LoginCommand("1001", "secret", null, null), default);

        result.Success.Should().BeTrue();
        result.AccessToken.Should().Be("token-1001");
        user.LastLoginAt.Should().NotBeNull();
    }

    [Fact]
    public async Task Handle_MissingLocalUserWithValidLegacy_CreatesUserAndReturnsToken()
    {
        var legacy = new LegacyEmployee("1002", "Bob", "IT", "Developer", "HQ", null, "md5");
        var users = new FakeUserRepository(null, Array.Empty<string>());
        var handler = new LoginCommandHandler(users, new FakeLegacyHrRepository(legacy), new FakePasswordHasher(false), new FakeJwtTokenService(), new FakeAuditWriter());

        var result = await handler.Handle(new LoginCommand("1002", "secret", null, null), default);

        result.Success.Should().BeTrue();
        result.Nik.Should().Be("1002");
        users.AddedUser.Should().NotBeNull();
    }

    private sealed class FakeUserRepository(User? user, IReadOnlyCollection<string> roles) : IUserRepository
    {
        public User? AddedUser { get; private set; }
        public Task<User?> FindByNikAsync(string nik, CancellationToken cancellationToken) => Task.FromResult(user);
        public Task<IReadOnlyCollection<string>> GetRoleNamesAsync(Guid userId, CancellationToken cancellationToken) => Task.FromResult(roles);
        public Task AddAsync(User userToAdd, CancellationToken cancellationToken) { AddedUser = userToAdd; user = userToAdd; return Task.CompletedTask; }
        public Task SaveChangesAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }

    private sealed class FakeLegacyHrRepository(LegacyEmployee? employee) : ILegacyHrRepository
    {
        public Task<LegacyEmployee?> FindByNikAsync(string nik, CancellationToken cancellationToken) => Task.FromResult(employee);
        public Task<LegacyEmployee?> AuthenticateAsync(string nik, string md5Password, CancellationToken cancellationToken) => Task.FromResult(employee);
        public async IAsyncEnumerable<LegacyEmployee> StreamAllAsync([EnumeratorCancellation] CancellationToken cancellationToken)
        {
            if (employee is not null) yield return employee;
            await Task.CompletedTask;
        }
    }

    private sealed class FakePasswordHasher(bool verifyResult) : IPasswordHasher
    {
        public string Hash(string password) => "hash-" + password;
        public bool Verify(string password, string hash) => verifyResult;
    }

    private sealed class FakeJwtTokenService : IJwtTokenService
    {
        public string CreateAccessToken(User user, IReadOnlyCollection<string> roles) => "token-" + user.Nik;
    }

    private sealed class FakeAuditWriter : IAuditWriter
    {
        public Task WriteAsync(string eventType, Guid actorUserId, string? actorNik, Guid? targetUserId, string? targetResource, string? payload, string? ipAddress, string? userAgent, CancellationToken cancellationToken) => Task.CompletedTask;
    }
}
