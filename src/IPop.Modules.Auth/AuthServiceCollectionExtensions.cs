using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using SJAConnect.Shared.Auth;

namespace SJAConnect.Modules.Auth;

public static class AuthServiceCollectionExtensions
{
    public static IServiceCollection AddSJAConnectAuth(this IServiceCollection services, IConfiguration configuration)
    {
        var issuer = configuration["Jwt:Issuer"] ?? "sjaconnect";
        var audience = configuration["Jwt:Audience"] ?? "sjaconnect-users";
        var signingKey = configuration["Jwt:SigningKey"] ?? "dev-only-signing-key-change-in-production-32chars";

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.RequireHttpsMetadata = false;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = issuer,
                    ValidAudience = audience,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(signingKey))
                };
            });

        services.AddAuthorization(opt =>
        {
            opt.AddPolicy(AuthorizationPolicies.RequireAuthenticated, p => p.RequireAuthenticatedUser());
            opt.AddPolicy(AuthorizationPolicies.RequireAdmin, p => p.RequireAuthenticatedUser().RequireRole(AuthorizationPolicies.AdminRoleName));
        });

        return services;
    }
}
