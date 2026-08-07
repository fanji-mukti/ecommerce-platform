namespace ECommerce.Tests.Common.Builders;

/// <summary>
/// Portable cart line-item data record used in test builders.
/// Service integration tests map this to the service's own cart line-item shape by matching property names.
/// </summary>
public record CartLineData(
    Guid ProductId,
    string ProductName,
    decimal UnitPrice,
    int Quantity);

/// <summary>
/// Fluent test data builder for cart line-item data.
/// </summary>
public class CartBuilder
{
    private Guid _productId = Guid.NewGuid();
    private string _productName = "Test Product";
    private decimal _unitPrice = 9.99m;
    private int _quantity = 1;

    public CartBuilder WithProductId(Guid productId)
    {
        _productId = productId;
        return this;
    }

    public CartBuilder WithProductName(string productName)
    {
        _productName = productName;
        return this;
    }

    public CartBuilder WithUnitPrice(decimal unitPrice)
    {
        _unitPrice = unitPrice;
        return this;
    }

    public CartBuilder WithQuantity(int quantity)
    {
        _quantity = quantity;
        return this;
    }

    public CartLineData Build() => new(
        ProductId: _productId,
        ProductName: _productName,
        UnitPrice: _unitPrice,
        Quantity: _quantity);
}
