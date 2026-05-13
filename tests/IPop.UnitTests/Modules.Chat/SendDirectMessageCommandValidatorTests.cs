using FluentAssertions;
using IPop.Modules.Chat.Application.Messages;
using Xunit;

namespace IPop.UnitTests.Modules.Chat;

public sealed class SendDirectMessageCommandValidatorTests
{
    [Fact]
    public void Validate_ValidCommand_Passes()
    {
        var validator = new SendDirectMessageCommandValidator();
        var command = new SendDirectMessageCommand(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "hello", Array.Empty<Guid>());
        validator.Validate(command).IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_MissingRecipient_Fails()
    {
        var validator = new SendDirectMessageCommandValidator();
        var command = new SendDirectMessageCommand(Guid.NewGuid(), Guid.Empty, Guid.NewGuid(), "hello", Array.Empty<Guid>());
        validator.Validate(command).IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validate_BlankBody_Fails()
    {
        var validator = new SendDirectMessageCommandValidator();
        var command = new SendDirectMessageCommand(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), " ", Array.Empty<Guid>());
        validator.Validate(command).IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validate_TooLongBody_Fails()
    {
        var validator = new SendDirectMessageCommandValidator();
        var command = new SendDirectMessageCommand(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), new string('x', 4001), Array.Empty<Guid>());
        validator.Validate(command).IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validate_SelfSend_Fails()
    {
        var userId = Guid.NewGuid();
        var validator = new SendDirectMessageCommandValidator();
        var command = new SendDirectMessageCommand(userId, userId, Guid.NewGuid(), "hello", Array.Empty<Guid>());
        validator.Validate(command).IsValid.Should().BeFalse();
    }
}
