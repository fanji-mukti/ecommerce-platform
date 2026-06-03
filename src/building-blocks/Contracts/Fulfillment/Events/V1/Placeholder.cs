using ECommerce.Contracts;

namespace ECommerce.Fulfillment.Events.V1;

public record FulfillmentServiceReady(
    Guid MessageId,
    Guid CorrelationId,
    Guid CausationId,
    DateTimeOffset OccurredAt
) : IMessageEnvelope;
