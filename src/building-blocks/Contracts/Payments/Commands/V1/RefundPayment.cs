using ECommerce.Contracts;

namespace ECommerce.Payments.Commands.V1;

public record RefundPayment(
    Guid MessageId,
    Guid CorrelationId,
    Guid CausationId,
    DateTimeOffset OccurredAt,
    Guid CheckoutId,
    decimal Amount,
    string Reason
) : IMessageEnvelope;
