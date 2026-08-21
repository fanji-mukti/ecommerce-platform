using Xunit;

namespace ECommerce.Notifications.Tests.Integration;

public class OrderShippedNotificationConsumerTests
{
    [Fact]
    public async Task OrderShippedNotificationConsumer_WhenOrderShippedPublished_InsertsNotificationEntry()
    {
        await using var steps = new OrderShippedNotificationConsumerSteps();
        await steps.Given_HarnessWithInMemoryTransport();

        var userId = Guid.NewGuid().ToString();
        var checkoutId = Guid.NewGuid();
        var shippedAt = DateTimeOffset.UtcNow;

        await steps.When_OrderShippedPublished(userId, checkoutId, shippedAt);

        await steps.Then_NotificationEntryExists("Your order has shipped.", "OrderShipped");
    }
}
