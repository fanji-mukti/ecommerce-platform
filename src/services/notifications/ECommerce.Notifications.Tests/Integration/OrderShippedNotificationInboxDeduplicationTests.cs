using Xunit;

namespace ECommerce.Notifications.Tests.Integration;

public class OrderShippedNotificationInboxDeduplicationTests : IAsyncLifetime
{
    private readonly OrderShippedNotificationInboxDeduplicationSteps _steps = new();

    public ValueTask InitializeAsync() => _steps.InitializeAsync();

    public ValueTask DisposeAsync() => _steps.DisposeAsync();

    [Fact]
    public async Task OrderShippedNotificationConsumer_WhenSameMessageIdDeliveredTwice_ProducesExactlyOneNotificationEntry()
    {
        await _steps.Given_HarnessWithPostgresInbox();

        var messageId = Guid.NewGuid();
        var checkoutId = Guid.NewGuid();
        var userId = Guid.NewGuid().ToString();

        await _steps.When_SameOrderShippedPublishedTwice(messageId, checkoutId, userId);

        await _steps.Then_InboxStateHasExactlyOneRow();
        await _steps.Then_NotificationEntriesHasExactlyOneRow();
    }
}
