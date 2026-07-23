using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ECommerce.Tests.Common;

/// <summary>
/// Shared fake JWT auth scheme for integration tests. Every service's *WebApplicationFactory
/// replaces its real JwtBearer scheme with this one, so tests can authenticate as an
/// arbitrary user by setting the <see cref="TestUserIdHeader"/> header — no real token issuance
/// or Identity round-trip required.
/// </summary>
public class TestAuthHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder)
    : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    public const string SchemeName = "TestAuth";
    public const string TestUserIdHeader = "X-Test-User-Id";

    private static readonly string DefaultUserId = new Guid("11111111-1111-1111-1111-111111111111").ToString();

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var userId = Request.Headers.TryGetValue(TestUserIdHeader, out var header) && !string.IsNullOrWhiteSpace(header)
            ? header.ToString()
            : DefaultUserId;

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId),
            new Claim("sub", userId)
        };

        var identity = new ClaimsIdentity(claims, SchemeName);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, SchemeName);

        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}
