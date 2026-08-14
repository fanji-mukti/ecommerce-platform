using ECommerce.Orders.API.Data;
using ECommerce.Orders.API.Features.Orders;
using ECommerce.Orders.Events.V1;
using ECommerce.Tests.Common;
using FluentAssertions;
using MassTransit;
using MassTransit.EntityFrameworkCoreIntegration;
using MassTransit.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace ECommerce.Orders.Tests.Integration;

public class OrderReadModelInboxDeduplicationSteps : IAsyncLifetime
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

        services.AddDbContext<OrdersDbContext>(o =>
            o.UseNpgsql(connectionString));

        services.AddMassTransitTestHarness(x =>
        {
            x.AddConsumer<OrderReadModelProjector>();

            x.AddEntityFrameworkOutbox<OrdersDbContext>(o =>
            {
                o.UsePostgres();
                // No UseBusOutbox — this harness only exercises the consumer/inbox side.
            });

            x.AddConfigureEndpointsCallback((context, name, cfg) =>
            {
                cfg.UseEntityFrameworkOutbox<OrdersDbContext>(context);
            });
        });

        _provider = services.BuildServiceProvider(true);

        // Apply migrations to create InboxState/OrderReadModels tables before running tests.
        await using var scope = _provider.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<OrdersDbContext>();
        await db.Database.MigrateAsync();

        _harness = _provider.GetRequiredService<ITestHarness>();
        await _harness.Start();
    }

    public async Task When_SameOrderCreatedMessagePublishedTwice(Guid messageId, Guid orderId)
    {
        var message = new OrderCreated(
            MessageId: messageId,
            CorrelationId: orderId,
            CausationId: Guid.Empty,
            OccurredAt: DateTimeOffset.UtcNow,
            OrderId: orderId,
            UserId: "11111111-1111-1111-1111-111111111111",
            LineItems: [new OrderLineItemData(Guid.NewGuid(), "Test Product", 19.99m, 2)],
            TotalAmount: 39.98m,
            CreatedAt: DateTimeOffset.UtcNow);

        // Gap 4 fix (mirrors CatalogSeededInboxDeduplicationSteps): pin the transport-level
        // MessageId to the same value on both publishes so the EF Core inbox deduplicates.
        await _harness!.Bus.Publish(message, ctx => ctx.MessageId = messageId);
        await _harness!.Bus.Publish(message, ctx => ctx.MessageId = messageId);

        await _harness!.InactivityTask;
    }

    public async Task Then_InboxStateHasExactlyOneRow()
    {
        await using var scope = _provider!.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<OrdersDbContext>();

        var inboxCount = await db.Set<InboxState>().CountAsync();
        inboxCount.Should().Be(1,
            "the MassTransit EF Core inbox should persist exactly one InboxState row for a duplicate-MessageId delivery");
    }

    public async Task Then_OrderReadModelHasExactlyOneRowFor(Guid orderId)
    {
        await using var scope = _provider!.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<OrdersDbContext>();

        var readModelCount = await db.OrderReadModels.CountAsync(o => o.Id == orderId);
        readModelCount.Should().Be(1,
            "OrderReadModelProjector's defense-in-depth guard plus the transport-level inbox should together produce exactly one OrderReadModel row");
    }
}
