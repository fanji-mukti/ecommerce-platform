using ECommerce.Fulfillment.Events.V1;
using ECommerce.Notifications.API.Data;
using ECommerce.Notifications.API.Features.Notifications;
using MassTransit;

namespace ECommerce.Notifications.API.Consumers;

public class OrderShippedNotificationConsumer(NotificationsDbContext db, ILogger<OrderShippedNotificationConsumer> logger)
    : IConsumer<OrderShipped>
{
    public async Task Consume(ConsumeContext<OrderShipped> context)
    {
        var msg = context.Message;

        logger.LogInformation(
            "OrderShipped received: CheckoutId={CheckoutId}, UserId={UserId}",
            msg.CheckoutId,
            msg.UserId);

        db.NotificationEntries.Add(new NotificationEntry
        {
            Id = Guid.NewGuid(),
            UserId = msg.UserId,
            OrderId = msg.CheckoutId,
            Message = "Your order has shipped.",
            EventType = "OrderShipped",
            OccurredAt = msg.ShippedAt
        });

        await db.SaveChangesAsync(context.CancellationToken);
    }
}
