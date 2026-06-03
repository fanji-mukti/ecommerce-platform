using ECommerce.Contracts;

namespace ECommerce.Identity.Commands.V1;

public record IdentityCommandsPlaceholder(
    Guid MessageId,
    Guid CorrelationId,
    Guid CausationId,
    DateTimeOffset OccurredAt
) : IMessageEnvelope;
