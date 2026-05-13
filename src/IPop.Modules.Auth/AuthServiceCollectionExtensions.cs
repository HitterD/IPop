using System.Text;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;
using IPop.Modules.Auth.Infrastructure;
using IPop.Shared.Auth;

namespace IPop.Modules.Auth;

public static class AuthServiceCollectionExtensions
{
    public const string CookieScheme = "IPopCookie";

    public static IServiceCollection AddIPopAuth(this IServiceCollection services, IConfiguration configuration, IHostEnvironment environment)
    {
        var issuer = configuration["Jwt:Issuer"] ?? "IPop";
        var audience = configuration["Jwt:Audience"] ?? "IPop-users";
        var signingKey = JwtSigningKeyValidator.RequireKey(configuration, environment);

        services.AddAuthentication(options =>
            {
                options.DefaultScheme = CookieScheme;
                options.DefaultChallengeScheme = CookieScheme;
            })
            .AddCookie(CookieScheme, options =>
            {
                options.Cookie.Name = "ipop.auth";
                options.Cookie.HttpOnly = true;
                options.Cookie.SameSite = SameSiteMode.Lax;
                options.LoginPath = "/login";
                options.LogoutPath = "/auth/signout";
                options.AccessDeniedPath = "/login";
                options.ExpireTimeSpan = TimeSpan.FromHours(8);
                options.SlidingExpiration = true;
            })
            .AddJwtBearer(options =>
            {
                options.RequireHttpsMetadata = !environment.IsDevelopment();
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

        services.AddCascadingAuthenticationState();

        return services;
    }
}
