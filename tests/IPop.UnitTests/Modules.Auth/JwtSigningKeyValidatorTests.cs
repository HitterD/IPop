using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using IPop.Modules.Auth.Infrastructure;
using Xunit;

namespace IPop.UnitTests.Modules.Auth;

public sealed class JwtSigningKeyValidatorTests
{
    [Fact]
    public void RequireKey_Production_MissingKey_Throws()
    {
        var config = Build(new Dictionary<string, string?>());
        var act = () => JwtSigningKeyValidator.RequireKey(config, new TestEnv("Production"));
        act.Should().Throw<InvalidOperationException>().WithMessage("*Jwt:SigningKey is not configured*");
    }

    [Fact]
    public void RequireKey_Production_DevDefault_Throws()
    {
        var config = Build(new Dictionary<string, string?> { ["Jwt:SigningKey"] = "dev-only-signing-key-change-in-production-32chars" });
        var act = () => JwtSigningKeyValidator.RequireKey(config, new TestEnv("Production"));
        act.Should().Throw<InvalidOperationException>().WithMessage("*development default*");
    }

    [Fact]
    public void RequireKey_AnyEnv_TooShort_Throws()
    {
        var config = Build(new Dictionary<string, string?> { ["Jwt:SigningKey"] = "short" });
        var act = () => JwtSigningKeyValidator.RequireKey(config, new TestEnv("Production"));
        act.Should().Throw<InvalidOperationException>().WithMessage($"*at least {JwtSigningKeyValidator.MinKeyLength}*");
    }

    [Fact]
    public void RequireKey_Production_ProperKey_Returns()
    {
        var goodKey = new string('k', JwtSigningKeyValidator.MinKeyLength);
        var config = Build(new Dictionary<string, string?> { ["Jwt:SigningKey"] = goodKey });
        var result = JwtSigningKeyValidator.RequireKey(config, new TestEnv("Production"));
        result.Should().Be(goodKey);
    }

    [Fact]
    public void RequireKey_Development_MissingKey_ReturnsDevDefault()
    {
        var config = Build(new Dictionary<string, string?>());
        var result = JwtSigningKeyValidator.RequireKey(config, new TestEnv("Development"));
        result.Should().Be("dev-only-signing-key-change-in-production-32chars");
    }

    private static IConfiguration Build(IDictionary<string, string?> data) =>
        new ConfigurationBuilder().AddInMemoryCollection(data).Build();

    private sealed class TestEnv(string name) : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = name;
        public string ApplicationName { get; set; } = "test";
        public string ContentRootPath { get; set; } = ".";
        public Microsoft.Extensions.FileProviders.IFileProvider ContentRootFileProvider { get; set; } = null!;
    }
}
