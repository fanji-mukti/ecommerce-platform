using ECommerce.Orders.API.Data;
using ECommerce.Orders.Events.V1;
using MassTransit;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Orders.API.Features.Orders;

/// <summary>
/// First MassTransit consumer (as opposed to saga) in the codebase. Asynchronously populates
/// the CQRS read-side <see cref="OrderReadModel"/> table from domain events the write-side
/// saga publishes through the transactional outbox (D-07). MassTransit's EF Core inbox
/// (InboxState, keyed by transport MessageId) already deduplicates redelivery at the transport
/// level; the AnyAsync check below is a defense-in-depth guard at the projection level.
/// </summary>
public class OrderReadModelProjector(OrdersDbContext db)
    : IConsumer<OrderCreated>, IConsumer<OrderStatusChanged>
{
    public async Task Consume(ConsumeContext<OrderCreated> context)
    {
        var msg = context.Message;

        var alreadyProjected = await db.OrderReadModels
            .AnyAsync(o => o.Id == msg.OrderId, context.CancellationToken);
        if (alreadyProjected)
            return;

        var readModel = new OrderReadModel
        {
            Id = msg.OrderId,
            UserId = msg.UserId,
            Status = "Pending",
            TotalAmount = msg.TotalAmount,
            ItemCount = msg.LineItems.Count,
            LineItems = msg.LineItems
                .Select(li => new OrderLineItem
                {
                    ProductId = li.ProductId,
                    ProductName = li.ProductName,
                    UnitPrice = li.UnitPrice,
                    Quantity = li.Quantity
                })
                .ToList(),
            CreatedAt = msg.CreatedAt,
            UpdatedAt = msg.CreatedAt
        };

        db.OrderReadModels.Add(readModel);
        await db.SaveChangesAsync(context.CancellationToken);
    }

    public async Task Consume(ConsumeContext<OrderStatusChanged> context)
    {
        var msg = context.Message;

        var readModel = await db.OrderReadModels
            .FirstOrDefaultAsync(o => o.Id == msg.OrderId, context.CancellationToken);
        if (readModel is null)
            return; // No-op — matching write-side saga instance not yet projected (or unknown).

        readModel.Status = msg.NewStatus;
        readModel.UpdatedAt = msg.ChangedAt;
        await db.SaveChangesAsync(context.CancellationToken);
    }
}
