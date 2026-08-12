using System.Security.Claims;
using ECommerce.Checkout.Commands.V1;
using ECommerce.Orders.API.Data;
using ECommerce.Orders.Events.V1;
using MassTransit;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Orders.API.Features.Orders;

public static class OrdersEndpoints
{
    public static void Map(WebApplication app)
    {
        app.MapGet("/orders", async (
            [Microsoft.AspNetCore.Mvc.FromQuery] int page,
            [Microsoft.AspNetCore.Mvc.FromQuery] int pageSize,
            ClaimsPrincipal user,
            OrdersDbContext db,
            OrderMapper mapper,
            CancellationToken ct) =>
        {
            // Input validation — clamp bounds (ASVS V5, mirrors Catalog's ProductsEndpoints).
            (page, pageSize) = PaginationHelper.Clamp(page, pageSize);
            var userId = GetUserId(user);

            var query = db.OrderReadModels.Where(o => o.UserId == userId);

            var total = await query.CountAsync(ct);
            var orders = await query
                // Deterministic pagination even on CreatedAt collisions (ORD-01).
                .OrderByDescending(o => o.CreatedAt)
                .ThenBy(o => o.Id)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(ct);

            var items = orders.Select(mapper.ToSummaryDto).ToList();
            return Results.Ok(new { Items = items, TotalCount = total, Page = page, PageSize = pageSize });
        }).RequireAuthorization();

        app.MapGet("/orders/{id:guid}", async (
            Guid id,
            ClaimsPrincipal user,
            OrdersDbContext db,
            OrderMapper mapper,
            CancellationToken ct) =>
        {
            var userId = GetUserId(user);
            var order = await db.OrderReadModels
                .Include(o => o.LineItems)
                .FirstOrDefaultAsync(o => o.Id == id, ct);

            // Same 404 whether the order doesn't exist or belongs to another user —
            // no existence leak distinguishes the two cases (ORD-02, IDOR-safe, T-03-10).
            if (order is null || order.UserId != userId)
                return Results.NotFound(new { error = "Order not found." });

            return Results.Ok(mapper.ToDto(order));
        }).RequireAuthorization();

        // Real saga-driven checkout entry point (Phase 4). Replaces Phase 3's demo-only
        // order-creation trigger endpoint (D-01/D-02). The caller-minted checkoutId (from
        // Checkout.API) is reused directly as the OrderId/saga CorrelationId so no separate
        // id-minting round trip is needed.
        app.MapPost("/orders/checkout", async (
            [Microsoft.AspNetCore.Mvc.FromBody] StartCheckout request,
            HttpContext httpContext,
            ClaimsPrincipal user,
            ICartClient cartClient,
            IPublishEndpoint publishEndpoint,
            OrdersDbContext db,
            CancellationToken ct) =>
        {
            var userId = GetUserId(user);
            var token = ExtractBearerToken(httpContext);

            var cart = await cartClient.GetCartAsync(token, ct);
            if (cart is null || cart.Items.Count == 0)
                return Results.BadRequest(new { error = "Cart is empty." });

            // Reuse the caller-minted checkoutId as the OrderId/saga CorrelationId — never
            // regenerate it here (T-04-07: worst case is a self-chosen id, no cross-user impact
            // since UserId is still derived from the JWT claim, not the request body).
            var orderId = request.CheckoutId;
            var now = DateTimeOffset.UtcNow;
            var totalAmount = cart.Items.Sum(i => i.UnitPrice * i.Quantity);

            await publishEndpoint.Publish(new OrderCreated(
                MessageId: Guid.NewGuid(),
                CorrelationId: orderId,
                CausationId: request.MessageId,
                OccurredAt: now,
                OrderId: orderId,
                UserId: userId,
                LineItems: cart.Items
                    .Select(i => new OrderLineItemData(i.ProductId, i.ProductName, i.UnitPrice, i.Quantity))
                    .ToList(),
                TotalAmount: totalAmount,
                CreatedAt: now,
                SimulatePaymentFailure: request.SimulatePaymentFailure), ct);

            // Flush the transactional outbox (same OrdersDbContext.SaveChangesAsync call) BEFORE
            // clearing the cart — CART-04 must only fire once OrderCreated is durably published.
            await db.SaveChangesAsync(ct);

            await cartClient.ClearCartAsync(token, ct);

            return Results.Accepted($"/orders/{orderId}", new { orderId });
        }).RequireAuthorization();
    }

    private static string GetUserId(ClaimsPrincipal user) =>
        user.FindFirstValue(ClaimTypes.NameIdentifier)
        ?? user.FindFirstValue("sub")
        ?? throw new InvalidOperationException("Authenticated request missing a user id claim.");

    private static string ExtractBearerToken(HttpContext httpContext)
    {
        var header = httpContext.Request.Headers.Authorization.ToString();
        return header.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase)
            ? header["Bearer ".Length..]
            : header;
    }
}
