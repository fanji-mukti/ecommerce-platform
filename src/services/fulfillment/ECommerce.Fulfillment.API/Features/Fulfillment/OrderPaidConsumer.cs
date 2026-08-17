using ECommerce.Fulfillment.Events.V1;
using ECommerce.Orders.Events.V1;
using MassTransit;
using Microsoft.Extensions.Options;

namespace ECommerce.Fulfillment.API.Features.Fulfillment;

public class OrderPaidConsumer(IOptions<FulfillmentOptions> options) : IConsumer<OrderStatusChanged>
{
    public async Task Consume(ConsumeContext<OrderStatusChanged> context)
    {
        var msg = context.Message;

        // Fulfillment cares only about the Paid transition — every other NewStatus (Cancelled,
        // Failed, Fulfilled) is a no-op here, mirroring the filter idiom OrderStateMachine
        // already uses for the same event (When(OrderStatusChangedEvent, ctx => ctx.Message
        // .NewStatus == "Paid")). The message is still acked/inbox-recorded either way.
        if (msg.NewStatus != "Paid")
            return;

        var delay = TimeSpan.FromSeconds(options.Value.ProcessingSeconds);
        var now = DateTimeOffset.UtcNow;

        await context.SchedulePublish(delay, new OrderShipped(
            MessageId: Guid.NewGuid(),
            CorrelationId: msg.OrderId,
            CausationId: msg.MessageId,
            OccurredAt: now,
            CheckoutId: msg.OrderId,
            UserId: msg.UserId,
            ShippedAt: now + delay));
    }
}
