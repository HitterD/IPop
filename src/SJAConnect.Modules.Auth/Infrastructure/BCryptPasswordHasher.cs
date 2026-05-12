using SJAConnect.Modules.Auth.Application.Abstractions;

namespace SJAConnect.Modules.Auth.Infrastructure;

public sealed class BCryptPasswordHasher : IPasswordHasher
{
    public string Hash(string password) => BCrypt.Net.BCrypt.HashPassword(password, workFactor: 12);
    public bool Verify(string password, string hash) => BCrypt.Net.BCrypt.Verify(password, hash);
}
