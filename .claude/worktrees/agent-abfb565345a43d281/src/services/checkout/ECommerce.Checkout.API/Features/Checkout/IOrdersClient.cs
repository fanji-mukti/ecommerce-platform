namespace ECommerce.Checkout.API.Features.Checkout;

/// <summary>
/// Checkout -> Orders synchronous HTTP client. Every call forwards the caller's own bearer token
/// (not a service credential) so Orders.API's normal auth/ownership scoping applies unchanged.
/// Mirrors Orders.API's ICartClient/CartClient pattern (Plan 04-03).
/// </summary>
public interface IOrdersClient
{
    Task<Guid?> StartCheckoutAsync(Guid checkoutId, bool simulatePaymentFailure, string bearerToken, CancellationToken ct);

    Task<OrderStatusSnapshot?> GetStatusAsync(Guid checkoutId, string bearerToken, CancellationToken ct);
}

/// <summary>
/// Mirrors the fields Checkout.API needs from Orders.API's GET /orders/{id} response (OrderDto).
/// Unknown JSON members are ignored by System.Text.Json's default deserialization.
/// </summary>
public record OrderStatusSnapshot(string Status, string? FailureReason);
