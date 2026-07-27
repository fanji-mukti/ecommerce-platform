namespace ECommerce.Orders.API.Features.Orders;

/// <summary>
/// Orders -> Cart synchronous HTTP client. Every call forwards the caller's own bearer token
/// (not a service credential) so Cart.API's normal auth/ownership scoping applies unchanged.
/// </summary>
public interface ICartClient
{
    Task<CartSnapshot?> GetCartAsync(string bearerToken, CancellationToken ct);

    Task ClearCartAsync(string bearerToken, CancellationToken ct);
}

/// <summary>
/// Mirrors the shape of Cart.API's GET /cart response (Plan 03-01). Only the fields Orders
/// needs (Items, GrandTotal) are declared — unknown JSON members (e.g. ItemCount) are ignored
/// by System.Text.Json's default deserialization.
/// </summary>
public record CartSnapshot(List<CartLineItemSnapshot> Items, decimal GrandTotal);

public record CartLineItemSnapshot(Guid ProductId, string ProductName, decimal UnitPrice, int Quantity);
