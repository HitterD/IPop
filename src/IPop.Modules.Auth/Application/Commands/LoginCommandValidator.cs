using FluentValidation;

namespace IPop.Modules.Auth.Application.Commands;

public sealed class LoginCommandValidator : AbstractValidator<LoginCommand>
{
    public LoginCommandValidator()
    {
        RuleFor(x => x.Nik).NotEmpty().MaximumLength(32);
        RuleFor(x => x.Password).NotEmpty().MaximumLength(256);
    }
}
