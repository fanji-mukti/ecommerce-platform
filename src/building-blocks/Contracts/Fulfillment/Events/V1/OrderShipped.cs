using ECommerce.Contracts;

namespace ECommerce.Fulfillment.Events.V1;

public record OrderShipped(
    Guid MessageId,
    Guid CorrelationId,
    Guid CausationId,
    DateTimeOffset OccurredAt,
    Guid CheckoutId,
    string UserId,
    DateTimeOffset ShippedAt
) : IMessageEnvelope;
