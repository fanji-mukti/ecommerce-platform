namespace ECommerce.Notifications.API.Features.Notifications;

public class OrderStatusSnapshot
{
    public Guid OrderId { get; set; }
    public string Status { get; set; } = default!;
    public DateTimeOffset UpdatedAt { get; set; }
}
