using IPop.Modules.Auth.Domain;

namespace IPop.Modules.Auth.Application.Abstractions;

public interface IJwtTokenService
{
    string CreateAccessToken(User user, IReadOnlyCollection<string> roles);
}
