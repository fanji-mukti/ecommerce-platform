using ECommerce.Contracts;

namespace ECommerce.Payments.Commands.V1;

public record PaymentsCommandsPlaceholder(
    Guid MessageId,
    Guid CorrelationId,
    Guid CausationId,
    DateTimeOffset OccurredAt
) : IMessageEnvelope;
