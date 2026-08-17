namespace ECommerce.Fulfillment.API.Features.Fulfillment;

/// <summary>
/// Binds the "Fulfillment" configuration section (D-01). ProcessingSeconds is a double
/// (not int) specifically so integration/unit tests can inject sub-second overrides,
/// matching CheckoutOptions.TimeoutMinutes's established double-for-sub-unit-test-overrides
/// rationale, without a second config key.
/// </summary>
public class FulfillmentOptions
{
    public const string SectionName = "Fulfillment";

    public double ProcessingSeconds { get; set; } = 45;
}
