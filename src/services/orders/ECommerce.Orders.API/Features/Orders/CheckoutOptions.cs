namespace ECommerce.Orders.API.Features.Orders;

/// <summary>
/// Binds the "Checkout" configuration section (D-04). TimeoutMinutes is a double
/// (not int) specifically so integration/unit tests can inject sub-minute overrides
/// (e.g. 0.05 = 3 seconds) for the CHK-05 real-time timeout proof, without a second
/// config key.
/// </summary>
public class CheckoutOptions
{
    public const string SectionName = "Checkout";

    public double TimeoutMinutes { get; set; } = 15;
}
