using Xunit;

namespace ECommerce.Notifications.Tests.Integration;

public class NotificationConsumersTests
{
    [Fact]
    public async Task OrderPaidNotificationConsumer_WhenNewStatusIsPaid_InsertsNotificationEntry()
    {
        await using var steps = new NotificationConsumersSteps();
        await steps.Given_HarnessWithInMemoryTransport();

        var userId = Guid.NewGuid().ToString();
        var orderId = Guid.NewGuid();
        var changedAt = DateTimeOffset.UtcNow;

        await steps.When_OrderStatusChangedPublished(userId, orderId, "Paid", changedAt);

        await steps.Then_NotificationEntryExists(userId, orderId, "Your order has been paid.", "OrderPaid", changedAt);
    }

    [Theory]
    [InlineData("Cancelled")]
    [InlineData("Failed")]
    [InlineData("Fulfilled")]
    public async Task OrderPaidNotificationConsumer_WhenNewStatusIsNotPaid_DoesNotInsertNotificationEntry(string newStatus)
    {
        await using var steps = new NotificationConsumersSteps();
        await steps.Given_HarnessWithInMemoryTransport();

        var userId = Guid.NewGuid().ToString();
        var orderId = Guid.NewGuid();

        await steps.When_OrderStatusChangedPublished(userId, orderId, newStatus, DateTimeOffset.UtcNow);

        await steps.Then_NoNotificationEntryExists();
    }

    [Fact]
    public async Task OrderShippedNotificationConsumer_WhenOrderShippedPublished_InsertsNotificationEntry()
    {
        await using var steps = new NotificationConsumersSteps();
        await steps.Given_HarnessWithInMemoryTransport();

        var userId = Guid.NewGuid().ToString();
        var checkoutId = Guid.NewGuid();
        var shippedAt = DateTimeOffset.UtcNow;

        await steps.When_OrderShippedPublished(userId, checkoutId, shippedAt);

        await steps.Then_NotificationEntryExists(userId, checkoutId, "Your order has shipped.", "OrderShipped", shippedAt);
    }

    [Fact]
    public async Task PaymentFailedNotificationConsumer_WhenPaymentFailedPublished_InsertsNotificationEntry()
    {
        await using var steps = new NotificationConsumersSteps();
        await steps.Given_HarnessWithInMemoryTransport();

        var userId = Guid.NewGuid().ToString();
        var checkoutId = Guid.NewGuid();
        var failedAt = DateTimeOffset.UtcNow;

        await steps.When_PaymentFailedPublished(userId, checkoutId, 42.50m, "Card declined", failedAt);

        await steps.Then_NotificationEntryExists(userId, checkoutId, "Payment failed for your order.", "PaymentFailed", failedAt);
    }
}
