using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace SJAConnect.IntegrationTests;

public class SmokeTest : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public SmokeTest(WebApplicationFactory<Program> factory) => _factory = factory;

    [Fact]
    public async Task RootEndpoint_Returns200()
    {
        var client = _factory.CreateClient();
        var res = await client.GetAsync("/");
        res.IsSuccessStatusCode.Should().BeTrue();
        var body = await res.Content.ReadAsStringAsync();
        body.Should().Contain("SJAConnect");
    }

    [Fact]
    public async Task HealthLive_Returns200()
    {
        var client = _factory.CreateClient();
        var res = await client.GetAsync("/health/live");
        res.IsSuccessStatusCode.Should().BeTrue();
    }
}
