using System.Net;
using System.Net.Http.Json;
using ECommerce.Tests.Common;
using FluentAssertions;

namespace ECommerce.Identity.Tests.Integration;

public class RegisterEndpointSteps(PostgresFixture fixture)
{
    // Lazy init so each test class gets its own factory (per D-28 per-class isolation)
    private readonly Lazy<HttpClient> _clientLazy = new(() =>
        new ServiceWebApplicationFactory<Program>(fixture.ConnectionString).CreateClient());

    private HttpClient Client => _clientLazy.Value;

    public async Task Given_UserAlreadyExists(string email, string password)
        => await When_PostRegisterIsCalled(email, password);

    public async Task<HttpResponseMessage> When_PostRegisterIsCalled(string email, string password)
    {
        var request = new { Email = email, Password = password };
        return await Client.PostAsJsonAsync("/register", request);
    }

    public async Task Then_ResponseIs(HttpResponseMessage response, HttpStatusCode expectedStatus)
        => response.StatusCode.Should().Be(expectedStatus);
}
