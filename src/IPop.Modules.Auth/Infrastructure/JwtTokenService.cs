using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using IPop.Modules.Auth.Application.Abstractions;
using IPop.Modules.Auth.Domain;

namespace IPop.Modules.Auth.Infrastructure;

public sealed class JwtTokenService(IConfiguration configuration) : IJwtTokenService
{
    public string CreateAccessToken(User user, IReadOnlyCollection<string> roles)
    {
        var issuer = configuration["Jwt:Issuer"] ?? "IPop";
        var audience = configuration["Jwt:Audience"] ?? "IPop-users";
        var key = JwtSigningKeyValidator.RequireKey(configuration);
        var minutes = int.TryParse(configuration["Jwt:AccessTokenMinutes"], out var v) ? v : 15;

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new("nik", user.Nik),
            new(ClaimTypes.Name, user.DisplayName)
        };
        claims.AddRange(roles.Select(r => new Claim(ClaimTypes.Role, r)));

        var token = new JwtSecurityToken(
            issuer,
            audience,
            claims,
            expires: DateTime.UtcNow.AddMinutes(minutes),
            signingCredentials: new SigningCredentials(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)), SecurityAlgorithms.HmacSha256));

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
