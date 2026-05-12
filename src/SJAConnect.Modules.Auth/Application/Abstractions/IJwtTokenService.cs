using SJAConnect.Modules.Auth.Domain;

namespace SJAConnect.Modules.Auth.Application.Abstractions;

public interface IJwtTokenService
{
    string CreateAccessToken(User user, IReadOnlyCollection<string> roles);
}
