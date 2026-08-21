using ECommerce.Tests.Common;
using Xunit;

namespace ECommerce.Notifications.Tests.Integration;

public class NotificationsEndpointTests(PostgresFixture fixture) : IClassFixture<PostgresFixture>
{
    private readonly NotificationsEndpointSteps _steps = new(fixture);

    [Fact]
    public async Task GetNotifications_ForUserWithMultipleEntries_ReturnsOnlyThatUsersEntriesNewestFirst()
    {
        var userA = Guid.NewGuid().ToString();
        var userB = Guid.NewGuid().ToString();
        var now = DateTimeOffset.UtcNow;

        await _steps.Given_NotificationExistsForUser(userA, "OrderPaid", "Your order has been paid.", Guid.NewGuid(), now.AddMinutes(-5));
        await _steps.Given_NotificationExistsForUser(userA, "OrderShipped", "Your order has shipped.", Guid.NewGuid(), now);
        await _steps.Given_NotificationExistsForUser(userB, "OrderPaid", "Your order has been paid.", Guid.NewGuid(), now);

        var response = await _steps.When_GetNotificationsIsCalled(userA);

        _steps.Then_ResponseIs200(response);
        var body = await _steps.Then_ResponseContainsExactlyEntriesFor(response, userA, expectedCount: 2);
        _steps.Then_EntriesAreOrderedByOccurredAtDescending(body);
    }

    [Fact]
    public async Task GetNotifications_ForDifferentUser_NeverLeaksOtherUsersEntries()
    {
        var userA = Guid.NewGuid().ToString();
        var userB = Guid.NewGuid().ToString();
        var now = DateTimeOffset.UtcNow;

        await _steps.Given_NotificationExistsForUser(userA, "OrderPaid", "Your order has been paid.", Guid.NewGuid(), now.AddMinutes(-5));
        await _steps.Given_NotificationExistsForUser(userA, "OrderShipped", "Your order has shipped.", Guid.NewGuid(), now);
        await _steps.Given_NotificationExistsForUser(userB, "PaymentFailed", "Payment failed for your order.", Guid.NewGuid(), now);

        // IDOR-safe: a different X-Test-User-Id hitting the SAME endpoint must only ever see
        // its own entries — userA's rows must never leak (mirrors GET /orders's mitigation).
        var response = await _steps.When_GetNotificationsIsCalled(userB);

        _steps.Then_ResponseIs200(response);
        await _steps.Then_ResponseContainsExactlyEntriesFor(response, userB, expectedCount: 1);
    }

    [Fact]
    public async Task GetNotifications_ForUserWithNoEntries_ReturnsEmptyArray()
    {
        var userWithNoEntries = Guid.NewGuid().ToString();

        var response = await _steps.When_GetNotificationsIsCalled(userWithNoEntries);

        // An empty inbox is a valid, expected state — 200 with an empty array, not 404.
        _steps.Then_ResponseIs200(response);
        await _steps.Then_ResponseContainsExactlyEntriesFor(response, userWithNoEntries, expectedCount: 0);
    }
}
