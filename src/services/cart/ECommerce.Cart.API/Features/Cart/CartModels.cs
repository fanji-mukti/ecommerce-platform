namespace ECommerce.Cart.API.Features.Cart;

/// <summary>
/// Redis-persisted cart aggregate for a single user. Mutable — Items is a List so
/// line-item quantities can be updated in place.
/// </summary>
public record CartData(string UserId, List<CartLineItem> Items);

/// <summary>
/// A single cart line item. ProductName/UnitPrice are server-captured snapshots (D-04/D-05) —
/// never re-fetched from Catalog for totals, and never re-written on a repeat add (CART-02).
/// </summary>
public class CartLineItem
{
    public required Guid ProductId { get; set; }
    public required string ProductName { get; set; }
    public required decimal UnitPrice { get; set; }
    public required int Quantity { get; set; }
}

/// <summary>Cart summary DTO returned to the client — totals computed server-side (CART-03).</summary>
public record CartDto(List<CartLineItemDto> Items, int ItemCount, decimal GrandTotal);

public record CartLineItemDto(Guid ProductId, string ProductName, decimal UnitPrice, int Quantity, decimal LineTotal);
