using ECommerce.Fulfillment.Events.V1;
using ECommerce.Notifications.API.Consumers;
using ECommerce.Notifications.API.Data;
using ECommerce.Notifications.API.Features.Notifications;
using ECommerce.Orders.Events.V1;
using ECommerce.Payments.Events.V1;
using FluentAssertions;
using MassTransit;
using MassTransit.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace ECommerce.Notifications.Tests.Integration;

public class NotificationConsumersSteps : IAsyncDisposable
{
    private ServiceProvider? _provider;
    private ITestHarness? _harness;
    private readonly string _dbName = $"notification-consumers-test-{Guid.NewGuid()}";

    public async Task Given_HarnessWithInMemoryTransport()
    {
        var services = new ServiceCollection();
        services.AddLogging();

        services.AddDbContext<NotificationsDbContext>(o =>
            o.UseInMemoryDatabase(_dbName));

        services.AddMassTransitTestHarness(x =>
        {
            x.AddConsumer<OrderPaidNotificationConsumer>();
            x.AddConsumer<OrderShippedNotificationConsumer>();
            x.AddConsumer<PaymentFailedNotificationConsumer>();
        });

        _provider = services.BuildServiceProvider(true);
        _harness = _provider.GetRequiredService<ITestHarness>();
        await _harness.Start();
    }

    public async Task When_OrderStatusChangedPublished(string userId, Guid orderId, string newStatus, DateTimeOffset changedAt)
    {
        var messageId = Guid.NewGuid();
        var message = new OrderStatusChanged(
            MessageId: messageId,
            CorrelationId: orderId,
            CausationId: Guid.Empty,
            OccurredAt: DateTimeOffset.UtcNow,
            OrderId: orderId,
            UserId: userId,
            PreviousStatus: "Pending",
            NewStatus: newStatus,
            ChangedAt: changedAt);

        await _harness!.Bus.Publish(message, ctx => ctx.MessageId = messageId);
        await _harness!.InactivityTask;
    }

    public async Task When_OrderShippedPublished(string userId, Guid checkoutId, DateTimeOffset shippedAt)
    {
        var messageId = Guid.NewGuid();
        var message = new OrderShipped(
            MessageId: messageId,
            CorrelationId: checkoutId,
            CausationId: Guid.Empty,
            OccurredAt: DateTimeOffset.UtcNow,
            CheckoutId: checkoutId,
            UserId: userId,
            ShippedAt: shippedAt);

        await _harness!.Bus.Publish(message, ctx => ctx.MessageId = messageId);
        await _harness!.InactivityTask;
    }

    public async Task When_PaymentFailedPublished(string userId, Guid checkoutId, decimal amount, string reason, DateTimeOffset failedAt)
    {
        var messageId = Guid.NewGuid();
        var message = new PaymentFailed(
            MessageId: messageId,
            CorrelationId: checkoutId,
            CausationId: Guid.Empty,
            OccurredAt: DateTimeOffset.UtcNow,
            CheckoutId: checkoutId,
            UserId: userId,
            Amount: amount,
            Reason: reason,
            FailedAt: failedAt);

        await _harness!.Bus.Publish(message, ctx => ctx.MessageId = messageId);
        await _harness!.InactivityTask;
    }

    public async Task Then_NotificationEntryExists(string expectedUserId, Guid expectedOrderId, string expectedMessage, string expectedEventType, DateTimeOffset expectedOccurredAt)
    {
        await using var scope = _provider!.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<NotificationsDbContext>();

        var entry = await db.NotificationEntries.SingleOrDefaultAsync(n =>
            n.UserId == expectedUserId && n.OrderId == expectedOrderId && n.EventType == expectedEventType);

        entry.Should().NotBeNull("a NotificationEntry row should have been inserted for this event");
        entry!.Message.Should().Be(expectedMessage);
        entry.OccurredAt.Should().Be(expectedOccurredAt);
    }

    public async Task Then_NoNotificationEntryExists()
    {
        await using var scope = _provider!.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<NotificationsDbContext>();

        var count = await db.NotificationEntries.CountAsync();
        count.Should().Be(0, "no-op filtered OrderStatusChanged transitions must not insert a row");
    }

    public async ValueTask DisposeAsync()
    {
        if (_harness is not null)
            await _harness.Stop();
        if (_provider is not null)
            await _provider.DisposeAsync();
    }
}
