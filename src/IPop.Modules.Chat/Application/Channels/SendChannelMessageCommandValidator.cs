using FluentValidation;

namespace IPop.Modules.Chat.Application.Channels;

public sealed class SendChannelMessageCommandValidator : AbstractValidator<SendChannelMessageCommand>
{
    public SendChannelMessageCommandValidator()
    {
        RuleFor(x => x.ChannelId).NotEmpty();
        RuleFor(x => x.SenderUserId).NotEmpty();
        RuleFor(x => x.ClientMessageId).NotEmpty();
        RuleFor(x => x.Body).NotEmpty().MaximumLength(4000);
    }
}
