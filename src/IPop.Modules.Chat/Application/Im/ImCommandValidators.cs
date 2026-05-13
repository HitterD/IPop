using FluentValidation;
using IPop.Modules.Chat.Domain;

namespace IPop.Modules.Chat.Application.Im;

public sealed class SendImCommandValidator : AbstractValidator<SendImCommand>
{
    public SendImCommandValidator()
    {
        RuleFor(x => x.SenderUserId).NotEmpty();
        RuleFor(x => x.RecipientUserIds).NotEmpty().WithMessage("At least one recipient is required.");
        RuleForEach(x => x.RecipientUserIds).NotEmpty().WithMessage("Recipient user id cannot be empty.");
        RuleFor(x => x.Subject).NotEmpty().MaximumLength(ImMessage.SubjectMaxLength);
        RuleFor(x => x.Body).NotEmpty().MaximumLength(ImMessage.BodyMaxLength);
        RuleFor(x => x.MessageType).IsInEnum();
    }
}

public sealed class MarkImReadCommandValidator : AbstractValidator<MarkImReadCommand>
{
    public MarkImReadCommandValidator()
    {
        RuleFor(x => x.CurrentUserId).NotEmpty();
        RuleFor(x => x.MessageId).NotEmpty();
    }
}

public sealed class DeleteImCommandValidator : AbstractValidator<DeleteImCommand>
{
    public DeleteImCommandValidator()
    {
        RuleFor(x => x.CurrentUserId).NotEmpty();
        RuleFor(x => x.MessageIds).NotEmpty();
        RuleForEach(x => x.MessageIds).NotEmpty();
    }
}
