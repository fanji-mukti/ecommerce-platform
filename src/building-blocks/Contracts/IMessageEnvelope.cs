namespace ECommerce.Contracts;

public interface IMessageEnvelope
{
    Guid MessageId { get; }
    Guid CorrelationId { get; }
    Guid CausationId { get; }
    DateTimeOffset OccurredAt { get; }
}
