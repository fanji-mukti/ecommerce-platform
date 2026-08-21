using ECommerce.Fulfillment.API.Data;
using ECommerce.Fulfillment.API.Features.Fulfillment;
using ECommerce.Fulfillment.Events.V1;
using ECommerce.Orders.Events.V1;
using FluentAssertions;
using MassTransit;
using MassTransit.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace ECommerce.Fulfillment.Tests.Integration;

public class OrderPaidConsumerSteps : IAsyncDisposable
{
    private ServiceProvider? _provider;
    private ITestHarness? _harness;
    private Task<ConsumeContext<OrderShipped>>? _orderShippedHandlerTask;

    public async Task Given_HarnessWithInMemoryScheduler()
    {
        var services = new ServiceCollection();

        services.AddLogging();

        // FulfillmentDbContext registration mirrors CatalogSeededConsumerSteps's Gap 6 fix —
        // OrderPaidConsumer doesn't inject the DbContext directly, but MassTransit's outbox/inbox
        // wiring is not exercised by this InMemory harness (no AddEntityFrameworkOutbox call here);
        // this suite only proves filter/scheduling behavior, not idempotency (see Task 2 for that).
        services.AddDbContext<FulfillmentDbContext>(o =>
            o.UseInMemoryDatabase("fulfillment-consumer-test"));

        // Short ProcessingSeconds so the delayed OrderShipped publish fires almost immediately.
        services.Configure<FulfillmentOptions>(o => o.ProcessingSeconds = 0.1);

        Quartz.ISchedulerFactory? schedulerFactory = null;

        services.AddMassTransitTestHarness(x =>
        {
            x.AddConsumer<OrderPaidConsumer>();

            x.UsingInMemory((context, cfg) =>
            {
                // Rule 1 fix: the parameterless/context-only overloads of UseInMemoryScheduler()
                // do successfully wire ScheduleMessageConsumer to the "quartz" receive endpoint
                // (verified via a temporary IReceiveObserver during investigation — the
                // ScheduleMessage command IS consumed without fault), but the delayed redelivery
                // is a raw SerializedMessageBody Send to the OrderShipped topic address — it never
                // shows up in the harness's typed Published<T>/Sent<T> collections. Fulfillment has
                // no IConsumer<OrderShipped> (only Orders' saga consumes it in production), so the
                // only reliable way to observe the actual delayed delivery in this harness is
                // ConnectPublishHandler<OrderShipped>, registered below before the triggering
                // message is published (see Then_OrderShippedScheduledWithCheckoutIdAndUserId).
                cfg.UseInMemoryScheduler(out schedulerFactory);
                cfg.ConfigureEndpoints(context);
            });
        });

        _provider = services.BuildServiceProvider(true);
        _harness = _provider.GetRequiredService<ITestHarness>();

        _orderShippedHandlerTask = await _harness.ConnectPublishHandler<OrderShipped>(_ => true);

        await _harness.Start();

        // Belt-and-braces: UseInMemoryScheduler's embedded Quartz scheduler is documented to
        // auto-start (QuartzSchedulerOptions.StartScheduler defaults to true), but explicitly
        // confirm/start it here so this test doesn't silently depend on that default surviving
        // a future MassTransit.Quartz upgrade.
        var scheduler = await schedulerFactory!.GetScheduler();
        if (!scheduler.IsStarted)
            await scheduler.Start();
    }

    public async Task When_OrderStatusChangedPublished(Guid orderId, string userId, string newStatus)
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
            ChangedAt: DateTimeOffset.UtcNow);

        await _harness!.Bus.Publish(message);

        await _harness!.InactivityTask;
    }

    public async Task Then_OrderShippedScheduledWithCheckoutIdAndUserId(Guid expectedCheckoutId, string expectedUserId)
    {
        var received = await _orderShippedHandlerTask!.WaitAsync(TimeSpan.FromSeconds(10));

        received.Message.CheckoutId.Should().Be(expectedCheckoutId,
            "OrderPaidConsumer should schedule OrderShipped carrying the OrderStatusChanged's OrderId as CheckoutId");
        received.Message.UserId.Should().Be(expectedUserId,
            "OrderPaidConsumer should schedule OrderShipped carrying the OrderStatusChanged's UserId");
    }

    public async Task Then_NoOrderShippedScheduled()
    {
        // No SchedulePublish call is made for a non-Paid NewStatus, so the publish handler task
        // should never complete — wait past the configured ProcessingSeconds delay (0.1s) to rule
        // out a false negative from checking too early, then confirm it is still pending.
        var completed = await Task.WhenAny(_orderShippedHandlerTask!, Task.Delay(TimeSpan.FromMilliseconds(500)));

        completed.Should().NotBe(_orderShippedHandlerTask,
            "OrderPaidConsumer should not schedule OrderShipped when NewStatus is not Paid");
    }

    public async ValueTask DisposeAsync()
    {
        if (_harness is not null)
            await _harness.Stop();
        if (_provider is not null)
            await _provider.DisposeAsync();
    }
}
