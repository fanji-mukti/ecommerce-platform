using System.Net;
using System.Net.Http.Json;
using ECommerce.Notifications.API.Data;
using ECommerce.Tests.Common;
using FluentAssertions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace ECommerce.Notifications.Tests.Integration;

/// <summary>
/// WebApplicationFactory for Notifications integration tests. Swaps the postgres connection
/// string, forces MassTransit onto the in-memory transport (Program.cs's "placeholder"
/// sentinel), and replaces the real JwtBearer scheme with the shared TestAuthHandler.
/// Notifications has no typed outbound HTTP client to redirect (unlike Orders' Cart client),
/// so this factory is simpler than OrdersWebApplicationFactory.
/// </summary>
internal sealed class NotificationsWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly string _postgresConnectionString;

    public NotificationsWebApplicationFactory(string postgresConnectionString)
    {
        _postgresConnectionString = postgresConnectionString;

        // Aspire's AddNpgsqlDbContext reads ConnectionStrings:postgres EAGERLY, before
        // WebApplicationFactory's ConfigureAppConfiguration override can reach it. Setting the
        // environment variable here, before the host is constructed, ensures
        // WebApplication.CreateBuilder's built-in AddEnvironmentVariables() source has the test
        // connection string in place before Program.cs's own code runs. The same eagerness
        // applies to the "messaging" connection string read inside the AddMassTransit(...) call.
        Environment.SetEnvironmentVariable("ConnectionStrings__postgres", postgresConnectionString);
        Environment.SetEnvironmentVariable("ConnectionStrings__messaging", "placeholder");
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        var postgresConnectionString = _postgresConnectionString;

        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:postgres"] = postgresConnectionString,
                // Provide a placeholder so MassTransit's in-memory-transport branch activates.
                ["ConnectionStrings:messaging"] = "placeholder"
            });
        });

        builder.ConfigureServices(services =>
        {
            // Remove DbInitializer so it does not race with test-driven migrations/seeding.
            services.RemoveAll<DbInitializer>();

            services.AddAuthentication(TestAuthHandler.SchemeName)
                .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(TestAuthHandler.SchemeName, _ => { });
        });
    }
}

/// <summary>
/// GET /notifications returns raw NotificationEntry rows (not a DTO) — this record mirrors the
/// camelCase JSON shape System.Text.Json's default naming policy produces for that entity.
/// </summary>
public record NotificationEntryDto(Guid Id, string UserId, Guid OrderId, string Message, string EventType, DateTimeOffset OccurredAt);

public class NotificationsEndpointSteps : IDisposable
{
    private readonly string _postgresConnectionString;
    private readonly NotificationsWebApplicationFactory _factory;
    private HttpClient? _client;

    public NotificationsEndpointSteps(PostgresFixture fixture)
    {
        _postgresConnectionString = fixture.ConnectionString;
        _factory = new NotificationsWebApplicationFactory(fixture.ConnectionString);
    }

    private HttpClient Client => _client ??= _factory.CreateClient();

    private async Task<NotificationsDbContext> CreateDbContextAsync()
    {
        var options = new DbContextOptionsBuilder<NotificationsDbContext>()
            .UseNpgsql(_postgresConnectionString)
            .Options;
        var db = new NotificationsDbContext(options);
        await db.Database.MigrateAsync();
        return db;
    }

    // ----- Given -----

    public async Task Given_NotificationExistsForUser(string userId, string eventType, string message, Guid orderId, DateTimeOffset occurredAt)
    {
        await using var db = await CreateDbContextAsync();
        db.NotificationEntries.Add(new API.Features.Notifications.NotificationEntry
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            OrderId = orderId,
            Message = message,
            EventType = eventType,
            OccurredAt = occurredAt
        });
        await db.SaveChangesAsync();
    }

    // ----- When -----

    public async Task<HttpResponseMessage> When_GetNotificationsIsCalled(string userId)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "/notifications");
        request.Headers.Add(TestAuthHandler.TestUserIdHeader, userId);
        return await Client.SendAsync(request);
    }

    // ----- Then -----

    public void Then_ResponseIs200(HttpResponseMessage response)
        => response.StatusCode.Should().Be(HttpStatusCode.OK);

    public async Task<List<NotificationEntryDto>> Then_ResponseContainsExactlyEntriesFor(HttpResponseMessage response, string expectedUserId, int expectedCount)
    {
        var body = await response.Content.ReadFromJsonAsync<List<NotificationEntryDto>>();
        body.Should().NotBeNull();
        body!.Should().HaveCount(expectedCount);
        body.Should().OnlyContain(n => n.UserId == expectedUserId,
            "a different X-Test-User-Id must never see another user's notification entries (IDOR-safe)");
        return body;
    }

    public void Then_EntriesAreOrderedByOccurredAtDescending(List<NotificationEntryDto> body)
        => body.Should().BeInDescendingOrder(n => n.OccurredAt);

    public void Dispose()
    {
        _client?.Dispose();
        _factory.Dispose();
    }
}
