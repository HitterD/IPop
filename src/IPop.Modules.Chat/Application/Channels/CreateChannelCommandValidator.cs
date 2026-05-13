using FluentValidation;
using IPop.Modules.Chat.Domain;

namespace IPop.Modules.Chat.Application.Channels;

public sealed class CreateChannelCommandValidator : AbstractValidator<CreateChannelCommand>
{
    public CreateChannelCommandValidator()
    {
        RuleFor(x => x.CreatorUserId).NotEmpty();
        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(Channel.NameMaxLength);
        RuleFor(x => x.Description)
            .MaximumLength(Channel.DescriptionMaxLength)
            .When(x => !string.IsNullOrWhiteSpace(x.Description));
        RuleFor(x => x.Type).IsInEnum();
    }
}
