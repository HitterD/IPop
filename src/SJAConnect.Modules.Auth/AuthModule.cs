using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SJAConnect.Modules.Auth.Application.Abstractions;
using SJAConnect.Modules.Auth.Application.Commands;
using SJAConnect.Modules.Auth.Infrastructure;
using SJAConnect.Shared.Abstractions;

namespace SJAConnect.Modules.Auth;

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
    }

    public sealed record LoginRequest(string Nik, string Password);
}
