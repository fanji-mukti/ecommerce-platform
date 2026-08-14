namespace ECommerce.Tests.Common.Builders;

/// <summary>
/// Portable processed-payment data used in test builders.
/// Service integration tests map this to the service's ProcessedPayment entity by matching
/// property names (mirrors OrderBuilder's pattern).
/// </summary>
public record ProcessedPaymentData(
    Guid CheckoutId,
    string Outcome,
    decimal Amount,
    string? FailureReason,
    DateTimeOffset ProcessedAt);

/// <summary>
/// Fluent test data builder for processed-payment data.
/// </summary>
public class PaymentBuilder
{
    private Guid _checkoutId = Guid.NewGuid();
    private string _outcome = "Authorised";
    private decimal _amount = 25.00m;
    private string? _failureReason;
    private DateTimeOffset _processedAt = DateTimeOffset.UtcNow;

    public PaymentBuilder WithCheckoutId(Guid checkoutId)
    {
        _checkoutId = checkoutId;
        return this;
    }

    public PaymentBuilder WithOutcome(string outcome)
    {
        _outcome = outcome;
        return this;
    }

    public PaymentBuilder WithAmount(decimal amount)
    {
        _amount = amount;
        return this;
    }

    public PaymentBuilder WithFailureReason(string? failureReason)
    {
        _failureReason = failureReason;
        return this;
    }

    public PaymentBuilder WithProcessedAt(DateTimeOffset processedAt)
    {
        _processedAt = processedAt;
        return this;
    }

    public ProcessedPaymentData Build() => new(
        CheckoutId: _checkoutId,
        Outcome: _outcome,
        Amount: _amount,
        FailureReason: _failureReason,
        ProcessedAt: _processedAt);
}
