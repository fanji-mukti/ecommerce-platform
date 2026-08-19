---
phase: 05-fulfillment-notifications
reviewed: 2026-08-19T00:00:00Z
depth: standard
files_reviewed: 56
files_reviewed_list:
  - src/building-blocks/Contracts/Fulfillment/Events/V1/OrderShipped.cs
  - src/building-blocks/Contracts/Orders/Events/V1/OrderStatusChanged.cs
  - src/building-blocks/Contracts/Payments/Commands/V1/AuthorisePayment.cs
  - src/building-blocks/Contracts/Payments/Events/V1/PaymentFailed.cs
  - src/ecommerce.AppHost/Program.cs
  - src/frontend/ecommerce-app/src/app/app.html
  - src/frontend/ecommerce-app/src/app/app.routes.ts
  - src/frontend/ecommerce-app/src/app/core/services/notifications.service.ts
  - src/frontend/ecommerce-app/src/app/features/notifications/notifications-page/notifications-page.component.html
  - src/frontend/ecommerce-app/src/app/features/notifications/notifications-page/notifications-page.component.scss
  - src/frontend/ecommerce-app/src/app/features/notifications/notifications-page/notifications-page.component.spec.ts
  - src/frontend/ecommerce-app/src/app/features/notifications/notifications-page/notifications-page.component.ts
  - src/frontend/ecommerce-app/src/app/features/orders/order-detail/order-detail.component.html
  - src/frontend/ecommerce-app/src/app/features/orders/order-detail/order-detail.component.scss
  - src/frontend/ecommerce-app/src/app/features/orders/order-detail/order-detail.component.spec.ts
  - src/frontend/ecommerce-app/src/app/features/orders/order-detail/order-detail.component.ts
  - src/frontend/ecommerce-app/src/app/shared/models/notification.model.ts
  - src/services/fulfillment/ECommerce.Fulfillment.API/Data/DbInitializer.cs
  - src/services/fulfillment/ECommerce.Fulfillment.API/Data/FulfillmentDbContext.cs
  - src/services/fulfillment/ECommerce.Fulfillment.API/ECommerce.Fulfillment.API.csproj
  - src/services/fulfillment/ECommerce.Fulfillment.API/Features/Fulfillment/FulfillmentOptions.cs
  - src/services/fulfillment/ECommerce.Fulfillment.API/Features/Fulfillment/OrderPaidConsumer.cs
  - src/services/fulfillment/ECommerce.Fulfillment.API/Migrations/20260814150228_InitialFulfillmentSchema.cs
  - src/services/fulfillment/ECommerce.Fulfillment.API/Program.cs
  - src/services/fulfillment/ECommerce.Fulfillment.Tests/ECommerce.Fulfillment.Tests.csproj
  - src/services/fulfillment/ECommerce.Fulfillment.Tests/Integration/OrderPaidConsumerSteps.cs
  - src/services/fulfillment/ECommerce.Fulfillment.Tests/Integration/OrderPaidConsumerTests.cs
  - src/services/fulfillment/ECommerce.Fulfillment.Tests/Integration/OrderPaidInboxDeduplicationSteps.cs
  - src/services/fulfillment/ECommerce.Fulfillment.Tests/Integration/OrderPaidInboxDeduplicationTests.cs
  - src/services/fulfillment/Fulfillment.sln
  - src/services/notifications/ECommerce.Notifications.API/Consumers/OrderPaidNotificationConsumer.cs
  - src/services/notifications/ECommerce.Notifications.API/Consumers/OrderShippedNotificationConsumer.cs
  - src/services/notifications/ECommerce.Notifications.API/Consumers/PaymentFailedNotificationConsumer.cs
  - src/services/notifications/ECommerce.Notifications.API/Data/NotificationsDbContext.cs
  - src/services/notifications/ECommerce.Notifications.API/ECommerce.Notifications.API.csproj
  - src/services/notifications/ECommerce.Notifications.API/Features/Notifications/NotificationEntry.cs
  - src/services/notifications/ECommerce.Notifications.API/Features/Notifications/NotificationsEndpoints.cs
  - src/services/notifications/ECommerce.Notifications.API/Migrations/20260814150207_AddNotificationEntry.cs
  - src/services/notifications/ECommerce.Notifications.API/Program.cs
  - src/services/notifications/ECommerce.Notifications.Tests/ECommerce.Notifications.Tests.csproj
  - src/services/notifications/ECommerce.Notifications.Tests/Integration/NotificationsEndpointSteps.cs
  - src/services/notifications/ECommerce.Notifications.Tests/Integration/NotificationsEndpointTests.cs
  - src/services/notifications/ECommerce.Notifications.Tests/Integration/OrderPaidNotificationConsumerSteps.cs
  - src/services/notifications/ECommerce.Notifications.Tests/Integration/OrderPaidNotificationConsumerTests.cs
  - src/services/notifications/ECommerce.Notifications.Tests/Integration/OrderPaidNotificationInboxDeduplicationSteps.cs
  - src/services/notifications/ECommerce.Notifications.Tests/Integration/OrderPaidNotificationInboxDeduplicationTests.cs
  - src/services/notifications/ECommerce.Notifications.Tests/Integration/OrderShippedNotificationConsumerSteps.cs
  - src/services/notifications/ECommerce.Notifications.Tests/Integration/OrderShippedNotificationConsumerTests.cs
  - src/services/notifications/ECommerce.Notifications.Tests/Integration/PaymentFailedNotificationConsumerSteps.cs
  - src/services/notifications/ECommerce.Notifications.Tests/Integration/PaymentFailedNotificationConsumerTests.cs
  - src/services/orders/ECommerce.Orders.API/Features/Orders/OrderStateMachine.cs
  - src/services/orders/ECommerce.Orders.Tests/Unit/OrderStateMachineSteps.cs
  - src/services/orders/ECommerce.Orders.Tests/Unit/OrderStateMachineTests.cs
  - src/services/payments/ECommerce.Payments.API/Features/Payments/AuthorisePaymentConsumer.cs
  - src/services/payments/ECommerce.Payments.Tests/Integration/AuthorisePaymentConsumerSteps.cs
  - src/services/payments/ECommerce.Payments.Tests/Integration/AuthorisePaymentConsumerTests.cs
findings:
  critical: 1
  warning: 4
  info: 4
  total: 9
status: issues_found
---

# Phase 05: Code Review Report

**Reviewed:** 2026-08-19T00:00:00Z
**Depth:** standard
**Files Reviewed:** 56
**Status:** issues_found

## Summary

Phase 05 wires up the Fulfillment and Notifications services and closes the Paid→Fulfilled loop in
`OrderStateMachine` via the new `OrderShipped` event, plus a `/notifications` read endpoint and Angular
UI. The MassTransit inbox/outbox wiring, saga catch-all discipline, and payment idempotency work are
carried through carefully and are well tested for the "happy path" and most redelivery scenarios.

The one critical finding is a genuine data-correctness gap introduced by this phase: Fulfillment
schedules a delayed `OrderShipped` publish the moment an order is Paid, but nothing cancels that
scheduled message if the order is later cancelled (e.g. via the existing "simulate fulfillment failure"
demo endpoint). The result is a customer-visible, factually wrong "Your order has shipped" notification
for an order that was actually cancelled and refunded. This is reachable through an existing,
authorized demo flow — not a contrived edge case — and should be fixed before this phase ships.

The remaining findings are test-reliability and quality gaps: a no-op `RemoveAll<DbInitializer>()` call
newly authored in `NotificationsEndpointSteps.cs` (mirrors a pre-existing bug elsewhere in the repo),
an asymmetric inbox-deduplication test-coverage gap between the three Notifications consumers, and a
polling loop in the Angular order-detail page that silently and permanently stops on the first
transient HTTP error.

## Critical Issues

### CR-01: Scheduled OrderShipped is never cancelled when an order is subsequently Cancelled — customers can receive a false "Your order has shipped" notification

**File:** `src/services/fulfillment/ECommerce.Fulfillment.API/Features/Fulfillment/OrderPaidConsumer.cs:24-31`
**Also implicated:** `src/services/orders/ECommerce.Orders.API/Features/Orders/OrderStateMachine.cs:201-224` (FulfillmentFailed→Cancelled compensation), `src/services/notifications/ECommerce.Notifications.API/Consumers/OrderShippedNotificationConsumer.cs:20-28`

**Issue:**
`OrderPaidConsumer` reacts to `OrderStatusChanged(NewStatus == "Paid")` by scheduling a delayed
`OrderShipped` publish (`ProcessingSeconds`, default 45s):

```csharp
await context.SchedulePublish(delay, new OrderShipped(
    MessageId: Guid.NewGuid(),
    CorrelationId: msg.OrderId,
    CausationId: msg.MessageId,
    OccurredAt: now,
    CheckoutId: msg.OrderId,
    UserId: msg.UserId,
    ShippedAt: now + delay));
```

This scheduled message is never cancelled. `OrderStateMachine` already supports a legitimate
Paid→Cancelled compensation path via `FulfillmentFailedEvent` (published, in production, by
`Checkout.API`'s "simulate fulfillment failure" endpoint while the order is `Paid` —
`CheckoutEndpoints.cs`), which cancels the order and publishes `RefundPayment`. Reproduction:

1. Order reaches `Paid` → Fulfillment schedules `OrderShipped` for `now + 45s`.
2. Within that 45s window, an authorized user calls the fulfillment-failure demo endpoint
   (`POST .../simulate-fulfillment-failure` or equivalent, gated only on `snapshot.Status == "Paid"`).
3. The saga transitions Paid→Cancelled and publishes `RefundPayment` + `OrderStatusChanged(Cancelled)`.
4. ~45s later, Fulfillment's previously-scheduled `OrderShipped` still fires. The saga's
   `During(Cancelled, ...)` block correctly absorbs it (`Ignore(OrderShippedEvent)`, line 286) so the
   saga itself doesn't fault — but `OrderShippedNotificationConsumer` has no such awareness: it
   unconditionally inserts `"Your order has shipped."` into the customer's notification feed for an
   order that was actually cancelled and refunded.

The customer now sees two contradictory notifications ("Payment failed"/order cancelled, then later
"Your order has shipped") for the same order, and the order-detail page's status chip will correctly
show `Cancelled` while the notifications list claims it shipped. This is a genuine, reachable
data-correctness bug, not a hypothetical race — the demo endpoint exists specifically to trigger it
while the order is `Paid`.

**Fix:** Capture the scheduled message token (`ScheduledMessage<OrderShipped>`) and persist/unschedule
it when the order leaves `Paid` via a non-shipping path, or — simpler for this architecture — have
`OrderShippedNotificationConsumer` (and/or the saga's `OrderShippedEvent` handler) check the current
order status before treating a late `OrderShipped` as ship-worthy, e.g. by having Fulfillment publish a
distinguishable "shipment cancelled" signal, or by unscheduling in `OrderStateMachine`:

```csharp
When(FulfillmentFailedEvent)
    .Then(ctx => ctx.Saga.FailureReason = $"Fulfillment failed — order cancelled and refunded ({ctx.Message.Reason})")
    // ... existing Publish(RefundPayment) / Publish(OrderStatusChanged) ...
```
would need a corresponding `context.CancelScheduledPublish<OrderShipped>(...)` call in Fulfillment, or
Fulfillment itself should re-check the order's current status (e.g. via a lightweight read-model query)
immediately before publishing `OrderShipped`, and skip the publish if the order is no longer `Paid`.

## Warnings

### WR-01: `RemoveAll<DbInitializer>()` in the Notifications test factory is a no-op — does not achieve the isolation the code claims

**File:** `src/services/notifications/ECommerce.Notifications.Tests/Integration/NotificationsEndpointSteps.cs:58`

**Issue:**
```csharp
builder.ConfigureServices(services =>
{
    // Remove DbInitializer so it does not race with test-driven migrations/seeding.
    services.RemoveAll<DbInitializer>();
    ...
});
```
`DbInitializer` is registered in `Program.cs` via `builder.Services.AddHostedService<DbInitializer>()`,
which under the hood registers the descriptor as `ServiceDescriptor.Singleton<IHostedService, DbInitializer>()`
— i.e. `ServiceType == typeof(IHostedService)`, not `typeof(DbInitializer)`.
`services.RemoveAll<DbInitializer>()` filters by `descriptor.ServiceType == typeof(DbInitializer)`, which
never matches, so this call removes nothing. `DbInitializer.StartAsync` (which calls
`db.Database.MigrateAsync(cancellationToken)`) still runs during test host startup, concurrently with
the test's own `db.Database.MigrateAsync()` calls in `CreateDbContextAsync()` / `Given_HarnessWithPostgresOutbox()`.
EF Core's migration lock generally prevents outright corruption, but the intended isolation described by
the comment does not actually exist, and this masks the real behavior for anyone reading the test.

Note: this exact pattern (`RemoveAll<DbInitializer>()` against an `AddHostedService`-registered type)
already exists in `OrdersEndpointSteps.cs:66` and `ProductsEndpointSteps.cs:41` elsewhere in the repo, so
this is a systemic issue being propagated into a third project by this phase rather than a wholly new
defect — still worth fixing here since this file is new in this phase.

**Fix:** Remove by implementation type or remove all hosted services in the test host:
```csharp
services.RemoveAll(d => d.ServiceType == typeof(IHostedService)
    && d.ImplementationType == typeof(DbInitializer));
```
or, if no other hosted services matter in the test host:
```csharp
services.RemoveAll<IHostedService>();
```

### WR-02: Asymmetric inbox-deduplication test coverage across the three Notifications consumers

**File:** `src/services/notifications/ECommerce.Notifications.Tests/Integration/OrderShippedNotificationConsumerSteps.cs`, `src/services/notifications/ECommerce.Notifications.Tests/Integration/PaymentFailedNotificationConsumerSteps.cs`

**Issue:** All three Notifications consumers (`OrderPaidNotificationConsumer`, `OrderShippedNotificationConsumer`,
`PaymentFailedNotificationConsumer`) run behind the same `AddEntityFrameworkOutbox<NotificationsDbContext>`
inbox middleware configured in `Program.cs:41-50`. Only `OrderPaidNotificationConsumer` has a dedicated
redelivery/dedup integration test (`OrderPaidNotificationInboxDeduplicationTests.cs`, proving exactly one
`InboxState` row and exactly one `NotificationEntry` row for a duplicate-MessageId delivery). The other two
consumers' Steps classes (`OrderShippedNotificationConsumerSteps`, `PaymentFailedNotificationConsumerSteps`)
build their harness without `AddEntityFrameworkOutbox` at all and never assert on `InboxState`/duplicate
delivery, so a redelivery bug specific to either of those two consumers (e.g. an accidental change that
breaks inbox wiring for just one receive endpoint) would not be caught by the test suite despite the
project's own established convention (used for `OrderPaidConsumer` in Fulfillment and both payment
consumers) of proving idempotency per-consumer.

**Fix:** Add `OrderShippedNotificationInboxDeduplicationTests` / `PaymentFailedNotificationInboxDeduplicationTests`
mirroring `OrderPaidNotificationInboxDeduplicationSteps.cs`'s pattern (Postgres-backed harness with
`AddEntityFrameworkOutbox`, publish the same pinned `MessageId` twice, assert one `InboxState` row and one
`NotificationEntry` row).

### WR-03: Order-detail polling permanently stops on the first transient HTTP error, with no retry and no user-facing indication

**File:** `src/frontend/ecommerce-app/src/app/features/orders/order-detail/order-detail.component.ts:73-87`

**Issue:**
```ts
private startPolling(id: string): void {
  interval(1500)
    .pipe(
      switchMap(() => this.ordersService.getOrder(id)),
      takeWhile((order) => {
        this.order.set(order);
        return !ORDER_DETAIL_TERMINAL_STATUSES.includes(order.status);
      }, true),
      takeUntilDestroyed(this.destroyRef),
    )
    .subscribe({
      next: () => {},
      error: () => {},
    });
}
```
If any single poll request errors (e.g. a transient 5xx/network blip), the inner observable produced by
`switchMap` errors, which propagates through `takeWhile`/`takeUntilDestroyed` and terminates the whole
`interval` subscription — polling stops permanently for the rest of the component's lifetime. The
`error: () => {}` handler swallows the error silently; neither `hasError` nor any other signal is set
during polling (those are only used for the initial `ngOnInit` load), so the user is left staring at a
stale, possibly non-terminal status (e.g. "Preparing your shipment…") with no indication that live
updates have stopped and no way to recover short of a full page reload.

**Fix:** Recover from per-poll errors instead of letting them terminate the stream, e.g.:
```ts
switchMap(() => this.ordersService.getOrder(id).pipe(catchError(() => EMPTY))),
```
(and consider surfacing a subtle "updates paused" indicator so the user isn't misled by a stale status).

### WR-04: `AuthorisePaymentConsumer.Consume` throws for an unrecognized `Outcome` with no consumer-facing test coverage for the resulting message-fault behavior

**File:** `src/services/payments/ECommerce.Payments.API/Features/Payments/AuthorisePaymentConsumer.cs:42-48`

**Issue:** The `default` branch throws `InvalidOperationException` for a corrupted/unrecognized
`ProcessedPayment.Outcome`. That's a reasonable "loud failure" choice per the comment, but note the
consequence for production behavior: this throw happens *inside* the EF Core outbox transaction/consume
pipeline, so MassTransit will retry-then-fault the message per its default retry policy, and — because
this consumer redelivers the *same* `CheckoutId` on every retry — it will deterministically fault every
single time (not a transient condition), permanently stalling that checkout's payment outcome and
leaving the corresponding `OrderStateMachine` instance stuck in `Paid`/`Pending` forever with no
compensating timeout once `PaymentAuthorised`/`PaymentFailed` can never be published for that order. This
isn't reachable through any current code path (no writer produces an out-of-enum `Outcome` value today),
so it's not a Critical, but it's a latent "stuck order" trap with no operational mitigation (no
dead-letter alerting/visibility described anywhere in the reviewed files) if the enum of outcomes is ever
extended without updating this switch.

**Fix:** At minimum, log a structured error with enough context to alert an operator before/instead of
throwing, or ensure there's a documented dead-letter/alerting story for Payments' receive endpoint so a
stuck checkout doesn't silently retry forever.

## Info

### IN-01: `iconFor()` has no default/exhaustiveness guard against future `NotificationEventType` additions

**File:** `src/frontend/ecommerce-app/src/app/features/notifications/notifications-page/notifications-page.component.ts:40-49`

**Issue:** The `switch` over `NotificationEventType` relies on TypeScript's control-flow exhaustiveness
analysis (no `default` case) to guarantee a return in every branch. That works today, but if
`NotificationEventType` gains a new member without updating this switch, the change won't produce a
compile error unless `strict`/`noImplicitReturns` catches it project-wide — the function would then
return `undefined` at runtime for the new event type, rendering a blank `<mat-icon>`.

**Fix:** Add an explicit exhaustiveness assertion, e.g. a `default: { const _exhaustive: never = eventType; return _exhaustive; }`, to force a compile-time error on future additions.

### IN-02: Inconsistent assertion style in `AuthorisePaymentConsumerTests.cs`

**File:** `src/services/payments/ECommerce.Payments.Tests/Integration/AuthorisePaymentConsumerTests.cs:23,46,58,75,89,96,113,114,130`

**Issue:** This test class mixes xUnit's `Assert.Equal(...)` with the rest of the codebase's established
FluentAssertions `.Should()` style (used consistently in the corresponding `*Steps.cs` helper methods and
in every other reviewed test file). Purely stylistic, but it's a drift from the codebase's own convention
introduced within this phase's diff.

**Fix:** Use `(await _steps.Then_PublishedCount<T>()).Should().Be(n)` for consistency with the rest of the suite.

### IN-03: `FulfillmentOptions.ProcessingSeconds` has no validation against zero/negative configuration values

**File:** `src/services/fulfillment/ECommerce.Fulfillment.API/Features/Fulfillment/FulfillmentOptions.cs:13`

**Issue:** `ProcessingSeconds` is bound directly from configuration with no `[Range]`/`IValidateOptions`
guard. A misconfigured negative value would cause `TimeSpan.FromSeconds(options.Value.ProcessingSeconds)`
in `OrderPaidConsumer.cs:21` to throw `ArgumentOutOfRangeException` for every `Paid` transition, faulting
every message on that receive endpoint.

**Fix:** Add options validation (`services.AddOptions<FulfillmentOptions>().Bind(...).Validate(o => o.ProcessingSeconds > 0, "ProcessingSeconds must be positive")`) consistent with the rest of the stack's FluentValidation-first convention.

### IN-04: Initial HTTP load subscriptions are not tied to `takeUntilDestroyed`, unlike the polling subscription

**File:** `src/frontend/ecommerce-app/src/app/features/orders/order-detail/order-detail.component.ts:60-70`, `src/frontend/ecommerce-app/src/app/features/notifications/notifications-page/notifications-page.component.ts:59-68`

**Issue:** Both components' initial `ngOnInit` HTTP calls are a plain `.subscribe({...})` with no
`takeUntilDestroyed(this.destroyRef)`, while `order-detail.component.ts`'s polling subscription
explicitly adds it. Since these are one-shot HTTP requests they complete on their own and this is low
risk in practice (Angular's `HttpClient` observables complete after one emission), but if the component is
destroyed while the request is still in flight, `order.set(...)` / `startPolling(id)` will still execute
against a torn-down component, which is inconsistent with the defensive pattern already used two lines
below for the polling stream.

**Fix:** Add `takeUntilDestroyed(this.destroyRef)` to the initial load subscription for consistency.

---

_Reviewed: 2026-08-19T00:00:00Z_
_Reviewer: Claude (gsd-code-reviewer)_
_Depth: standard_
