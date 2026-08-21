using Xunit;

namespace ECommerce.Notifications.Tests.Integration;

public class OrderPaidNotificationInboxDeduplicationTests : IAsyncLifetime
{
    private readonly OrderPaidNotificationInboxDeduplicationSteps _steps = new();

    public ValueTask InitializeAsync() => _steps.InitializeAsync();

    public ValueTask DisposeAsync() => _steps.DisposeAsync();

    [Fact]
    public async Task OrderPaidNotificationConsumer_WhenSameMessageIdDeliveredTwice_ProducesExactlyOneNotificationEntry()
    {
        await _steps.Given_HarnessWithPostgresInbox();

        var messageId = Guid.NewGuid();
        var orderId = Guid.NewGuid();
        var userId = Guid.NewGuid().ToString();

        await _steps.When_SameOrderStatusChangedPublishedTwice(messageId, orderId, userId);

        await _steps.Then_InboxStateHasExactlyOneRow();
        await _steps.Then_NotificationEntriesHasExactlyOneRow();
    }
}
