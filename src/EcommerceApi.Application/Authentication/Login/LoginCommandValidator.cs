using FluentValidation;

namespace EcommerceApi.Application.Authentication.Login;

public sealed class LoginCommandValidator : AbstractValidator<LoginCommand>
{
    public LoginCommandValidator()
    {
        RuleFor(command => command.Email)
            .NotEmpty()
            .EmailAddress();
        RuleFor(command => command.Password).NotEmpty();
    }
}
