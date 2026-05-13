using System.Security.Cryptography;
using System.Text;
using MediatR;
using IPop.Modules.Auth.Application.Abstractions;
using IPop.Modules.Auth.Domain;

namespace IPop.Modules.Auth.Application.Commands;

public sealed class LoginCommandHandler(
    IUserRepository users,
    ILegacyHrRepository legacyHr,
    IPasswordHasher passwordHasher,
    IJwtTokenService jwtTokenService,
    IAuditWriter auditWriter) : IRequestHandler<LoginCommand, LoginResult>
{
    private static readonly Guid SystemActorId = Guid.Empty;

    public async Task<LoginResult> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        var nik = request.Nik.Trim();
        var user = await users.FindByNikAsync(nik, cancellationToken);

        if (user is not null && user.IsActive && passwordHasher.Verify(request.Password, user.PasswordHash))
        {
            user.RecordLogin(DateTimeOffset.UtcNow);
            await users.SaveChangesAsync(cancellationToken);
            await auditWriter.WriteAsync("auth.login.success", user.Id, user.Nik, user.Id, "user", null, request.IpAddress, request.UserAgent, cancellationToken);
            return await SuccessAsync(user, cancellationToken);
        }

        var md5 = ComputeMd5(request.Password);
        var legacy = await legacyHr.AuthenticateAsync(nik, md5, cancellationToken);
        if (legacy is null)
        {
            await auditWriter.WriteAsync("auth.login.failed", user?.Id ?? SystemActorId, nik, user?.Id, "user", null, request.IpAddress, request.UserAgent, cancellationToken);
            return new LoginResult(false, null, null, nik, null, Array.Empty<string>(), "Invalid NIK or password");
        }

        if (user is null)
        {
            user = User.Create(
                legacy.NikHris,
                legacy.Name,
                legacy.Department,
                legacy.Position,
                legacy.Location,
                passwordHasher.Hash(request.Password));
            await users.AddAsync(user, cancellationToken);
        }
        else
        {
            if (!user.IsActive)
            {
                return new LoginResult(false, null, null, nik, null, Array.Empty<string>(), "User is disabled");
            }

            user.SetPasswordHash(passwordHasher.Hash(request.Password));
        }

        user.RecordLogin(DateTimeOffset.UtcNow);
        await users.SaveChangesAsync(cancellationToken);
        await auditWriter.WriteAsync("auth.login.success", user.Id, user.Nik, user.Id, "user", "{\"source\":\"sjatr\"}", request.IpAddress, request.UserAgent, cancellationToken);
        return await SuccessAsync(user, cancellationToken);
    }

    private async Task<LoginResult> SuccessAsync(User user, CancellationToken cancellationToken)
    {
        var roles = await users.GetRoleNamesAsync(user.Id, cancellationToken);
        if (roles.Count == 0)
        {
            roles = new[] { AuthConstants.UserRole };
        }

        var token = jwtTokenService.CreateAccessToken(user, roles);
        return new LoginResult(true, token, user.Id, user.Nik, user.DisplayName, roles, null);
    }

    private static string ComputeMd5(string input)
    {
        var bytes = MD5.HashData(Encoding.UTF8.GetBytes(input));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }
}
