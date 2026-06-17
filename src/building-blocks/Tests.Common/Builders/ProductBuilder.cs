namespace ECommerce.Tests.Common.Builders;

/// <summary>
/// Portable product data record used in test builders.
/// Service integration tests map this to the service's Product entity by matching property names.
/// </summary>
public record ProductData(
    Guid Id,
    string Name,
    string Sku,
    string Description,
    decimal Price,
    int StockQuantity,
    string Category,
    string? ImageUrl,
    DateTimeOffset CreatedAt);

/// <summary>
/// Fluent test data builder for product data.
/// </summary>
public class ProductBuilder
{
    private string _name = "Test Product";
    private string _sku = "TST-001";
    private string _description = "A test product.";
    private decimal _price = 9.99m;
    private int _stock = 100;
    private string _category = "Electronics";
    private string? _imageUrl = null;

    public ProductBuilder WithName(string name)
    {
        _name = name;
        return this;
    }

    public ProductBuilder WithPrice(decimal price)
    {
        _price = price;
        return this;
    }

    public ProductBuilder WithCategory(string category)
    {
        _category = category;
        return this;
    }

    public ProductBuilder WithStock(int stock)
    {
        _stock = stock;
        return this;
    }

    public ProductBuilder WithSku(string sku)
    {
        _sku = sku;
        return this;
    }

    public ProductBuilder WithImageUrl(string? imageUrl)
    {
        _imageUrl = imageUrl;
        return this;
    }

    public ProductData Build() => new(
        Id: Guid.NewGuid(),
        Name: _name,
        Sku: _sku,
        Description: _description,
        Price: _price,
        StockQuantity: _stock,
        Category: _category,
        ImageUrl: _imageUrl,
        CreatedAt: DateTimeOffset.UtcNow);
}
