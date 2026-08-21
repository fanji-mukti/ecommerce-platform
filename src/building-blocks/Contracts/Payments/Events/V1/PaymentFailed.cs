using ECommerce.Contracts;

namespace ECommerce.Payments.Events.V1;

public record PaymentFailed(
    Guid MessageId,
    Guid CorrelationId,
    Guid CausationId,
    DateTimeOffset OccurredAt,
    Guid CheckoutId,
    string UserId,
    decimal Amount,
    string Reason,
    DateTimeOffset FailedAt
) : IMessageEnvelope;
