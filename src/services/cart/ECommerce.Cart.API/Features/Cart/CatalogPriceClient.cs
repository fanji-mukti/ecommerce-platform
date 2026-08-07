using System.Net;
using System.Net.Http.Json;

namespace ECommerce.Cart.API.Features.Cart;

/// <summary>
/// Synchronous internal HTTP call to Catalog for a product price/name snapshot (D-04).
/// Not routed through YARP — a direct service-to-service call within the Aspire network.
/// </summary>
public class CatalogPriceClient(HttpClient http) : ICatalogPriceClient
{
    // Minimal DTO mirroring Catalog's ProductDto — only Name/Price are consumed here.
    private record CatalogProductDto(Guid Id, string Name, string Sku, decimal Price, int StockQuantity, string Category, string? ImageUrl);

    public async Task<ProductSnapshot?> GetProductAsync(Guid productId, CancellationToken ct)
    {
        var response = await http.GetAsync($"/products/{productId}", ct);

        if (response.StatusCode == HttpStatusCode.NotFound)
            return null;

        response.EnsureSuccessStatusCode();

        var product = await response.Content.ReadFromJsonAsync<CatalogProductDto>(ct);
        return product is null ? null : new ProductSnapshot(product.Name, product.Price);
    }
}
