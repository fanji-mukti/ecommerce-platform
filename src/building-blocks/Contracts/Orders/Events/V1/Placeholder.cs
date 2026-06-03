using ECommerce.Contracts;

namespace ECommerce.Orders.Events.V1;

public record OrdersServiceReady(
    Guid MessageId,
    Guid CorrelationId,
    Guid CausationId,
    DateTimeOffset OccurredAt
) : IMessageEnvelope;
