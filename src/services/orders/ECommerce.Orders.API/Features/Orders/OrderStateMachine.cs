using ECommerce.Orders.Events.V1;
using MassTransit;

namespace ECommerce.Orders.API.Features.Orders;

/// <summary>
/// First MassTransit saga state machine in the codebase (ADR-0005). Enforces
/// Pending -> Paid -> Fulfilled / Cancelled / Failed transitions (ORD-03, D-08).
/// Only Pending is reachable via a live endpoint until Phase 4/5 wire the
/// triggering Paid/Fulfilled/Cancelled/Failed events, but the guard logic
/// is real and fully tested (see OrderStateMachineTests).
/// </summary>
public class OrderStateMachine : MassTransitStateMachine<Order>
{
    public State Pending { get; private set; } = null!;
    public State Paid { get; private set; } = null!;
    public State Fulfilled { get; private set; } = null!;
    public State Cancelled { get; private set; } = null!;
    public State Failed { get; private set; } = null!;

    public Event<OrderCreated> OrderCreatedEvent { get; private set; } = null!;
    public Event<OrderStatusChanged> OrderStatusChangedEvent { get; private set; } = null!;

    public OrderStateMachine()
    {
        InstanceState(x => x.CurrentState);

        Event(() => OrderCreatedEvent, x => x.CorrelateById(m => m.Message.OrderId));
        Event(() => OrderStatusChangedEvent, x => x.CorrelateById(m => m.Message.OrderId));

        Initially(
            When(OrderCreatedEvent)
                .Then(ctx =>
                {
                    ctx.Saga.UserId = ctx.Message.UserId;
                    ctx.Saga.LineItems = ctx.Message.LineItems
                        .Select(li => new OrderLineItem
                        {
                            ProductId = li.ProductId,
                            ProductName = li.ProductName,
                            UnitPrice = li.UnitPrice,
                            Quantity = li.Quantity
                        })
                        .ToList();
                    ctx.Saga.TotalAmount = ctx.Message.TotalAmount;
                    ctx.Saga.CreatedAt = ctx.Message.CreatedAt;
                    ctx.Saga.UpdatedAt = ctx.Message.CreatedAt;
                })
                .TransitionTo(Pending));

        // Pending -> Paid / Cancelled / Failed are the only valid transitions out of Pending.
        // The trailing catch-all (no filter, no .TransitionTo()) MUST be declared last — it
        // absorbs any non-matching NewStatus (e.g. "Fulfilled" requested while still Pending)
        // as a handled-but-ignored event, keeping the saga in Pending without an unhandled-event
        // fault being raised (ORD-03, T-03-08).
        During(Pending,
            When(OrderStatusChangedEvent, ctx => ctx.Message.NewStatus == "Paid")
                .Then(ctx => ctx.Saga.UpdatedAt = ctx.Message.ChangedAt)
                .TransitionTo(Paid),
            When(OrderStatusChangedEvent, ctx => ctx.Message.NewStatus == "Cancelled")
                .Then(ctx => ctx.Saga.UpdatedAt = ctx.Message.ChangedAt)
                .TransitionTo(Cancelled),
            When(OrderStatusChangedEvent, ctx => ctx.Message.NewStatus == "Failed")
                .Then(ctx => ctx.Saga.UpdatedAt = ctx.Message.ChangedAt)
                .TransitionTo(Failed),
            When(OrderStatusChangedEvent));

        // Paid -> Fulfilled / Cancelled / Failed, with the same trailing catch-all pattern.
        During(Paid,
            When(OrderStatusChangedEvent, ctx => ctx.Message.NewStatus == "Fulfilled")
                .Then(ctx => ctx.Saga.UpdatedAt = ctx.Message.ChangedAt)
                .TransitionTo(Fulfilled),
            When(OrderStatusChangedEvent, ctx => ctx.Message.NewStatus == "Cancelled")
                .Then(ctx => ctx.Saga.UpdatedAt = ctx.Message.ChangedAt)
                .TransitionTo(Cancelled),
            When(OrderStatusChangedEvent, ctx => ctx.Message.NewStatus == "Failed")
                .Then(ctx => ctx.Saga.UpdatedAt = ctx.Message.ChangedAt)
                .TransitionTo(Failed),
            When(OrderStatusChangedEvent));

        SetCompletedWhenFinalized();
    }
}
