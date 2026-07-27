using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace ECommerce.Orders.API.Features.Orders;

/// <summary>
/// First internal service-to-service synchronous HTTP call in the codebase originating from
/// Orders (mirrors Cart.API's CatalogPriceClient pattern). Registered as a typed HttpClient
/// pointed at http://cart via Aspire service discovery (Program.cs).
/// </summary>
public class CartClient(HttpClient http) : ICartClient
{
    public async Task<CartSnapshot?> GetCartAsync(string bearerToken, CancellationToken ct)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "/cart");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", bearerToken);

        using var response = await http.SendAsync(request, ct);
        if (!response.IsSuccessStatusCode)
            return null;

        return await response.Content.ReadFromJsonAsync<CartSnapshot>(cancellationToken: ct);
    }

    public async Task ClearCartAsync(string bearerToken, CancellationToken ct)
    {
        using var request = new HttpRequestMessage(HttpMethod.Delete, "/cart");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", bearerToken);

        using var response = await http.SendAsync(request, ct);
        response.EnsureSuccessStatusCode();
    }
}
