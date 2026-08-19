using Xunit;

namespace ECommerce.Notifications.Tests.Integration;

public class OrderPaidNotificationConsumerTests
{
    [Fact]
    public async Task OrderPaidNotificationConsumer_WhenNewStatusIsPaid_InsertsNotificationEntry()
    {
        await using var steps = new OrderPaidNotificationConsumerSteps();
        await steps.Given_HarnessWithInMemoryTransport();

        var userId = Guid.NewGuid().ToString();
        var orderId = Guid.NewGuid();
        var changedAt = DateTimeOffset.UtcNow;

        await steps.When_OrderStatusChangedPublished(userId, orderId, "Paid", changedAt);

        await steps.Then_NotificationEntryExists("Your order has been paid.", "OrderPaid");
    }

    [Fact]
    public async Task OrderPaidNotificationConsumer_WhenNewStatusIsNotPaid_InsertsNoRow()
    {
        await using var steps = new OrderPaidNotificationConsumerSteps();
        await steps.Given_HarnessWithInMemoryTransport();

        var userId = Guid.NewGuid().ToString();
        var orderId = Guid.NewGuid();

        await steps.When_OrderStatusChangedPublished(userId, orderId, "Cancelled", DateTimeOffset.UtcNow);

        await steps.Then_NoNotificationEntryExists();
    }
}
