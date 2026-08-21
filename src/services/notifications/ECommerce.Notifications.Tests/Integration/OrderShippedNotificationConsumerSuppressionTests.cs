using Xunit;

namespace ECommerce.Notifications.Tests.Integration;

public class OrderShippedNotificationConsumerSuppressionTests
{
    [Fact]
    public async Task OrderShippedNotificationConsumer_WhenSnapshotStatusIsCancelled_SuppressesShippedNotification()
    {
        await using var steps = new OrderShippedNotificationConsumerSuppressionSteps();
        await steps.Given_HarnessWithInMemoryTransport();

        var userId = Guid.NewGuid().ToString();
        var checkoutId = Guid.NewGuid();
        var shippedAt = DateTimeOffset.UtcNow;

        await steps.Given_ExistingSnapshot(checkoutId, "Cancelled", shippedAt.AddMinutes(-5));
        await steps.When_OrderShippedPublished(userId, checkoutId, shippedAt);

        await steps.Then_NoNotificationEntryExists(checkoutId, "OrderShipped");
    }

    [Fact]
    public async Task OrderShippedNotificationConsumer_WhenSnapshotStatusIsFailed_SuppressesShippedNotification()
    {
        await using var steps = new OrderShippedNotificationConsumerSuppressionSteps();
        await steps.Given_HarnessWithInMemoryTransport();

        var userId = Guid.NewGuid().ToString();
        var checkoutId = Guid.NewGuid();
        var shippedAt = DateTimeOffset.UtcNow;

        await steps.Given_ExistingSnapshot(checkoutId, "Failed", shippedAt.AddMinutes(-5));
        await steps.When_OrderShippedPublished(userId, checkoutId, shippedAt);

        await steps.Then_NoNotificationEntryExists(checkoutId, "OrderShipped");
    }

    [Fact]
    public async Task OrderShippedNotificationConsumer_WhenNoSnapshotExists_InsertsShippedNotification()
    {
        await using var steps = new OrderShippedNotificationConsumerSuppressionSteps();
        await steps.Given_HarnessWithInMemoryTransport();

        var userId = Guid.NewGuid().ToString();
        var checkoutId = Guid.NewGuid();
        var shippedAt = DateTimeOffset.UtcNow;

        await steps.When_OrderShippedPublished(userId, checkoutId, shippedAt);

        await steps.Then_NotificationEntryExists("Your order has shipped.", "OrderShipped");
    }

    [Fact]
    public async Task OrderShippedNotificationConsumer_WhenSnapshotStatusIsPaid_InsertsShippedNotification()
    {
        await using var steps = new OrderShippedNotificationConsumerSuppressionSteps();
        await steps.Given_HarnessWithInMemoryTransport();

        var userId = Guid.NewGuid().ToString();
        var checkoutId = Guid.NewGuid();
        var shippedAt = DateTimeOffset.UtcNow;

        await steps.Given_ExistingSnapshot(checkoutId, "Paid", shippedAt.AddMinutes(-5));
        await steps.When_OrderShippedPublished(userId, checkoutId, shippedAt);

        await steps.Then_NotificationEntryExists("Your order has shipped.", "OrderShipped");
    }
}
