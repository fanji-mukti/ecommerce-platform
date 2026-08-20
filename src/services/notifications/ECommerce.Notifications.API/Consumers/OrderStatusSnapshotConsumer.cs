using ECommerce.Notifications.API.Data;
using ECommerce.Notifications.API.Features.Notifications;
using ECommerce.Orders.Events.V1;
using MassTransit;

namespace ECommerce.Notifications.API.Consumers;

/// <summary>
/// Consumes every OrderStatusChanged transition (unfiltered — no NewStatus check) to maintain a
/// locally-owned snapshot of each order's latest known status. This lets Notifications answer
/// "what is this order's current status" without a synchronous call back to Orders, preserving
/// D-03/PROJECT.md's no-direct-coupling premise. Consumed by OrderShippedNotificationConsumer to
/// suppress false "shipped" notifications for orders that were subsequently cancelled/failed.
/// </summary>
public class OrderStatusSnapshotConsumer(NotificationsDbContext db, ILogger<OrderStatusSnapshotConsumer> logger)
    : IConsumer<OrderStatusChanged>
{
    public async Task Consume(ConsumeContext<OrderStatusChanged> context)
    {
        var msg = context.Message;

        var existing = await db.OrderStatusSnapshots.FindAsync([msg.OrderId], context.CancellationToken);

        if (existing is null)
        {
            db.OrderStatusSnapshots.Add(new OrderStatusSnapshot
            {
                OrderId = msg.OrderId,
                Status = msg.NewStatus,
                UpdatedAt = msg.ChangedAt
            });
        }
        else if (msg.ChangedAt >= existing.UpdatedAt)
        {
            existing.Status = msg.NewStatus;
            existing.UpdatedAt = msg.ChangedAt;
        }
        else
        {
            logger.LogInformation(
                "Skipped stale/out-of-order OrderStatusChanged for OrderId={OrderId}: incoming ChangedAt={IncomingChangedAt} is older than known UpdatedAt={KnownUpdatedAt}",
                msg.OrderId,
                msg.ChangedAt,
                existing.UpdatedAt);
        }

        await db.SaveChangesAsync(context.CancellationToken);
    }
}
