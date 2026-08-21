using Xunit;

namespace ECommerce.Notifications.Tests.Integration;

public class PaymentFailedNotificationInboxDeduplicationTests : IAsyncLifetime
{
    private readonly PaymentFailedNotificationInboxDeduplicationSteps _steps = new();

    public ValueTask InitializeAsync() => _steps.InitializeAsync();

    public ValueTask DisposeAsync() => _steps.DisposeAsync();

    [Fact]
    public async Task PaymentFailedNotificationConsumer_WhenSameMessageIdDeliveredTwice_ProducesExactlyOneNotificationEntry()
    {
        await _steps.Given_HarnessWithPostgresInbox();

        var messageId = Guid.NewGuid();
        var checkoutId = Guid.NewGuid();
        var userId = Guid.NewGuid().ToString();
        var failedAt = DateTimeOffset.UtcNow;

        await _steps.When_SamePaymentFailedPublishedTwice(messageId, checkoutId, userId, 42.50m, "Card declined", failedAt);

        await _steps.Then_InboxStateHasExactlyOneRow();
        await _steps.Then_NotificationEntriesHasExactlyOneRow();
    }
}
