using FluentAssertions;
using SJAConnect.Shared.Abstractions;
using Xunit;

namespace SJAConnect.UnitTests.Shared;

public class ResultTests
{
    [Fact]
    public void Ok_CreatesSuccessResult()
    {
        var r = Result.Ok();
        r.IsSuccess.Should().BeTrue();
        r.Error.Should().Be(Error.None);
    }

    [Fact]
    public void Fail_CreatesFailureResult()
    {
        var err = new Error("x", "bad");
        var r = Result.Fail(err);
        r.IsFailure.Should().BeTrue();
        r.Error.Should().Be(err);
    }

    [Fact]
    public void ResultT_Ok_ExposesValue()
    {
        var r = Result<int>.Ok(42);
        r.IsSuccess.Should().BeTrue();
        r.Value.Should().Be(42);
    }

    [Fact]
    public void ResultT_Fail_ThrowsOnValueAccess()
    {
        var r = Result<int>.Fail(new Error("x", "bad"));
        var act = () => r.Value;
        act.Should().Throw<InvalidOperationException>();
    }
}
