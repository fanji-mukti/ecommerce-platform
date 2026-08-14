namespace ECommerce.Orders.API.Features.Orders;

/// <summary>
/// CQRS read-side projection table, populated asynchronously from domain events published
/// through the transactional outbox (D-07). GET /orders and GET /orders/{id} (Plan 03-03)
/// are served exclusively from this table, never from the <see cref="Order"/> write aggregate.
/// </summary>
public class OrderReadModel
{
    public Guid Id { get; set; }
    public string UserId { get; set; } = default!;
    public string Status { get; set; } = default!;
    public decimal TotalAmount { get; set; }
    public int ItemCount { get; set; }
    public List<OrderLineItem> LineItems { get; set; } = new();
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public string? FailureReason { get; set; }
}
