using ECommerce.Notifications.API.Data;
using ECommerce.Notifications.API.Features.Notifications;
using ECommerce.Orders.Events.V1;
using MassTransit;

namespace ECommerce.Notifications.API.Consumers;

public class OrderPaidNotificationConsumer(NotificationsDbContext db, ILogger<OrderPaidNotificationConsumer> logger)
    : IConsumer<OrderStatusChanged>
{
    public async Task Consume(ConsumeContext<OrderStatusChanged> context)
    {
        var msg = context.Message;
        if (msg.NewStatus != "Paid")
            return;

        logger.LogInformation(
            "OrderStatusChanged (Paid) received: OrderId={OrderId}, UserId={UserId}",
            msg.OrderId,
            msg.UserId);

        db.NotificationEntries.Add(new NotificationEntry
        {
            Id = Guid.NewGuid(),
            UserId = msg.UserId,
            OrderId = msg.OrderId,
            Message = "Your order has been paid.",
            EventType = "OrderPaid",
            OccurredAt = msg.ChangedAt
        });

        await db.SaveChangesAsync(context.CancellationToken);
    }
}
