namespace ECommerce.Orders.API.Features.Orders;

/// <summary>
/// Saga-internal scheduled event (CHK-05) — never crosses a service boundary, so this is
/// NOT defined in the shared Contracts library. Correlated by OrderId (the saga's
/// CorrelationId) since it has no CheckoutId field of its own.
/// </summary>
/// <remarks>
/// The explicit parameterless constructor is required by MassTransit's scheduled-message
/// factory (<c>BehaviorContext.Init&lt;T&gt;()</c>, used by the saga's <c>.Schedule()</c>
/// activity) — it instantiates T via a default constructor before populating members from
/// the supplied values object; a positional record with only its primary constructor has
/// no default constructor and fails at runtime with "No default constructor available for
/// message type".
/// </remarks>
public record CheckoutTimeoutExpired(Guid OrderId)
{
    public CheckoutTimeoutExpired() : this(default(Guid)) { }
}
