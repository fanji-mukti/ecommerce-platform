using Xunit;

namespace ECommerce.Fulfillment.Tests.Integration;

public class OrderPaidConsumerTests
{
    [Fact]
    public async Task OrderPaidConsumer_WhenNewStatusIsPaid_SchedulesOrderShippedWithMatchingCheckoutIdAndUserId()
    {
        await using var steps = new OrderPaidConsumerSteps();

        await steps.Given_HarnessWithInMemoryScheduler();

        var orderId = Guid.NewGuid();
        const string userId = "user-1";
        await steps.When_OrderStatusChangedPublished(orderId, userId, "Paid");

        await steps.Then_OrderShippedScheduledWithCheckoutIdAndUserId(orderId, userId);
    }

    [Fact]
    public async Task OrderPaidConsumer_WhenNewStatusIsNotPaid_DoesNotScheduleOrderShipped()
    {
        await using var steps = new OrderPaidConsumerSteps();

        await steps.Given_HarnessWithInMemoryScheduler();

        var orderId = Guid.NewGuid();
        const string userId = "user-1";
        await steps.When_OrderStatusChangedPublished(orderId, userId, "Cancelled");

        await steps.Then_NoOrderShippedScheduled();
    }
}
