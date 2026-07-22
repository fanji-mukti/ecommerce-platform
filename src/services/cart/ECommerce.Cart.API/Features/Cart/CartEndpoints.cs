using System.Security.Claims;
using ECommerce.Cart.API.Data;
using FluentValidation;

namespace ECommerce.Cart.API.Features.Cart;

public static class CartEndpoints
{
    public static void Map(WebApplication app)
    {
        app.MapGet("/cart", async (
            ClaimsPrincipal user, ICartStore store, CartMapper mapper, CancellationToken ct) =>
        {
            var userId = GetUserId(user);
            var cart = await store.GetAsync(userId, ct) ?? new CartData(userId, []);
            return Results.Ok(mapper.ToDto(cart));
        }).RequireAuthorization();

        app.MapPost("/cart/items", async (
            AddCartItemRequest request,
            ClaimsPrincipal user,
            IValidator<AddCartItemRequest> validator,
            ICartStore store,
            ICatalogPriceClient catalogClient,
            CartMapper mapper,
            CancellationToken ct) =>
        {
            var validation = await validator.ValidateAsync(request, ct);
            if (!validation.IsValid)
                return Results.ValidationProblem(validation.ToDictionary());

            var userId = GetUserId(user);
            var cart = await store.GetAsync(userId, ct) ?? new CartData(userId, []);

            var existing = cart.Items.FirstOrDefault(i => i.ProductId == request.ProductId);
            if (existing is not null)
            {
                // Increment in place — never re-fetch or overwrite the original snapshot (CART-01/CART-02).
                existing.Quantity += request.Quantity;
            }
            else
            {
                var snapshot = await catalogClient.GetProductAsync(request.ProductId, ct);
                if (snapshot is null)
                    return Results.NotFound(new { error = "Product not found." });

                cart.Items.Add(new CartLineItem
                {
                    ProductId = request.ProductId,
                    ProductName = snapshot.Name,
                    UnitPrice = snapshot.Price,
                    Quantity = request.Quantity
                });
            }

            await store.SaveAsync(cart, ct);
            return Results.Ok(mapper.ToDto(cart));
        }).RequireAuthorization();

        app.MapPatch("/cart/items/{productId:guid}", async (
            Guid productId,
            UpdateCartItemQuantityRequest request,
            ClaimsPrincipal user,
            IValidator<UpdateCartItemQuantityRequest> validator,
            ICartStore store,
            CartMapper mapper,
            CancellationToken ct) =>
        {
            var validation = await validator.ValidateAsync(request, ct);
            if (!validation.IsValid)
                return Results.ValidationProblem(validation.ToDictionary());

            var userId = GetUserId(user);
            var cart = await store.GetAsync(userId, ct);
            var item = cart?.Items.FirstOrDefault(i => i.ProductId == productId);
            if (cart is null || item is null)
                return Results.NotFound(new { error = "Cart item not found." });

            item.Quantity = request.Quantity; // absolute set, not increment
            await store.SaveAsync(cart, ct);
            return Results.Ok(mapper.ToDto(cart));
        }).RequireAuthorization();

        app.MapDelete("/cart/items/{productId:guid}", async (
            Guid productId,
            ClaimsPrincipal user,
            ICartStore store,
            CartMapper mapper,
            CancellationToken ct) =>
        {
            var userId = GetUserId(user);
            var cart = await store.GetAsync(userId, ct);
            var item = cart?.Items.FirstOrDefault(i => i.ProductId == productId);
            if (cart is null || item is null)
                return Results.NotFound(new { error = "Cart item not found." });

            cart.Items.Remove(item);
            await store.SaveAsync(cart, ct);
            return Results.Ok(mapper.ToDto(cart));
        }).RequireAuthorization();

        app.MapDelete("/cart", async (
            ClaimsPrincipal user, ICartStore store, CancellationToken ct) =>
        {
            var userId = GetUserId(user);
            await store.ClearAsync(userId, ct);
            return Results.NoContent();
        }).RequireAuthorization();
    }

    private static string GetUserId(ClaimsPrincipal user) =>
        user.FindFirstValue(ClaimTypes.NameIdentifier)
        ?? user.FindFirstValue("sub")
        ?? throw new InvalidOperationException("Authenticated request missing a user id claim.");
}
