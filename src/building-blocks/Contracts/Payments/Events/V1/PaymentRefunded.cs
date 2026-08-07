using ECommerce.Contracts;

namespace ECommerce.Payments.Events.V1;

public record PaymentRefunded(
    Guid MessageId,
    Guid CorrelationId,
    Guid CausationId,
    DateTimeOffset OccurredAt,
    Guid CheckoutId,
    decimal Amount,
    DateTimeOffset RefundedAt
) : IMessageEnvelope;
