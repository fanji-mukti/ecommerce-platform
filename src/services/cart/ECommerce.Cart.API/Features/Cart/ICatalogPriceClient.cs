namespace ECommerce.Cart.API.Features.Cart;

/// <summary>Server-side price/name snapshot lookup against Catalog (D-04).</summary>
public interface ICatalogPriceClient
{
    Task<ProductSnapshot?> GetProductAsync(Guid productId, CancellationToken ct);
}

public record ProductSnapshot(string Name, decimal Price);
