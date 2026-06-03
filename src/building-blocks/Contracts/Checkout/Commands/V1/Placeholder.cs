using ECommerce.Contracts;

namespace ECommerce.Checkout.Commands.V1;

public record CheckoutCommandsPlaceholder(
    Guid MessageId,
    Guid CorrelationId,
    Guid CausationId,
    DateTimeOffset OccurredAt
) : IMessageEnvelope;
