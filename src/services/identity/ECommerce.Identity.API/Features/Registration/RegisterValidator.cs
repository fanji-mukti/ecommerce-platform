using FluentValidation;

namespace ECommerce.Identity.API.Features.Registration;

public class RegisterValidator : AbstractValidator<RegisterRequest>
{
    public RegisterValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.Password).NotEmpty().MinimumLength(8); // ASVS V2: 8+ char minimum
    }
}
