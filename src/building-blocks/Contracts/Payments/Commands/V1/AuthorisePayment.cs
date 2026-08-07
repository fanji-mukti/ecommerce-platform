using ECommerce.Contracts;

namespace ECommerce.Payments.Commands.V1;

public record AuthorisePayment(
    Guid MessageId,
    Guid CorrelationId,
    Guid CausationId,
    DateTimeOffset OccurredAt,
    Guid CheckoutId,
    decimal Amount,
    bool SimulatePaymentFailure = false
) : IMessageEnvelope;
