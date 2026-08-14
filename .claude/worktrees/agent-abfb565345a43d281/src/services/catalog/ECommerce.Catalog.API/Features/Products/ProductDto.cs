namespace ECommerce.Catalog.API.Features.Products;

public record ProductDto(
    Guid Id,
    string Name,
    string Sku,
    decimal Price,
    int StockQuantity,
    string Category,
    string? ImageUrl);
