using ECommerce.Fulfillment.Events.V1;
using ECommerce.Notifications.API.Consumers;
using ECommerce.Notifications.API.Data;
using ECommerce.Tests.Common;
using FluentAssertions;
using MassTransit;
using MassTransit.EntityFrameworkCoreIntegration;
using MassTransit.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace ECommerce.Notifications.Tests.Integration;

public class OrderShippedNotificationInboxDeduplicationSteps : IAsyncLifetime
{
    private readonly PostgresFixture _postgresFixture = new();
    private ServiceProvider? _provider;
    private ITestHarness? _harness;

    public async ValueTask InitializeAsync()
    {
        await _postgresFixture.InitializeAsync();
    }

    public async ValueTask DisposeAsync()
    {
        if (_harness is not null)
            await _harness.Stop();
        if (_provider is not null)
            await _provider.DisposeAsync();
        await _postgresFixture.DisposeAsync();
    }

    public async Task Given_HarnessWithPostgresInbox()
    {
        var connectionString = _postgresFixture.ConnectionString;

        var services = new ServiceCollection();
        services.AddLogging();

        services.AddDbContext<NotificationsDbContext>(o =>
            o.UseNpgsql(connectionString));

        services.AddMassTransitTestHarness(x =>
        {
            x.AddConsumer<OrderShippedNotificationConsumer>();

            x.AddEntityFrameworkOutbox<NotificationsDbContext>(o =>
            {
                o.UsePostgres();
                // No UseBusOutbox — Notifications is consumer-only, matching the CatalogSeeded analog
            });

            x.AddConfigureEndpointsCallback((context, name, cfg) =>
            {
                cfg.UseEntityFrameworkOutbox<NotificationsDbContext>(context);
            });
        });

        _provider = services.BuildServiceProvider(true);

        // Apply migrations to create InboxState/NotificationEntries tables before running tests
        await using var scope = _provider.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<NotificationsDbContext>();
        await db.Database.MigrateAsync();

        _harness = _provider.GetRequiredService<ITestHarness>();
        await _harness.Start();
    }

    public async Task When_SameOrderShippedPublishedTwice(Guid messageId, Guid checkoutId, string userId)
    {
        var message = new OrderShipped(
            MessageId: messageId,
            CorrelationId: checkoutId,
            CausationId: Guid.Empty,
            OccurredAt: DateTimeOffset.UtcNow,
            CheckoutId: checkoutId,
            UserId: userId,
            ShippedAt: DateTimeOffset.UtcNow);

        // Pin the transport-level MessageId to the same value on both publishes — MassTransit
        // generates a fresh Guid transport MessageId per Publish call by default, so the inbox
        // would otherwise store two rows. This proves the EF Core inbox deduplicates a genuine
        // forced redelivery (same transport MessageId), not just two distinct messages.
        await _harness!.Bus.Publish(message, ctx => ctx.MessageId = messageId);
        await _harness!.InactivityTask;

        await _harness!.Bus.Publish(message, ctx => ctx.MessageId = messageId);
        await _harness!.InactivityTask;
    }

    public async Task Then_InboxStateHasExactlyOneRow()
    {
        await using var scope = _provider!.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<NotificationsDbContext>();

        var inboxCount = await db.Set<InboxState>().CountAsync();
        inboxCount.Should().Be(1,
            "the MassTransit EF Core inbox should persist exactly one InboxState row for a duplicate-MessageId delivery");
    }

    public async Task Then_NotificationEntriesHasExactlyOneRow()
    {
        await using var scope = _provider!.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<NotificationsDbContext>();

        var notificationCount = await db.NotificationEntries.CountAsync();
        notificationCount.Should().Be(1,
            "a redelivered OrderShipped (same transport MessageId) must produce exactly one NotificationEntry row, not two (NOT-02)");
    }
}
