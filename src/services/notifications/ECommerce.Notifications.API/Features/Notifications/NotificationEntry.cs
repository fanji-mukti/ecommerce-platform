namespace ECommerce.Notifications.API.Features.Notifications;

public class NotificationEntry
{
    public Guid Id { get; set; }
    public string UserId { get; set; } = default!;
    public Guid OrderId { get; set; }
    public string Message { get; set; } = default!;
    public string EventType { get; set; } = default!;
    public DateTimeOffset OccurredAt { get; set; }
}
