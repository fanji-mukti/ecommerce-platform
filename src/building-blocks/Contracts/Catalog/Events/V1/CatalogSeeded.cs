using ECommerce.Contracts;

namespace ECommerce.Catalog.Events.V1;

public record CatalogSeeded(
    Guid MessageId,
    Guid CorrelationId,
    Guid CausationId,
    DateTimeOffset OccurredAt,
    Guid SeedId,
    int ItemCount,
    DateTimeOffset SeededAt
) : IMessageEnvelope;
