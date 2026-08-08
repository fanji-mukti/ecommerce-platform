namespace ECommerce.Orders.API.Features.Orders;

/// <summary>
/// Saga-internal scheduled event (CHK-05) — never crosses a service boundary, so this is
/// NOT defined in the shared Contracts library. Correlated by OrderId (the saga's
/// CorrelationId) since it has no CheckoutId field of its own.
/// </summary>
public record CheckoutTimeoutExpired(Guid OrderId);
