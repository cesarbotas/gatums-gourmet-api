using FluentValidation;

namespace GatumsGourmet.Application.UseCases.V1.Auth.Login;

public class LoginValidator : AbstractValidator<LoginRequest>
{
    public LoginValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.Senha).NotEmpty();
    }
}
