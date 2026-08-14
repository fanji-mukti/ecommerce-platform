using Xunit;

namespace ECommerce.Identity.Tests.Unit;

public class RegistrationValidatorTests
{
    private readonly RegistrationValidatorSteps _steps = new();

    [Fact]
    public void PasswordTooShort_ShouldFailValidation()
    {
        var request = _steps.Given_RegisterRequest("user@example.com", "short");
        var result = _steps.When_Validated(request);
        _steps.Then_HasError(result, "Password");
    }

    [Fact]
    public void EmptyEmail_ShouldFailValidation()
    {
        var request = _steps.Given_RegisterRequest("", "ValidPass1!");
        var result = _steps.When_Validated(request);
        _steps.Then_HasError(result, "Email");
    }

    [Fact]
    public void InvalidEmailFormat_ShouldFailValidation()
    {
        var request = _steps.Given_RegisterRequest("not-an-email", "ValidPass1!");
        var result = _steps.When_Validated(request);
        _steps.Then_HasError(result, "Email");
    }

    [Fact]
    public void ValidRequest_ShouldPassValidation()
    {
        var request = _steps.Given_RegisterRequest("user@example.com", "ValidPass1!");
        var result = _steps.When_Validated(request);
        _steps.Then_IsValid(result);
    }
}
