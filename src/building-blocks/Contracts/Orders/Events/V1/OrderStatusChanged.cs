using ECommerce.Contracts;

namespace ECommerce.Orders.Events.V1;

public record OrderStatusChanged(
    Guid MessageId,
    Guid CorrelationId,
    Guid CausationId,
    DateTimeOffset OccurredAt,
    Guid OrderId,
    string UserId,
    string PreviousStatus,
    string NewStatus,
    DateTimeOffset ChangedAt,
    string? FailureReason = null
) : IMessageEnvelope;
