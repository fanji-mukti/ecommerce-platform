using ECommerce.Fulfillment.Events.V1;
using ECommerce.Notifications.API.Consumers;
using ECommerce.Notifications.API.Data;
using FluentAssertions;
using MassTransit;
using MassTransit.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace ECommerce.Notifications.Tests.Integration;

public class OrderShippedNotificationConsumerSteps : IAsyncDisposable
{
    // Unique DB name per test instance to avoid cross-test row bleed.
    private readonly string _dbName = $"notifications-order-shipped-{Guid.NewGuid()}";
    private ServiceProvider? _provider;
    private ITestHarness? _harness;

    public async Task Given_HarnessWithInMemoryTransport()
    {
        var services = new ServiceCollection();
        services.AddLogging();

        services.AddDbContext<NotificationsDbContext>(o => o.UseInMemoryDatabase(_dbName));

        services.AddMassTransitTestHarness(x =>
        {
            x.AddConsumer<OrderShippedNotificationConsumer>();
        });

        _provider = services.BuildServiceProvider(true);
        _harness = _provider.GetRequiredService<ITestHarness>();
        await _harness.Start();
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

    public async Task Then_NotificationEntryExists(string expectedMessage, string expectedEventType)
    {
        await using var scope = _provider!.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<NotificationsDbContext>();

        var entries = await db.NotificationEntries.ToListAsync();
        entries.Should().ContainSingle(n => n.Message == expectedMessage && n.EventType == expectedEventType,
            "OrderShippedNotificationConsumer should insert exactly one correctly-populated row");
    }

    public async ValueTask DisposeAsync()
    {
        if (_harness is not null)
            await _harness.Stop();
        if (_provider is not null)
            await _provider.DisposeAsync();
    }
}
