using ECommerce.Contracts;

namespace ECommerce.Orders.Events.V1;

public record OrderCreated(
    Guid MessageId,
    Guid CorrelationId,
    Guid CausationId,
    DateTimeOffset OccurredAt,
    Guid OrderId,
    string UserId,
    IReadOnlyList<OrderLineItemData> LineItems,
    decimal TotalAmount,
    DateTimeOffset CreatedAt,
    bool SimulatePaymentFailure = false
) : IMessageEnvelope;

public record OrderLineItemData(
    Guid ProductId,
    string ProductName,
    decimal UnitPrice,
    int Quantity
);
