using Xunit;

namespace ECommerce.Notifications.Tests.Integration;

public class OrderStatusSnapshotConsumerTests
{
    [Fact]
    public async Task OrderStatusSnapshotConsumer_WhenNoExistingSnapshot_InsertsSnapshot()
    {
        await using var steps = new OrderStatusSnapshotConsumerSteps();
        await steps.Given_HarnessWithInMemoryTransport();

        var orderId = Guid.NewGuid();
        var userId = Guid.NewGuid().ToString();
        var t1 = DateTimeOffset.UtcNow.AddMinutes(-10);

        await steps.When_OrderStatusChangedPublished(orderId, userId, "Paid", t1);

        await steps.Then_SnapshotStatusIs(orderId, "Paid", t1);
    }

    [Fact]
    public async Task OrderStatusSnapshotConsumer_WhenNewerStatusArrives_UpdatesSnapshot()
    {
        await using var steps = new OrderStatusSnapshotConsumerSteps();
        await steps.Given_HarnessWithInMemoryTransport();

        var orderId = Guid.NewGuid();
        var userId = Guid.NewGuid().ToString();
        var t1 = DateTimeOffset.UtcNow.AddMinutes(-10);
        var t2 = DateTimeOffset.UtcNow.AddMinutes(-5);

        await steps.When_OrderStatusChangedPublished(orderId, userId, "Paid", t1);
        await steps.Then_SnapshotStatusIs(orderId, "Paid", t1);
        await steps.When_OrderStatusChangedPublished(orderId, userId, "Cancelled", t2);

        await steps.Then_SnapshotStatusIs(orderId, "Cancelled", t2);
    }

    [Fact]
    public async Task OrderStatusSnapshotConsumer_WhenStaleOutOfOrderStatusArrives_DoesNotClobberNewerSnapshot()
    {
        await using var steps = new OrderStatusSnapshotConsumerSteps();
        await steps.Given_HarnessWithInMemoryTransport();

        var orderId = Guid.NewGuid();
        var userId = Guid.NewGuid().ToString();
        var t1 = DateTimeOffset.UtcNow.AddMinutes(-10);
        var t2 = DateTimeOffset.UtcNow.AddMinutes(-5);

        await steps.When_OrderStatusChangedPublished(orderId, userId, "Paid", t1);
        await steps.When_OrderStatusChangedPublished(orderId, userId, "Cancelled", t2);
        // Stale/out-of-order redelivery of the older "Paid" transition must not clobber "Cancelled".
        await steps.When_OrderStatusChangedPublished(orderId, userId, "Paid", t1);

        await steps.Then_SnapshotStatusIs(orderId, "Cancelled", t2);
    }
}
