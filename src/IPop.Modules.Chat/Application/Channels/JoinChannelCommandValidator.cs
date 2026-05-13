using FluentValidation;

namespace IPop.Modules.Chat.Application.Channels;

public sealed class JoinChannelCommandValidator : AbstractValidator<JoinChannelCommand>
{
    public JoinChannelCommandValidator()
    {
        RuleFor(x => x.ChannelId).NotEmpty();
        RuleFor(x => x.UserId).NotEmpty();
    }
}
