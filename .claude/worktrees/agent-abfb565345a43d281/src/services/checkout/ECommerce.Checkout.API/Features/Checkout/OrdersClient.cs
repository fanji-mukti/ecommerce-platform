using System.Net.Http.Headers;
using System.Net.Http.Json;
using ECommerce.Checkout.Commands.V1;

namespace ECommerce.Checkout.API.Features.Checkout;

/// <summary>
/// Checkout -> Orders internal HTTP client (mirrors Orders.API's CartClient pattern, Plan 04-03).
/// Registered as a typed HttpClient pointed at http://orders via Aspire service discovery
/// (Program.cs).
/// </summary>
public class OrdersClient(HttpClient http) : IOrdersClient
{
    public async Task<Guid?> StartCheckoutAsync(Guid checkoutId, bool simulatePaymentFailure, string bearerToken, CancellationToken ct)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "/orders/checkout")
        {
            Content = JsonContent.Create(new StartCheckout(
                Guid.NewGuid(), checkoutId, Guid.Empty, DateTimeOffset.UtcNow, checkoutId, simulatePaymentFailure))
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", bearerToken);

        using var response = await http.SendAsync(request, ct);
        if (!response.IsSuccessStatusCode)
            return null;

        var body = await response.Content.ReadFromJsonAsync<OrdersCheckoutResponse>(cancellationToken: ct);
        return body?.OrderId;
    }

    public async Task<OrderStatusSnapshot?> GetStatusAsync(Guid checkoutId, string bearerToken, CancellationToken ct)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, $"/orders/{checkoutId}");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", bearerToken);

        using var response = await http.SendAsync(request, ct);
        // Includes Orders' IDOR-safe 404 (not-found and not-owned are indistinguishable) — the
        // caller treats a null snapshot as the synthesized "Started" status per ADR-0009, never
        // as an error.
        if (!response.IsSuccessStatusCode)
            return null;

        var body = await response.Content.ReadFromJsonAsync<OrdersStatusResponse>(cancellationToken: ct);
        return body is null ? null : new OrderStatusSnapshot(body.Status, body.FailureReason);
    }

    private record OrdersCheckoutResponse(Guid OrderId);

    private record OrdersStatusResponse(string Status, string? FailureReason);
}
