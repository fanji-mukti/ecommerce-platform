using ECommerce.Contracts;

namespace ECommerce.Identity.Events.V1;

public record IdentityServiceReady(
    Guid MessageId,
    Guid CorrelationId,
    Guid CausationId,
    DateTimeOffset OccurredAt
) : IMessageEnvelope;
