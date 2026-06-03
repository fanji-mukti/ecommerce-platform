using ECommerce.Contracts;

namespace ECommerce.Catalog.Commands.V1;

public record CatalogCommandsPlaceholder(
    Guid MessageId,
    Guid CorrelationId,
    Guid CausationId,
    DateTimeOffset OccurredAt
) : IMessageEnvelope;
