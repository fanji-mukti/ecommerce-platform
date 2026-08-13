using Riok.Mapperly.Abstractions;

namespace ECommerce.Cart.API.Features.Cart;

[Mapper]
public partial class CartMapper
{
    // Hand-written (not Mapperly-generated) — ItemCount/GrandTotal/LineTotal are computed
    // server-side from the UnitPrice/Quantity snapshots (CART-03), not a 1:1 field mapping.
    public CartDto ToDto(CartData cart)
    {
        var items = cart.Items
            .Select(i => new CartLineItemDto(i.ProductId, i.ProductName, i.UnitPrice, i.Quantity, i.UnitPrice * i.Quantity))
            .ToList();

        return new CartDto(
            Items: items,
            ItemCount: items.Sum(i => i.Quantity),
            GrandTotal: items.Sum(i => i.LineTotal));
    }
}
