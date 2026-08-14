using System.Security.Claims;
using ECommerce.Fulfillment.Events.V1;
using MassTransit;

namespace ECommerce.Checkout.API.Features.Checkout;

public static class CheckoutEndpoints
{
    public static void Map(WebApplication app)
    {
        // Angular's raw request body for POST /checkout — distinct from the internal StartCheckout
        // contract OrdersClient builds (that one carries envelope fields Angular never supplies).
        app.MapPost("/checkout", async (
            HttpContext httpContext,
            ClaimsPrincipal user,
            [Microsoft.AspNetCore.Mvc.FromBody] StartCheckoutRequest? request,
            IOrdersClient ordersClient,
            CancellationToken ct) =>
        {
            var token = ExtractBearerToken(httpContext);
            var checkoutId = Guid.NewGuid();

            var orderId = await ordersClient.StartCheckoutAsync(
                checkoutId, request?.SimulatePaymentFailure ?? false, token, ct);

            if (orderId is null)
                return Results.BadRequest(new { error = "Cart is empty." });

            return Results.Accepted($"/checkout/{checkoutId}", new { checkoutId });
        }).RequireAuthorization();

        // Always 200 — never propagates Orders' IDOR-safe 404 as a 404. This is the ADR-0009
        // "Started" synthesis: intentionally identical for "not yet created" and "not owned"
        // (T-04-12, information-disclosure mitigation).
        app.MapGet("/checkout/{id:guid}", async (
            Guid id,
            HttpContext httpContext,
            ClaimsPrincipal user,
            IOrdersClient ordersClient,
            CancellationToken ct) =>
        {
            var token = ExtractBearerToken(httpContext);
            var snapshot = await ordersClient.GetStatusAsync(id, token, ct);
            var status = CheckoutStatusMapper.ToCheckoutVocabulary(snapshot?.Status);

            return Results.Ok(new CheckoutStatusDto(id, status, snapshot?.FailureReason));
        }).RequireAuthorization();

        // Demo-only CHK-04 trigger (D-01–D-03). Unlike GET /checkout/{id}, this DOES surface a
        // real 404 for not-found/not-owned — a side-effecting demo trigger must not silently
        // no-op against another user's order (T-04-13, elevation-of-privilege mitigation via the
        // caller's own bearer token forwarded to the same ownership-checked GetStatusAsync call).
        app.MapPost("/checkout/{id:guid}/simulate-fulfillment-failure", async (
            Guid id,
            HttpContext httpContext,
            ClaimsPrincipal user,
            IOrdersClient ordersClient,
            IPublishEndpoint publishEndpoint,
            CancellationToken ct) =>
        {
            var token = ExtractBearerToken(httpContext);
            var snapshot = await ordersClient.GetStatusAsync(id, token, ct);

            if (snapshot is null)
                return Results.NotFound(new { error = "Order not found." });

            if (snapshot.Status != "Paid")
                return Results.BadRequest(new { error = "Order is not in a state that can simulate a fulfillment failure." });

            // WR-01 (04-REVIEW.md): Checkout.API supplies the real, demo-specific fulfillment
            // failure reason here; the saga (OrderStateMachine.During(Paid, When(FulfillmentFailedEvent)))
            // reads ctx.Message.Reason and formats it into the final human-readable
            // Order.FailureReason rather than inventing its own generic string.
            await publishEndpoint.Publish(new FulfillmentFailed(
                Guid.NewGuid(), id, Guid.Empty, DateTimeOffset.UtcNow, id,
                "Warehouse out of stock", DateTimeOffset.UtcNow), ct);

            return Results.Accepted();
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

public record StartCheckoutRequest(bool SimulatePaymentFailure = false);
