using System.Text.Json;
using ECommerce.Cart.API.Features.Cart;
using StackExchange.Redis;

namespace ECommerce.Cart.API.Data;

/// <summary>
/// Redis-backed cart persistence. Key structure: cart:{userId} (D-03), no TTL —
/// the cart persists until explicitly cleared.
/// </summary>
public class RedisCartStore(IConnectionMultiplexer redis) : ICartStore
{
    private const string KeyPrefix = "cart:";

    private static string KeyFor(string userId) => $"{KeyPrefix}{userId}";

    public async Task<CartData?> GetAsync(string userId, CancellationToken ct)
    {
        var db = redis.GetDatabase();
        var value = await db.StringGetAsync(KeyFor(userId));
        if (!value.HasValue)
            return null;

        return JsonSerializer.Deserialize<CartData>((string)value!);
    }

    public async Task SaveAsync(CartData cart, CancellationToken ct)
    {
        var db = redis.GetDatabase();
        var json = JsonSerializer.Serialize(cart);
        await db.StringSetAsync(KeyFor(cart.UserId), json);
    }

    public async Task ClearAsync(string userId, CancellationToken ct)
    {
        var db = redis.GetDatabase();
        await db.KeyDeleteAsync(KeyFor(userId));
    }
}
