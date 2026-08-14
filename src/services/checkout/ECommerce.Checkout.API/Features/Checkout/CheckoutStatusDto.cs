namespace ECommerce.Checkout.API.Features.Checkout;

/// <summary>Response shape for GET /checkout/{id}. Never exposes the raw saga CurrentState.</summary>
public record CheckoutStatusDto(Guid CheckoutId, string Status, string? FailureReason);

/// <summary>
/// ADR-0009's checkout-facing vocabulary mapping table. Orders.API's persisted saga vocabulary
/// (Pending/Paid/Cancelled/Failed/Fulfilled) stays unchanged (ORD-03) — this mapper translates it
/// to the roadmap's checkout-facing vocabulary (Started/AwaitingPayment/Paid/Cancelled/Failed) at
/// the HTTP layer only. A null input represents Orders' IDOR-safe 404 (either the saga row hasn't
/// been projected yet, or the checkoutId is not owned by the caller — both synthesize "Started").
/// </summary>
internal static class CheckoutStatusMapper
{
    public static string ToCheckoutVocabulary(string? ordersStatus) => ordersStatus switch
    {
        null => "Started",
        "Pending" => "AwaitingPayment",
        "Paid" => "Paid",
        "Cancelled" => "Cancelled",
        "Failed" => "Failed",
        "Fulfilled" => "Fulfilled",
        _ => ordersStatus
    };
}
