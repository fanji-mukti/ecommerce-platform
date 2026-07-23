using MassTransit;

namespace ECommerce.Orders.API.Features.Orders;

/// <summary>
/// Write-side aggregate that also doubles as the MassTransit saga instance for
/// <see cref="OrderStateMachine"/>. <see cref="CorrelationId"/> IS the OrderId
/// throughout the Orders API — there is no separate Id property (D-06).
/// </summary>
public class Order : SagaStateMachineInstance
{
    public Guid CorrelationId { get; set; }
    public string CurrentState { get; set; } = default!;
    public string UserId { get; set; } = default!;
    public List<OrderLineItem> LineItems { get; set; } = new();
    public decimal TotalAmount { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}
