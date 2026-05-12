using FluentAssertions;
using FluentValidation;
using MediatR;
using SJAConnect.Shared.Abstractions;
using Xunit;

namespace SJAConnect.UnitTests.Shared;

public class ValidationBehaviorTests
{
    public sealed record Cmd(string Name) : IRequest<string>;

    public sealed class CmdValidator : AbstractValidator<Cmd>
    {
        public CmdValidator() => RuleFor(c => c.Name).NotEmpty();
    }

    [Fact]
    public async Task Valid_PassesThrough()
    {
        var behavior = new ValidationBehavior<Cmd, string>(new[] { new CmdValidator() });
        var result = await behavior.Handle(new Cmd("ok"), () => Task.FromResult("done"), default);
        result.Should().Be("done");
    }

    [Fact]
    public async Task Invalid_ThrowsValidationException()
    {
        var behavior = new ValidationBehavior<Cmd, string>(new[] { new CmdValidator() });
        Func<Task> act = () => behavior.Handle(new Cmd(""), () => Task.FromResult("done"), default);
        await act.Should().ThrowAsync<ValidationException>();
    }
}
