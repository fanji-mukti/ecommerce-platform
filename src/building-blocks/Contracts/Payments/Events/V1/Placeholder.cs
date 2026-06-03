using ECommerce.Contracts;

namespace ECommerce.Payments.Events.V1;

public record PaymentsServiceReady(
    Guid MessageId,
    Guid CorrelationId,
    Guid CausationId,
    DateTimeOffset OccurredAt
) : IMessageEnvelope;
