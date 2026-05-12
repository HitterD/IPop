using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SJAConnect.Shared.Abstractions;

namespace SJAConnect.Modules.Sample;

public sealed class SampleModule : IModule
{
    public string Name => "Sample";
    public string Version => "0.0.1";

    public void RegisterServices(IServiceCollection services, IConfiguration configuration)
    {
    }

    public void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/api/sample/ping", () => Results.Ok(new { module = "Sample", ok = true }));
    }
}
