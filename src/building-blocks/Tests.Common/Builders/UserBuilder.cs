namespace ECommerce.Tests.Common.Builders;

/// <summary>
/// Portable user data record used in test builders.
/// </summary>
public record UserData(string Email, string Password);

/// <summary>
/// Fluent test data builder for user data.
/// </summary>
public class UserBuilder
{
    private string _email = "test@example.com";
    private string _password = "Password123!";

    public UserBuilder WithEmail(string email)
    {
        _email = email;
        return this;
    }

    public UserBuilder WithPassword(string password)
    {
        _password = password;
        return this;
    }

    public UserData Build() => new(_email, _password);
}
