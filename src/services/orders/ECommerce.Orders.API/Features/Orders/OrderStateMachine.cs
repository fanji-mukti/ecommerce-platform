using ECommerce.Fulfillment.Events.V1;
using ECommerce.Orders.Events.V1;
using ECommerce.Payments.Commands.V1;
using ECommerce.Payments.Events.V1;
using MassTransit;
using Microsoft.Extensions.Options;

namespace ECommerce.Orders.API.Features.Orders;

/// <summary>
/// First MassTransit saga state machine in the codebase (ADR-0005). Enforces
/// Pending -> Paid -> Fulfilled / Cancelled / Failed transitions (ORD-03, D-08).
/// Phase 4 extends this in place (ADR-0009 — no new persisted "Started" state):
/// Initially() now publishes AuthorisePayment and schedules the CHK-05 timeout the
/// moment an order is created; Pending reacts to typed PaymentAuthorised/PaymentFailed
/// events (CHK-03) and the scheduled CheckoutTimeoutExpired event (CHK-05); Paid reacts
/// to FulfillmentFailed with a direct Paid->Cancelled refund-and-cancel compensation
/// (CHK-04, Open Question 2 resolved — no intermediate "Refunding" state).
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
    public Event<PaymentAuthorised> PaymentAuthorisedEvent { get; private set; } = null!;
    public Event<PaymentFailed> PaymentFailedEvent { get; private set; } = null!;
    public Event<FulfillmentFailed> FulfillmentFailedEvent { get; private set; } = null!;
    public Event<OrderShipped> OrderShippedEvent { get; private set; } = null!;

    public Schedule<Order, CheckoutTimeoutExpired> CheckoutTimeout { get; private set; } = null!;

    public OrderStateMachine(IOptions<CheckoutOptions> checkoutOptions)
    {
        InstanceState(x => x.CurrentState);

        Event(() => OrderCreatedEvent, x => x.CorrelateById(m => m.Message.OrderId));
        Event(() => OrderStatusChangedEvent, x => x.CorrelateById(m => m.Message.OrderId));
        Event(() => PaymentAuthorisedEvent, x => x.CorrelateById(m => m.Message.CheckoutId));
        Event(() => PaymentFailedEvent, x => x.CorrelateById(m => m.Message.CheckoutId));
        Event(() => FulfillmentFailedEvent, x => x.CorrelateById(m => m.Message.CheckoutId));
        Event(() => OrderShippedEvent, x => x.CorrelateById(m => m.Message.CheckoutId));

        // NOTE: CheckoutTimeoutExpired does NOT get a separate Event<T> declaration/Event()
        // registration — Schedule() below already registers it as CheckoutTimeout.Received
        // (an Event<CheckoutTimeoutExpired>). Declaring both causes a runtime
        // "An item with the same key has already been added" ArgumentException when
        // MassTransit builds the saga's message specification dictionary (duplicate
        // registration for the same message type). Use CheckoutTimeout.Received in When()
        // calls below instead.
        Schedule(() => CheckoutTimeout, instance => instance.CheckoutTimeoutTokenId, s =>
        {
            s.Delay = TimeSpan.FromMinutes(15);
            s.Received = r => r.CorrelateById(context => context.Message.OrderId);
        });

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
                .Publish(ctx => new AuthorisePayment(
                    MessageId: Guid.NewGuid(),
                    CorrelationId: ctx.Saga.CorrelationId,
                    CausationId: ctx.Message.MessageId,
                    OccurredAt: DateTimeOffset.UtcNow,
                    CheckoutId: ctx.Saga.CorrelationId,
                    UserId: ctx.Saga.UserId,
                    Amount: ctx.Message.TotalAmount,
                    SimulatePaymentFailure: ctx.Message.SimulatePaymentFailure))
                .Schedule(CheckoutTimeout,
                    // ctx.Init<T>() populates a NEW T from a values object by matching property
                    // names — it requires an anonymous object here, not a pre-constructed
                    // CheckoutTimeoutExpired instance (records have no parameterless
                    // constructor, so passing one directly throws "No default constructor
                    // available for message type" at runtime).
                    ctx => ctx.Init<CheckoutTimeoutExpired>(new { OrderId = ctx.Saga.CorrelationId }),
                    ctx => TimeSpan.FromMinutes(checkoutOptions.Value.TimeoutMinutes))
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
            // CHK-03: typed payment outcome events drive the real Pending->Paid/Cancelled
            // transitions; they also unschedule the CHK-05 timeout so a late timeout can't
            // fire after payment has already resolved.
            When(PaymentAuthorisedEvent)
                .Unschedule(CheckoutTimeout)
                .Then(ctx => ctx.Saga.UpdatedAt = ctx.Message.AuthorisedAt)
                .Publish(ctx => new OrderStatusChanged(
                    MessageId: Guid.NewGuid(),
                    CorrelationId: ctx.Saga.CorrelationId,
                    CausationId: ctx.Message.MessageId,
                    OccurredAt: DateTimeOffset.UtcNow,
                    OrderId: ctx.Saga.CorrelationId,
                    UserId: ctx.Saga.UserId,
                    PreviousStatus: "Pending",
                    NewStatus: "Paid",
                    ChangedAt: ctx.Message.AuthorisedAt,
                    FailureReason: null))
                .TransitionTo(Paid),
            When(PaymentFailedEvent)
                .Unschedule(CheckoutTimeout)
                .Then(ctx => ctx.Saga.FailureReason = ctx.Message.Reason)
                .Publish(ctx => new OrderStatusChanged(
                    MessageId: Guid.NewGuid(),
                    CorrelationId: ctx.Saga.CorrelationId,
                    CausationId: ctx.Message.MessageId,
                    OccurredAt: DateTimeOffset.UtcNow,
                    OrderId: ctx.Saga.CorrelationId,
                    UserId: ctx.Saga.UserId,
                    PreviousStatus: "Pending",
                    NewStatus: "Cancelled",
                    ChangedAt: ctx.Message.FailedAt,
                    FailureReason: ctx.Message.Reason))
                .TransitionTo(Cancelled),
            // CHK-05: the checkout never received a payment outcome within the configured
            // timeout window — cancel deterministically with a distinct failure reason.
            When(CheckoutTimeout.Received)
                .Then(ctx => ctx.Saga.FailureReason = "Checkout timed out before payment completed")
                .Publish(ctx => new OrderStatusChanged(
                    MessageId: Guid.NewGuid(),
                    CorrelationId: ctx.Saga.CorrelationId,
                    CausationId: Guid.Empty,
                    OccurredAt: DateTimeOffset.UtcNow,
                    OrderId: ctx.Saga.CorrelationId,
                    UserId: ctx.Saga.UserId,
                    PreviousStatus: "Pending",
                    NewStatus: "Cancelled",
                    ChangedAt: DateTimeOffset.UtcNow,
                    FailureReason: ctx.Saga.FailureReason))
                .TransitionTo(Cancelled),
            // 04-07-REVIEW CR-02: OrderCreatedEvent was only bound inside Initially(...) — a
            // redelivered OrderCreated for a saga instance that has already left the Initial
            // pseudo-state (i.e. already Pending) was unhandled and faulted. Absorb rather than
            // fault; the saga instance already exists and was already initialised from the
            // first delivery.
            Ignore(OrderCreatedEvent),
            // 04-VERIFICATION (3rd pass): FulfillmentFailed cannot currently reach a Pending
            // saga (the only publisher is gated behind a Paid-only read-model check), but every
            // other During() block absorbs all six registered event types defensively — this
            // one didn't, which is exactly the reachability-assumption gap that let CR-01/CR-02
            // slip through two prior review passes. Absorb here too, for the same reason.
            Ignore(FulfillmentFailedEvent),
            // 05-03: OrderShipped cannot currently reach a Pending saga (Fulfillment only
            // ships after Paid), but every other During() block absorbs all registered event
            // types defensively — mirrors the FulfillmentFailedEvent rationale immediately
            // above.
            Ignore(OrderShippedEvent),
            // 04-07-REVIEW WR-02: previously a bare When(OrderStatusChangedEvent) with no
            // activity chain. Switched to Ignore(...) for consistency with every other
            // trailing catch-all in this file (Paid/Cancelled/Fulfilled/Failed all use
            // Ignore(...) already) — see the WR-02 note on During(Paid, ...) below for why the
            // original rationale for that split (a claimed bare-When() runtime failure) does
            // not actually hold here.
            Ignore(OrderStatusChangedEvent));

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
            // CHK-04: fulfillment failed after payment succeeded — refund and cancel in one
            // step, direct Paid->Cancelled (Open Question 2 resolved: no intermediate
            // "Refunding" state).
            When(FulfillmentFailedEvent)
                .Then(ctx => ctx.Saga.FailureReason = $"Fulfillment failed — order cancelled and refunded ({ctx.Message.Reason})")
                .Publish(ctx => new RefundPayment(
                    MessageId: Guid.NewGuid(),
                    CorrelationId: ctx.Saga.CorrelationId,
                    CausationId: ctx.Message.MessageId,
                    OccurredAt: DateTimeOffset.UtcNow,
                    CheckoutId: ctx.Saga.CorrelationId,
                    Amount: ctx.Saga.TotalAmount,
                    // Non-null assertion is safe: the preceding .Then() unconditionally sets
                    // ctx.Saga.FailureReason on this exact activity chain before this runs.
                    Reason: ctx.Saga.FailureReason!))
                .Publish(ctx => new OrderStatusChanged(
                    MessageId: Guid.NewGuid(),
                    CorrelationId: ctx.Saga.CorrelationId,
                    CausationId: ctx.Message.MessageId,
                    OccurredAt: DateTimeOffset.UtcNow,
                    OrderId: ctx.Saga.CorrelationId,
                    UserId: ctx.Saga.UserId,
                    PreviousStatus: "Paid",
                    NewStatus: "Cancelled",
                    ChangedAt: ctx.Message.FailedAt,
                    FailureReason: ctx.Saga.FailureReason))
                .TransitionTo(Cancelled),
            // FUL-02/SC1/SC2: Fulfillment publishes OrderShipped once the shipment is
            // dispatched — the real Paid->Fulfilled transition (closes the loop opened by
            // Initially()'s AuthorisePayment publish).
            When(OrderShippedEvent)
                .Then(ctx => ctx.Saga.UpdatedAt = ctx.Message.ShippedAt)
                .Publish(ctx => new OrderStatusChanged(
                    MessageId: Guid.NewGuid(),
                    CorrelationId: ctx.Saga.CorrelationId,
                    CausationId: ctx.Message.MessageId,
                    OccurredAt: DateTimeOffset.UtcNow,
                    OrderId: ctx.Saga.CorrelationId,
                    UserId: ctx.Saga.UserId,
                    PreviousStatus: "Paid",
                    NewStatus: "Fulfilled",
                    ChangedAt: ctx.Message.ShippedAt,
                    FailureReason: null))
                .TransitionTo(Fulfilled),
            // Pitfall 2 (ASB unschedule race, GitHub MassTransit#3753): a scheduled timeout
            // unscheduled very close to its delivery time, or a redelivered payment outcome
            // (PAY-03's idempotent Payments service may legitimately redeliver the same
            // stored outcome), can still arrive after the saga has already moved to Paid.
            // Absorb rather than fault. Ignore(...) is the correct API for "accept and do
            // nothing" here. 04-07-REVIEW WR-02: an earlier version of this comment claimed a
            // bare When(event) with no activity chain does NOT register as accepted and still
            // throws NotAcceptedStateMachineException — that claim is unverified/overstated:
            // During(Pending, ...)'s own trailing bare When(OrderStatusChangedEvent) catch-all
            // passed its test with no fault. Ignore(...) is used here regardless, for
            // consistency across all five During() blocks in this file (all now use Ignore(...)
            // for their catch-alls) — not because bare When() is provably broken.
            Ignore(CheckoutTimeout.Received),
            Ignore(PaymentAuthorisedEvent),
            Ignore(PaymentFailedEvent),
            Ignore(OrderStatusChangedEvent),
            // 04-07-REVIEW CR-02: see the matching Ignore(OrderCreatedEvent) note in
            // During(Pending, ...) above — a redelivered OrderCreated must be absorbed here too.
            Ignore(OrderCreatedEvent));

        // Cancelled is terminal and reachable from both Pending (PaymentFailed/timeout) and
        // Paid (FulfillmentFailed) — both of those transitions also .Publish() an
        // OrderStatusChanged event, and this saga is itself subscribed to
        // OrderStatusChangedEvent (see Event() registration above, used by external
        // redelivery per ORD-03). That self-published message loops back to this exact same
        // saga instance's receive endpoint; without a catch-all here it raises
        // UnhandledEventException/NotAcceptedStateMachineException once the saga has already
        // reached Cancelled (discovered via saga unit tests — same class of race as Pitfall 2,
        // just self-inflicted by this plan's own new Publish() activities rather than an
        // external redelivery). CR-01 (04-REVIEW.md): the same widened catch-all as
        // During(Paid, ...) above is needed here too — a late/redelivered
        // PaymentAuthorisedEvent, PaymentFailedEvent, FulfillmentFailedEvent, or
        // CheckoutTimeout.Received can legitimately arrive after the saga has already reached
        // Cancelled (broker at-least-once redelivery, or a double-clicked demo trigger racing
        // the eventually-consistent read model). Absorb rather than fault; no transition,
        // stays terminal.
        During(Cancelled,
            Ignore(CheckoutTimeout.Received),
            Ignore(PaymentAuthorisedEvent),
            Ignore(PaymentFailedEvent),
            Ignore(FulfillmentFailedEvent),
            // T-05-04: a late/redelivered OrderShipped after the saga already left Paid
            // (e.g. cancelled via FulfillmentFailed racing a shipment that was already in
            // flight) must be absorbed, not faulted — same discipline as every other event.
            Ignore(OrderShippedEvent),
            Ignore(OrderStatusChangedEvent),
            // 04-07-REVIEW CR-02: see the matching Ignore(OrderCreatedEvent) note in
            // During(Pending, ...) above — a redelivered OrderCreated must be absorbed here too.
            Ignore(OrderCreatedEvent));

        // 04-07-REVIEW CR-01: Fulfilled and Failed are also terminal states reachable from
        // Paid/Pending, but had zero During() bindings at all — any late/redelivered event
        // (broker at-least-once redelivery of an already-consumed payment outcome, a stray
        // CheckoutTimeout.Received that raced Unschedule(), etc.) arriving after the saga
        // reaches either state faulted it, exactly the defect class the During(Cancelled, ...)
        // catch-all above exists to close. Absorb rather than fault; no transition, stays
        // terminal.
        During(Fulfilled,
            Ignore(CheckoutTimeout.Received),
            Ignore(PaymentAuthorisedEvent),
            Ignore(PaymentFailedEvent),
            Ignore(FulfillmentFailedEvent),
            // T-05-04: a redelivered OrderShipped after the saga already reached the
            // terminal Fulfilled state must be absorbed, not faulted.
            Ignore(OrderShippedEvent),
            Ignore(OrderStatusChangedEvent),
            // 04-07-REVIEW CR-02: see the matching Ignore(OrderCreatedEvent) note in
            // During(Pending, ...) above — a redelivered OrderCreated must be absorbed here too.
            Ignore(OrderCreatedEvent));

        During(Failed,
            Ignore(CheckoutTimeout.Received),
            Ignore(PaymentAuthorisedEvent),
            Ignore(PaymentFailedEvent),
            Ignore(FulfillmentFailedEvent),
            // T-05-04: a late/redelivered OrderShipped after the saga already reached the
            // terminal Failed state must be absorbed, not faulted.
            Ignore(OrderShippedEvent),
            Ignore(OrderStatusChangedEvent),
            // 04-07-REVIEW CR-02: see the matching Ignore(OrderCreatedEvent) note in
            // During(Pending, ...) above — a redelivered OrderCreated must be absorbed here too.
            Ignore(OrderCreatedEvent));

        SetCompletedWhenFinalized();
    }
}
