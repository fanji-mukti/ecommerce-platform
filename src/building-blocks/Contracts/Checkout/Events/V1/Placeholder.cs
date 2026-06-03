using ECommerce.Contracts;

namespace ECommerce.Checkout.Events.V1;

public record CheckoutServiceReady(
    Guid MessageId,
    Guid CorrelationId,
    Guid CausationId,
    DateTimeOffset OccurredAt
) : IMessageEnvelope;
