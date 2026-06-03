using ECommerce.Contracts;

namespace ECommerce.Orders.Commands.V1;

public record OrdersCommandsPlaceholder(
    Guid MessageId,
    Guid CorrelationId,
    Guid CausationId,
    DateTimeOffset OccurredAt
) : IMessageEnvelope;
