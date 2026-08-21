using ECommerce.Notifications.API.Consumers;
using ECommerce.Notifications.API.Data;
using ECommerce.Notifications.API.Features.Notifications;
using ECommerce.Orders.Events.V1;
using FluentAssertions;
using MassTransit;
using MassTransit.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace ECommerce.Notifications.Tests.Integration;

public class OrderStatusSnapshotConsumerSteps : IAsyncDisposable
{
    // Unique DB name per test instance to avoid cross-test row bleed.
    private readonly string _dbName = $"notifications-order-status-snapshot-{Guid.NewGuid()}";
    private ServiceProvider? _provider;
    private ITestHarness? _harness;

    public async Task Given_HarnessWithInMemoryTransport()
    {
        var services = new ServiceCollection();
        services.AddLogging();

        services.AddDbContext<NotificationsDbContext>(o => o.UseInMemoryDatabase(_dbName));

        services.AddMassTransitTestHarness(x =>
        {
            x.AddConsumer<OrderStatusSnapshotConsumer>();
        });

        _provider = services.BuildServiceProvider(true);
        _harness = _provider.GetRequiredService<ITestHarness>();
        await _harness.Start();
    }

    public async Task When_OrderStatusChangedPublished(Guid orderId, string userId, string newStatus, DateTimeOffset changedAt)
    {
        var messageId = Guid.NewGuid();
        var message = new OrderStatusChanged(
            MessageId: messageId,
            CorrelationId: orderId,
            CausationId: Guid.Empty,
            OccurredAt: DateTimeOffset.UtcNow,
            OrderId: orderId,
            UserId: userId,
            PreviousStatus: "Unknown",
            NewStatus: newStatus,
            ChangedAt: changedAt);

        await _harness!.Bus.Publish(message, ctx => ctx.MessageId = messageId);
        await _harness!.InactivityTask;
    }

    /// <summary>
    /// Polls (rather than asserting once) because ITestHarness.InactivityTask can resolve slightly
    /// before a just-consumed message's SaveChangesAsync has become visible to a freshly-opened
    /// DbContext scope when multiple OrderStatusChanged messages are published back-to-back in the
    /// same test — a harness synchronization race, not a bug in the consumer itself.
    /// </summary>
    public async Task Then_SnapshotStatusIs(Guid orderId, string expectedStatus, DateTimeOffset expectedUpdatedAt)
    {
        var deadline = DateTime.UtcNow.AddSeconds(5);
        OrderStatusSnapshot? snapshot = null;

        while (DateTime.UtcNow < deadline)
        {
            await using var scope = _provider!.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<NotificationsDbContext>();

            snapshot = await db.OrderStatusSnapshots.AsNoTracking().FirstOrDefaultAsync(s => s.OrderId == orderId);
            if (snapshot is not null && snapshot.Status == expectedStatus && snapshot.UpdatedAt == expectedUpdatedAt)
                return;

            await Task.Delay(50);
        }

        snapshot.Should().NotBeNull($"expected a row for {orderId} with Status={expectedStatus}");
        snapshot!.Status.Should().Be(expectedStatus);
        snapshot.UpdatedAt.Should().Be(expectedUpdatedAt);
    }

    public async ValueTask DisposeAsync()
    {
        if (_harness is not null)
            await _harness.Stop();
        if (_provider is not null)
            await _provider.DisposeAsync();
    }
}
