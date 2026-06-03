using ECommerce.Contracts;

namespace ECommerce.Notifications.Events.V1;

public record NotificationsServiceReady(
    Guid MessageId,
    Guid CorrelationId,
    Guid CausationId,
    DateTimeOffset OccurredAt
) : IMessageEnvelope;
