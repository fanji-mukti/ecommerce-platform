namespace ECommerce.Payments.API.Features.Payments;

public class ProcessedPayment
{
    public Guid CheckoutId { get; set; }
    public string Outcome { get; set; } = default!;
    public decimal Amount { get; set; }
    public string? FailureReason { get; set; }
    public DateTimeOffset ProcessedAt { get; set; }
}
