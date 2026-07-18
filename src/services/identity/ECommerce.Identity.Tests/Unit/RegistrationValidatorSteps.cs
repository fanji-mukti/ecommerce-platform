using ECommerce.Identity.API.Features.Registration;
using FluentAssertions;
using FluentValidation.Results;

namespace ECommerce.Identity.Tests.Unit;

public class RegistrationValidatorSteps
{
    private readonly RegisterValidator _validator = new();

    public RegisterRequest Given_RegisterRequest(string email, string password)
        => new(email, password);

    public ValidationResult When_Validated(RegisterRequest request)
        => _validator.Validate(request);

    public void Then_HasError(ValidationResult result, string propertyName)
    {
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == propertyName);
    }

    public void Then_IsValid(ValidationResult result)
        => result.IsValid.Should().BeTrue();
}
