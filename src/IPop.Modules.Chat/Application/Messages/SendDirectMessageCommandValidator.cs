using FluentValidation;

namespace IPop.Modules.Chat.Application.Messages;

public sealed class SendDirectMessageCommandValidator : AbstractValidator<SendDirectMessageCommand>
{
    public SendDirectMessageCommandValidator()
    {
        RuleFor(x => x.SenderUserId).NotEmpty();
        RuleFor(x => x.RecipientUserId).NotEmpty();
        RuleFor(x => x.ClientMessageId).NotEmpty();
        RuleFor(x => x.Body).NotEmpty().MaximumLength(4000);
        RuleForEach(x => x.AttachmentIds).NotEmpty();
        RuleFor(x => x).Must(x => x.SenderUserId != x.RecipientUserId).WithMessage("Sender and recipient must be different users.");
    }
}
