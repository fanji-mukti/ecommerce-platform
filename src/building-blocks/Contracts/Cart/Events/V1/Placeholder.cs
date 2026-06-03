using ECommerce.Contracts;

namespace ECommerce.Cart.Events.V1;

public record CartServiceReady(
    Guid MessageId,
    Guid CorrelationId,
    Guid CausationId,
    DateTimeOffset OccurredAt
) : IMessageEnvelope;
