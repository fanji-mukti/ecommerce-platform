using ECommerce.Tests.Common;
using Xunit;

namespace ECommerce.Identity.Tests.Integration;

public class RegisterEndpointTests(PostgresFixture fixture) : IClassFixture<PostgresFixture>
{
    private readonly RegisterEndpointSteps _steps = new(fixture);

    [Fact]
    public async Task PostRegister_WithValidRequest_Returns201()
    {
        var response = await _steps.When_PostRegisterIsCalled("newuser@example.com", "StrongPass1!");
        await _steps.Then_ResponseIs(response, System.Net.HttpStatusCode.Created);
    }

    [Fact]
    public async Task PostRegister_WithDuplicateEmail_Returns409()
    {
        await _steps.Given_UserAlreadyExists("duplicate@example.com", "StrongPass1!");
        var response = await _steps.When_PostRegisterIsCalled("duplicate@example.com", "StrongPass1!");
        await _steps.Then_ResponseIs(response, System.Net.HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task PostRegister_WithShortPassword_Returns400()
    {
        var response = await _steps.When_PostRegisterIsCalled("shortpwd@example.com", "short");
        await _steps.Then_ResponseIs(response, System.Net.HttpStatusCode.BadRequest);
    }
}
