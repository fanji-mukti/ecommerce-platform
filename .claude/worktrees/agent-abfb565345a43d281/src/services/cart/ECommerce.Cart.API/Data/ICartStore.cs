using ECommerce.Cart.API.Features.Cart;

namespace ECommerce.Cart.API.Data;

public interface ICartStore
{
    Task<CartData?> GetAsync(string userId, CancellationToken ct);

    Task SaveAsync(CartData cart, CancellationToken ct);

    Task ClearAsync(string userId, CancellationToken ct);
}
