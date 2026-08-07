using ECommerce.Contracts;

namespace ECommerce.Checkout.Commands.V1;

public record StartCheckout(
    Guid MessageId,
    Guid CorrelationId,
    Guid CausationId,
    DateTimeOffset OccurredAt,
    Guid CheckoutId,
    bool SimulatePaymentFailure = false
) : IMessageEnvelope;
