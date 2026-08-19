using ECommerce.Notifications.API.Consumers;
using ECommerce.Notifications.API.Data;
using ECommerce.Orders.Events.V1;
using FluentAssertions;
using MassTransit;
using MassTransit.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace ECommerce.Notifications.Tests.Integration;

public class OrderPaidNotificationConsumerSteps : IAsyncDisposable
{
    // Unique DB name per test instance to avoid cross-test row bleed.
    private readonly string _dbName = $"notifications-order-paid-{Guid.NewGuid()}";
    private ServiceProvider? _provider;
    private ITestHarness? _harness;

    public async Task Given_HarnessWithInMemoryTransport()
    {
        var services = new ServiceCollection();
        services.AddLogging();

        services.AddDbContext<NotificationsDbContext>(o => o.UseInMemoryDatabase(_dbName));

        services.AddMassTransitTestHarness(x =>
        {
            x.AddConsumer<OrderPaidNotificationConsumer>();
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

    public async Task Then_NotificationEntryExists(string expectedMessage, string expectedEventType)
    {
        await using var scope = _provider!.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<NotificationsDbContext>();

        var entries = await db.NotificationEntries.ToListAsync();
        entries.Should().ContainSingle(n => n.Message == expectedMessage && n.EventType == expectedEventType,
            "OrderPaidNotificationConsumer should insert exactly one correctly-populated row for a Paid transition");
    }

    public async Task Then_NoNotificationEntryExists()
    {
        await using var scope = _provider!.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<NotificationsDbContext>();

        var count = await db.NotificationEntries.CountAsync();
        count.Should().Be(0, "a non-Paid OrderStatusChanged transition must not insert a row");
    }

    public async ValueTask DisposeAsync()
    {
        if (_harness is not null)
            await _harness.Stop();
        if (_provider is not null)
            await _provider.DisposeAsync();
    }
}
