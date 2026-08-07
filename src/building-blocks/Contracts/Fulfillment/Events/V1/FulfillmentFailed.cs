using ECommerce.Contracts;

namespace ECommerce.Fulfillment.Events.V1;

public record FulfillmentFailed(
    Guid MessageId,
    Guid CorrelationId,
    Guid CausationId,
    DateTimeOffset OccurredAt,
    Guid CheckoutId,
    string Reason,
    DateTimeOffset FailedAt
) : IMessageEnvelope;
