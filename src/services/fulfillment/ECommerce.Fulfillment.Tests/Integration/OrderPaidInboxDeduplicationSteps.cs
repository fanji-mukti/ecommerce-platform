using ECommerce.Fulfillment.API.Data;
using ECommerce.Fulfillment.API.Features.Fulfillment;
using ECommerce.Orders.Events.V1;
using ECommerce.Tests.Common;
using FluentAssertions;
using MassTransit;
using MassTransit.EntityFrameworkCoreIntegration;
using MassTransit.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace ECommerce.Fulfillment.Tests.Integration;

public class OrderPaidInboxDeduplicationSteps : IAsyncLifetime
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

    public async Task Given_HarnessWithPostgresInboxAndOutbox()
    {
        var connectionString = _postgresFixture.ConnectionString;

        var services = new ServiceCollection();
        services.AddLogging();

        services.AddDbContext<FulfillmentDbContext>(o =>
            o.UseNpgsql(connectionString));

        // Short ProcessingSeconds so SchedulePublish's target delay is negligible — this test's
        // subject is inbox deduplication (INF-02), not the scheduled-publish timing itself.
        services.Configure<FulfillmentOptions>(o => o.ProcessingSeconds = 0.1);

        services.AddMassTransitTestHarness(x =>
        {
            x.AddConsumer<OrderPaidConsumer>();

            x.AddEntityFrameworkOutbox<FulfillmentDbContext>(o =>
            {
                o.UsePostgres();
                // UseBusOutbox() IS needed here, unlike the Notifications analog —
                // OrderPaidConsumer publishes/schedules OrderShipped, so the bus outbox
                // drainer applies (mirrors ECommerce.Fulfillment.API/Program.cs's production wiring).
                o.UseBusOutbox();
            });

            x.AddConfigureEndpointsCallback((context, name, cfg) =>
            {
                cfg.UseEntityFrameworkOutbox<FulfillmentDbContext>(context);
            });

            x.UsingInMemory((context, cfg) =>
            {
                cfg.UseInMemoryScheduler();
                cfg.ConfigureEndpoints(context);
            });
        });

        _provider = services.BuildServiceProvider(true);

        // Apply migrations to create InboxState/OutboxState/OutboxMessage tables before running tests
        await using var scope = _provider.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<FulfillmentDbContext>();
        await db.Database.MigrateAsync();

        _harness = _provider.GetRequiredService<ITestHarness>();
        await _harness.Start();
    }

    public async Task When_SameOrderStatusChangedPublishedTwice(Guid messageId, Guid orderId, string userId)
    {
        var message = new OrderStatusChanged(
            MessageId: messageId,
            CorrelationId: orderId,
            CausationId: Guid.Empty,
            OccurredAt: DateTimeOffset.UtcNow,
            OrderId: orderId,
            UserId: userId,
            PreviousStatus: "Pending",
            NewStatus: "Paid",
            ChangedAt: DateTimeOffset.UtcNow);

        // Gap 4 fix (mirrors CatalogSeededInboxDeduplicationSteps): pin the transport-level
        // MessageId to the same value on both publishes. MassTransit generates a fresh transport
        // MessageId per Publish call by default, so the inbox would otherwise store two rows.
        await _harness!.Bus.Publish(message, ctx => ctx.MessageId = messageId);
        await _harness!.Bus.Publish(message, ctx => ctx.MessageId = messageId);

        await _harness!.InactivityTask;
    }

    public async Task Then_InboxStateHasExactlyOneRow()
    {
        await using var scope = _provider!.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<FulfillmentDbContext>();

        // Query InboxState rows — proves EF Core inbox deduplication stored exactly one record,
        // meaning OrderPaidConsumer's body (and therefore its SchedulePublish<OrderShipped> call)
        // did not re-execute on the second delivery of the same transport MessageId.
        var inboxCount = await db.Set<InboxState>().CountAsync();
        inboxCount.Should().Be(1,
            "the MassTransit EF Core inbox should persist exactly one InboxState row for a duplicate-MessageId delivery");
    }
}
