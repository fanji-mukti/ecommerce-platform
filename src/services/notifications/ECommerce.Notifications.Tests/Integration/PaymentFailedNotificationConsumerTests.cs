using Xunit;

namespace ECommerce.Notifications.Tests.Integration;

public class PaymentFailedNotificationConsumerTests
{
    [Fact]
    public async Task PaymentFailedNotificationConsumer_WhenPaymentFailedPublished_InsertsNotificationEntry()
    {
        await using var steps = new PaymentFailedNotificationConsumerSteps();
        await steps.Given_HarnessWithInMemoryTransport();

        var userId = Guid.NewGuid().ToString();
        var checkoutId = Guid.NewGuid();
        var failedAt = DateTimeOffset.UtcNow;

        await steps.When_PaymentFailedPublished(userId, checkoutId, 42.50m, "Card declined", failedAt);

        await steps.Then_NotificationEntryExists("Payment failed for your order.", "PaymentFailed");
    }
}
