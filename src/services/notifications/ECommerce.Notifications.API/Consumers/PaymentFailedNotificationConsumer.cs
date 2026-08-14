using ECommerce.Notifications.API.Data;
using ECommerce.Notifications.API.Features.Notifications;
using ECommerce.Payments.Events.V1;
using MassTransit;

namespace ECommerce.Notifications.API.Consumers;

public class PaymentFailedNotificationConsumer(NotificationsDbContext db, ILogger<PaymentFailedNotificationConsumer> logger)
    : IConsumer<PaymentFailed>
{
    public async Task Consume(ConsumeContext<PaymentFailed> context)
    {
        var msg = context.Message;

        logger.LogInformation(
            "PaymentFailed received: CheckoutId={CheckoutId}, UserId={UserId}",
            msg.CheckoutId,
            msg.UserId);

        db.NotificationEntries.Add(new NotificationEntry
        {
            Id = Guid.NewGuid(),
            UserId = msg.UserId,
            OrderId = msg.CheckoutId,
            Message = "Payment failed for your order.",
            EventType = "PaymentFailed",
            OccurredAt = msg.FailedAt
        });

        await db.SaveChangesAsync(context.CancellationToken);
    }
}
