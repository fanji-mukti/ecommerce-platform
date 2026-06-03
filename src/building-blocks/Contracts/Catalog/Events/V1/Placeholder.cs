using ECommerce.Contracts;

namespace ECommerce.Catalog.Events.V1;

public record CatalogServiceReady(
    Guid MessageId,
    Guid CorrelationId,
    Guid CausationId,
    DateTimeOffset OccurredAt
) : IMessageEnvelope;
