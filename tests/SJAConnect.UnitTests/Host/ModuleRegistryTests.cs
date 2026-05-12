using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SJAConnect.Host;
using SJAConnect.Shared.Abstractions;
using Xunit;

namespace SJAConnect.UnitTests.Host;

public class ModuleRegistryTests
{
    [Fact]
    public void Discover_FindsAllIModuleImplementations()
    {
        var modules = ModuleRegistry.Discover(new[] { typeof(FakeModule).Assembly });
        modules.Should().ContainSingle(m => m.Name == "Fake");
    }
}

public sealed class FakeModule : IModule
{
    public string Name => "Fake";
    public string Version => "1.0.0";
    public void RegisterServices(IServiceCollection services, IConfiguration configuration) { }
    public void MapEndpoints(Microsoft.AspNetCore.Routing.IEndpointRouteBuilder endpoints) { }
}
