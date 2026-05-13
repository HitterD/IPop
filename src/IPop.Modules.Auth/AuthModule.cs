using System.Security.Claims;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using IPop.Modules.Auth.Application.Abstractions;
using IPop.Modules.Auth.Application.Commands;
using IPop.Modules.Auth.Infrastructure;
using IPop.Shared.Abstractions;

namespace IPop.Modules.Auth;

public sealed class AuthModule : IModule
{
    public string Name => "Auth";
    public string Version => "0.1.0";

    public void RegisterServices(IServiceCollection services, IConfiguration configuration)
    {
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(AuthModule).Assembly));
        services.AddValidatorsFromAssembly(typeof(AuthModule).Assembly);
        services.AddScoped<IPasswordHasher, BCryptPasswordHasher>();
        services.AddScoped<IJwtTokenService, JwtTokenService>();
    }

    public void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("/api/auth/login", async (LoginRequest request, HttpContext http, IMediator mediator, CancellationToken cancellationToken) =>
        {
            var command = new LoginCommand(
                request.Nik,
                request.Password,
                http.Connection.RemoteIpAddress?.ToString(),
                http.Request.Headers.UserAgent.ToString());
            var result = await mediator.Send(command, cancellationToken);
            return result.Success ? Results.Ok(result) : Results.BadRequest(result);
        });

        endpoints.MapPost("/auth/signin", async (HttpContext http, IMediator mediator, CancellationToken cancellationToken) =>
        {
            var form = await http.Request.ReadFormAsync(cancellationToken);
            var nik = form["nik"].ToString();
            var password = form["password"].ToString();
            var returnUrl = form["returnUrl"].ToString();
            if (string.IsNullOrWhiteSpace(returnUrl) || !Uri.IsWellFormedUriString(returnUrl, UriKind.Relative))
            {
                returnUrl = "/";
            }

            var command = new LoginCommand(nik, password, http.Connection.RemoteIpAddress?.ToString(), http.Request.Headers.UserAgent.ToString());
            var result = await mediator.Send(command, cancellationToken);
            if (!result.Success || result.UserId is null)
            {
                var encoded = Uri.EscapeDataString(result.Error ?? "Login failed");
                return Results.Redirect($"/login?error={encoded}");
            }

            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, result.UserId.Value.ToString()),
                new(ClaimTypes.Name, result.DisplayName ?? result.Nik ?? nik),
                new("nik", result.Nik ?? nik)
            };
            claims.AddRange(result.Roles.Select(r => new Claim(ClaimTypes.Role, r)));

            var identity = new ClaimsIdentity(claims, AuthServiceCollectionExtensions.CookieScheme);
            var principal = new ClaimsPrincipal(identity);
            await http.SignInAsync(AuthServiceCollectionExtensions.CookieScheme, principal, new AuthenticationProperties
            {
                IsPersistent = true,
                ExpiresUtc = DateTimeOffset.UtcNow.AddHours(8)
            });

            return Results.Redirect(returnUrl);
        });

        endpoints.MapPost("/auth/signout", async (HttpContext http) =>
        {
            await http.SignOutAsync(AuthServiceCollectionExtensions.CookieScheme);
            return Results.Redirect("/login");
        });
    }

    public sealed record LoginRequest(string Nik, string Password);
}
