using ECommerce.Contracts;

namespace ECommerce.Notifications.Commands.V1;

public record NotificationsCommandsPlaceholder(
    Guid MessageId,
    Guid CorrelationId,
    Guid CausationId,
    DateTimeOffset OccurredAt
) : IMessageEnvelope;
