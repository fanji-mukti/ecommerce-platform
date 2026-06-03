using ECommerce.Contracts;

namespace ECommerce.Fulfillment.Commands.V1;

public record FulfillmentCommandsPlaceholder(
    Guid MessageId,
    Guid CorrelationId,
    Guid CausationId,
    DateTimeOffset OccurredAt
) : IMessageEnvelope;
