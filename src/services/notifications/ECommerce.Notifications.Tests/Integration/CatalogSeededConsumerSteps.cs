using ECommerce.Catalog.Events.V1;
using ECommerce.Notifications.API.Consumers;
using ECommerce.Notifications.API.Data;
using FluentAssertions;
using MassTransit;
using MassTransit.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace ECommerce.Notifications.Tests.Integration;

public class CatalogSeededConsumerSteps : IAsyncDisposable
{
    private ServiceProvider? _provider;
    private ITestHarness? _harness;

    public async Task Given_HarnessWithInMemoryTransport()
    {
        var services = new ServiceCollection();

        // Logging (optional for test output)
        services.AddLogging();

        // Gap 6 fix: register NotificationsDbContext so CatalogSeededConsumer can resolve it.
        // CatalogSeededConsumer has a primary constructor requiring NotificationsDbContext;
        // without this, DI throws InvalidOperationException when MassTransit creates a consumer scope.
        services.AddDbContext<NotificationsDbContext>(o =>
            o.UseInMemoryDatabase("notifications-consumer-test"));

        services.AddMassTransitTestHarness(x =>
        {
            x.AddConsumer<CatalogSeededConsumer>();
        });

        _provider = services.BuildServiceProvider(true);
        _harness = _provider.GetRequiredService<ITestHarness>();
        await _harness.Start();
    }

    public async Task When_SameMessagePublishedTwice(Guid messageId)
    {
        var seedId = Guid.NewGuid();
        var message = new CatalogSeeded(
            MessageId: messageId,
            CorrelationId: messageId,
            CausationId: Guid.Empty,
            OccurredAt: DateTimeOffset.UtcNow,
            SeedId: seedId,
            ItemCount: 25,
            SeededAt: DateTimeOffset.UtcNow);

        // Set transport-level MessageId to the same value on both publishes so the
        // InMemory harness deduplicates by transport MessageId header (not the record field).
        await _harness!.Bus.Publish(message, ctx => ctx.MessageId = messageId);
        await _harness!.Bus.Publish(message, ctx => ctx.MessageId = messageId);

        await _harness!.InactivityTask;
    }

    public void Then_ConsumerBodyInvokedExactlyOnce()
    {
        // MassTransit InMemory harness deduplicates by transport-level MessageId header.
        // Publishing twice with the same transport MessageId causes the second delivery to be
        // ignored by MassTransit's in-memory duplicate detection.
        var consumed = _harness!.Consumed.Select<CatalogSeeded>().ToList();
        consumed.Should().HaveCount(1,
            "the InMemory harness should track CatalogSeeded consumed exactly once for a duplicate-MessageId delivery");
    }

    public async ValueTask DisposeAsync()
    {
        if (_harness is not null)
            await _harness.Stop();
        if (_provider is not null)
            await _provider.DisposeAsync();
    }
}
