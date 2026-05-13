using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace IPop.Modules.Auth.Infrastructure;

public static class JwtSigningKeyValidator
{
    public const int MinKeyLength = 32;
    private const string DevDefaultKey = "dev-only-signing-key-change-in-production-32chars";

    public static string RequireKey(IConfiguration configuration, IHostEnvironment? environment = null)
    {
        var key = configuration["Jwt:SigningKey"];
        var isDev = environment?.IsDevelopment() ?? string.Equals(
            Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT") ?? Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT"),
            "Development",
            StringComparison.OrdinalIgnoreCase);

        if (string.IsNullOrWhiteSpace(key))
        {
            if (isDev)
            {
                return DevDefaultKey;
            }
            throw new InvalidOperationException("Jwt:SigningKey is not configured. Provide it via environment variable or user secrets before starting the application.");
        }

        if (key.Length < MinKeyLength)
        {
            throw new InvalidOperationException($"Jwt:SigningKey must be at least {MinKeyLength} characters.");
        }

        if (!isDev && string.Equals(key, DevDefaultKey, StringComparison.Ordinal))
        {
            throw new InvalidOperationException("Jwt:SigningKey is using the development default value in a non-development environment. Rotate the key before deploying.");
        }

        return key;
    }
}
