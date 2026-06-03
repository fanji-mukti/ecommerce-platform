using ECommerce.Contracts;

namespace ECommerce.Cart.Commands.V1;

public record CartCommandsPlaceholder(
    Guid MessageId,
    Guid CorrelationId,
    Guid CausationId,
    DateTimeOffset OccurredAt
) : IMessageEnvelope;
