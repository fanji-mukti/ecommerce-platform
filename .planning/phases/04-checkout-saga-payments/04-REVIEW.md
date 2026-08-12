---
phase: 04-checkout-saga-payments
reviewed: 2026-08-12T00:00:00Z
depth: standard
files_reviewed: 62
files_reviewed_list:
  - docs/adr/0009-checkout-saga-state-reconciliation.md
  - spikes/04-asb-scheduling-spike/Config.json
  - spikes/04-asb-scheduling-spike/Program.cs
  - spikes/04-asb-scheduling-spike/SpikeRunner.csproj
  - spikes/04-asb-scheduling-spike/docker-compose.yml
  - src/building-blocks/Contracts/Checkout/Commands/V1/StartCheckout.cs
  - src/building-blocks/Contracts/Fulfillment/Events/V1/FulfillmentFailed.cs
  - src/building-blocks/Contracts/Orders/Events/V1/OrderCreated.cs
  - src/building-blocks/Contracts/Orders/Events/V1/OrderStatusChanged.cs
  - src/building-blocks/Contracts/Payments/Commands/V1/AuthorisePayment.cs
  - src/building-blocks/Contracts/Payments/Commands/V1/RefundPayment.cs
  - src/building-blocks/Contracts/Payments/Events/V1/PaymentAuthorised.cs
  - src/building-blocks/Contracts/Payments/Events/V1/PaymentFailed.cs
  - src/building-blocks/Contracts/Payments/Events/V1/PaymentRefunded.cs
  - src/building-blocks/Tests.Common/Builders/PaymentBuilder.cs
  - src/ecommerce.AppHost/Program.cs
  - src/frontend/ecommerce-app/src/app/app.routes.ts
  - src/frontend/ecommerce-app/src/app/core/services/checkout.service.ts
  - src/frontend/ecommerce-app/src/app/core/services/orders.service.ts
  - src/frontend/ecommerce-app/src/app/features/cart/cart-page/cart-page.component.html
  - src/frontend/ecommerce-app/src/app/features/cart/cart-page/cart-page.component.scss
  - src/frontend/ecommerce-app/src/app/features/checkout/checkout-page/checkout-page.component.html
  - src/frontend/ecommerce-app/src/app/features/checkout/checkout-page/checkout-page.component.scss
  - src/frontend/ecommerce-app/src/app/features/checkout/checkout-page/checkout-page.component.spec.ts
  - src/frontend/ecommerce-app/src/app/features/checkout/checkout-page/checkout-page.component.ts
  - src/frontend/ecommerce-app/src/app/features/orders/order-detail/order-detail.component.html
  - src/frontend/ecommerce-app/src/app/features/orders/order-detail/order-detail.component.scss
  - src/frontend/ecommerce-app/src/app/features/orders/order-detail/order-detail.component.spec.ts
  - src/frontend/ecommerce-app/src/app/features/orders/order-detail/order-detail.component.ts
  - src/frontend/ecommerce-app/src/app/shared/models/checkout.model.ts
  - src/frontend/ecommerce-app/src/app/shared/models/order.model.ts
  - src/services/checkout/Checkout.sln
  - src/services/checkout/ECommerce.Checkout.API/ECommerce.Checkout.API.csproj
  - src/services/checkout/ECommerce.Checkout.API/Features/Checkout/CheckoutEndpoints.cs
  - src/services/checkout/ECommerce.Checkout.API/Features/Checkout/CheckoutStatusDto.cs
  - src/services/checkout/ECommerce.Checkout.API/Features/Checkout/IOrdersClient.cs
  - src/services/checkout/ECommerce.Checkout.API/Features/Checkout/OrdersClient.cs
  - src/services/checkout/ECommerce.Checkout.API/Program.cs
  - src/services/checkout/ECommerce.Checkout.Tests/ECommerce.Checkout.Tests.csproj
  - src/services/checkout/ECommerce.Checkout.Tests/Integration/CheckoutEndpointSteps.cs
  - src/services/checkout/ECommerce.Checkout.Tests/Integration/CheckoutEndpointTests.cs
  - src/services/gateway/ECommerce.Gateway.API/appsettings.json
  - src/services/orders/ECommerce.Orders.API/ECommerce.Orders.API.csproj
  - src/services/orders/ECommerce.Orders.API/Features/Orders/CheckoutOptions.cs
  - src/services/orders/ECommerce.Orders.API/Features/Orders/CheckoutTimeoutExpired.cs
  - src/services/orders/ECommerce.Orders.API/Features/Orders/Order.cs
  - src/services/orders/ECommerce.Orders.API/Features/Orders/OrderDto.cs
  - src/services/orders/ECommerce.Orders.API/Features/Orders/OrderReadModel.cs
  - src/services/orders/ECommerce.Orders.API/Features/Orders/OrderReadModelProjector.cs
  - src/services/orders/ECommerce.Orders.API/Features/Orders/OrderStateMachine.cs
  - src/services/orders/ECommerce.Orders.API/Features/Orders/OrdersEndpoints.cs
  - src/services/orders/ECommerce.Orders.API/Migrations/20260808034000_AddCheckoutSagaFields.Designer.cs
  - src/services/orders/ECommerce.Orders.API/Migrations/20260808034000_AddCheckoutSagaFields.cs
  - src/services/orders/ECommerce.Orders.API/Migrations/OrdersDbContextModelSnapshot.cs
  - src/services/orders/ECommerce.Orders.API/Program.cs
  - src/services/orders/ECommerce.Orders.API/appsettings.json
  - src/services/orders/ECommerce.Orders.Tests/Integration/OrdersEndpointSteps.cs
  - src/services/orders/ECommerce.Orders.Tests/Integration/OrdersEndpointTests.cs
  - src/services/orders/ECommerce.Orders.Tests/Unit/OrderStateMachineSteps.cs
  - src/services/orders/ECommerce.Orders.Tests/Unit/OrderStateMachineTests.cs
  - src/services/payments/ECommerce.Payments.API/Data/DbInitializer.cs
  - src/services/payments/ECommerce.Payments.API/Data/PaymentsDbContext.cs
  - src/services/payments/ECommerce.Payments.API/ECommerce.Payments.API.csproj
  - src/services/payments/ECommerce.Payments.API/Features/Payments/AuthorisePaymentConsumer.cs
  - src/services/payments/ECommerce.Payments.API/Features/Payments/ProcessedPayment.cs
  - src/services/payments/ECommerce.Payments.API/Features/Payments/RefundPaymentConsumer.cs
  - src/services/payments/ECommerce.Payments.API/Migrations/20260807225224_InitialPaymentsSchema.Designer.cs
  - src/services/payments/ECommerce.Payments.API/Migrations/20260807225224_InitialPaymentsSchema.cs
  - src/services/payments/ECommerce.Payments.API/Migrations/PaymentsDbContextModelSnapshot.cs
  - src/services/payments/ECommerce.Payments.API/Program.cs
  - src/services/payments/ECommerce.Payments.Tests/ECommerce.Payments.Tests.csproj
  - src/services/payments/ECommerce.Payments.Tests/Integration/AuthorisePaymentConsumerSteps.cs
  - src/services/payments/ECommerce.Payments.Tests/Integration/AuthorisePaymentConsumerTests.cs
  - src/services/payments/Payments.sln
findings:
  critical: 1
  warning: 4
  info: 3
  total: 8
status: issues_found
---

# Phase 04: Code Review Report

**Reviewed:** 2026-08-12T00:00:00Z
**Depth:** standard
**Files Reviewed:** 62
**Status:** issues_found

## Summary

Reviewed the Phase 4 checkout-saga-payments implementation: the extended `OrderStateMachine`
(CHK-03/CHK-04/CHK-05), the Payments service's `AuthorisePaymentConsumer`/`RefundPaymentConsumer`,
Checkout.API's HTTP-layer "Started" status synthesis (ADR-0009), and the Angular checkout/order
detail pages. The overall design is sound and well-documented (extensive inline rationale,
matching ADR-0009's decision record). However, the saga's new terminal-state handling has a real
gap: `Cancelled` only absorbs redelivered `OrderStatusChangedEvent`, not the newly-introduced
`PaymentAuthorisedEvent`/`PaymentFailedEvent`/`FulfillmentFailedEvent` — all three of which can
plausibly arrive late (broker redelivery, or a double-clicked demo trigger) after the saga has
already reached `Cancelled`, and MassTransit will throw `UnhandledEventException` when that
happens. There are also two payment-idempotency logic gaps in the Payments consumers, a
data-loss defect where the saga discards the real `FulfillmentFailed.Reason` in favor of a
hardcoded string, and a couple of frontend robustness gaps in the checkout status-polling flow.

## Critical Issues

### CR-01: OrderStateMachine's `Cancelled` state does not absorb late payment/fulfillment events — redelivery throws `UnhandledEventException`

**File:** `src/services/orders/ECommerce.Orders.API/Features/Orders/OrderStateMachine.cs:205-216`
**Issue:**
`During(Paid, ...)` explicitly absorbs late `CheckoutTimeout.Received`, `PaymentAuthorisedEvent`,
and `PaymentFailedEvent` (see the "Pitfall 2" comment at lines 195-199) because these can
legitimately arrive after the saga has already transitioned. The same race exists for `Cancelled`
— which is reachable from `Pending` via `PaymentFailedEvent` or `CheckoutTimeout.Received`, and
from `Paid` via `FulfillmentFailedEvent` — but `During(Cancelled, ...)` only declares a catch-all
for `OrderStatusChangedEvent` (lines 215-216):

```csharp
During(Cancelled,
    When(OrderStatusChangedEvent));
```

Concrete reproduction:
1. Saga is `Pending`. `CheckoutTimeout.Received` fires (CHK-05), transitioning to `Cancelled`.
2. The original `AuthorisePayment` command that was in flight before the timeout eventually
   completes and Payments publishes `PaymentAuthorisedEvent` (or a redelivered `PaymentFailedEvent`
   from a broker retry arrives after the timeout already cancelled the order).
3. The saga is now in `Cancelled`, which has no `When(PaymentAuthorisedEvent)` /
   `When(PaymentFailedEvent)` binding. MassTransit throws `UnhandledEventException`, faulting the
   consumer and putting the message into the retry/dead-letter path.

A second, more directly reachable path: the demo `POST /checkout/{id}/simulate-fulfillment-failure`
trigger (`CheckoutEndpoints.cs:53-77`) checks `snapshot.Status == "Paid"` against the read model
(eventually consistent) before publishing `FulfillmentFailed`. Two rapid clicks can both pass that
check before the first `FulfillmentFailed` is consumed; the second `FulfillmentFailed` then arrives
after the saga has already moved `Paid -> Cancelled`, hitting the exact same unhandled-event fault
(`During(Cancelled)` has no `When(FulfillmentFailedEvent)` either).

**Fix:** Extend the `Cancelled` (and, for completeness, verify `Fulfilled`) catch-all to absorb the
same set of events `Paid` already defends against:
```csharp
During(Cancelled,
    When(CheckoutTimeout.Received),
    When(PaymentAuthorisedEvent),
    When(PaymentFailedEvent),
    When(FulfillmentFailedEvent),
    When(OrderStatusChangedEvent));
```

## Warnings

### WR-01: Saga discards the real `FulfillmentFailed.Reason` and always substitutes a hardcoded string

**File:** `src/services/orders/ECommerce.Orders.API/Features/Orders/OrderStateMachine.cs:172-183`
**Issue:** `FulfillmentFailed` carries a `Reason` field (`src/building-blocks/Contracts/Fulfillment/Events/V1/FulfillmentFailed.cs:11`), but the saga never reads `ctx.Message.Reason` — it hardcodes its own string instead:
```csharp
When(FulfillmentFailedEvent)
    .Then(ctx => ctx.Saga.FailureReason = "Fulfillment failed — order cancelled and refunded")
```
The unit test `FulfillmentFailed_WhenPaid_TransitionsToCancelledAndPublishesRefundPayment`
(`OrderStateMachineTests.cs:72-89`) publishes `FulfillmentFailed` with `reason: "Warehouse out of
stock"` and then asserts the saga's `FailureReason` equals the unrelated hardcoded string —
confirming the event's `Reason` payload is silently discarded, not a coincidence. This also
contradicts the code comment in `CheckoutEndpoints.cs:70-71` ("Checkout.API does not invent the
failure reason string — only the saga does") — in fact Checkout.API's demo trigger *does* invent a
reason string (`"Fulfillment failed — order cancelled and refunded"`, `CheckoutEndpoints.cs:74`),
and the saga separately invents an identical one rather than reading it from the event. Any future
producer of `FulfillmentFailed` with a genuinely different reason (e.g. a real Fulfillment service
reporting "Warehouse out of stock") will have that information silently thrown away and replaced
with the generic string, and the two hardcoded copies must be kept in sync by hand.

**Fix:** Use the event's own data:
```csharp
.Then(ctx => ctx.Saga.FailureReason = $"Fulfillment failed — order cancelled and refunded ({ctx.Message.Reason})")
```
or simply `ctx.Message.Reason` if the generic suffix isn't needed, and remove the duplicate
hardcoded string from `CheckoutEndpoints.cs`.

### WR-02: `AuthorisePaymentConsumer` mishandles redelivery after a payment has been refunded

**File:** `src/services/payments/ECommerce.Payments.API/Features/Payments/AuthorisePaymentConsumer.cs:19-36`
**Issue:** The idempotent-replay branch only distinguishes `Outcome == "Authorised"` from
"everything else":
```csharp
if (existing.Outcome == "Authorised")
{
    await publish.Publish(new PaymentAuthorised(...));
}
else
{
    await publish.Publish(new PaymentFailed(
        Guid.NewGuid(), msg.CheckoutId, msg.MessageId, now,
        msg.CheckoutId, existing.Amount, existing.FailureReason!, existing.ProcessedAt), ...);
}
```
If a redelivered `AuthorisePayment` arrives after the payment has since been refunded
(`existing.Outcome == "Refunded"`), this branch republishes it as a `PaymentFailed` event —
mislabeling a payment that actually succeeded (and was later refunded) as having failed. Worse,
`existing.FailureReason` is `null` for a `"Refunded"` row (it's only ever set on the `"Failed"`
path in this consumer), so `existing.FailureReason!` suppresses the compiler's nullable warning but
smuggles an actual `null` into `PaymentFailed.Reason`, a non-nullable `string` contract field.
**Fix:** Branch on the actual outcome instead of an `Authorised`/else binary:
```csharp
switch (existing.Outcome)
{
    case "Authorised":
        await publish.Publish(new PaymentAuthorised(...));
        break;
    case "Failed":
        await publish.Publish(new PaymentFailed(..., existing.FailureReason!, ...));
        break;
    case "Refunded":
        // Already refunded — do not republish a misleading outcome; log and no-op,
        // or republish PaymentRefunded if a consumer needs the replay.
        break;
}
```

### WR-03: `RefundPaymentConsumer` allows refunding a payment that was never authorised

**File:** `src/services/payments/ECommerce.Payments.API/Features/Payments/RefundPaymentConsumer.cs:16-21`
**Issue:** The idempotency guard only excludes `null` and already-`"Refunded"` records:
```csharp
var existing = await db.ProcessedPayments.FindAsync([msg.CheckoutId], context.CancellationToken);
if (existing is null || existing.Outcome == "Refunded")
{
    return;
}
existing.Outcome = "Refunded";
```
If `existing.Outcome == "Failed"` (the payment was declined and never actually charged), this code
still proceeds to mark it `"Refunded"` and publishes `PaymentRefunded` — a logically invalid state
transition (refunding money that was never taken). In the current saga this path shouldn't be
reachable through normal flow (`RefundPayment` is only published from `Paid`, which requires a
prior `PaymentAuthorisedEvent`), but the consumer itself has no defense-in-depth guard against it.
**Fix:** Guard on the expected precondition explicitly: `if (existing is null || existing.Outcome != "Authorised") return;`

### WR-04: Checkout status polling has no cleanup and no recovery from a transient error

**File:** `src/frontend/ecommerce-app/src/app/features/checkout/checkout-page/checkout-page.component.ts:103-126`
**Issue:** Two related robustness gaps in `startPolling`:
1. The `interval(1500).pipe(switchMap(...), takeWhile(...))` subscription is never torn down on
   component destroy (no `ngOnDestroy`, no `takeUntilDestroyed()`). If the user navigates away from
   `/checkout` before reaching a terminal status and later returns, `ngOnInit` → `onPlaceOrder` can
   start a second, independent polling loop, resulting in duplicate concurrent HTTP requests and
   potentially duplicate `router.navigate(['/orders', id])` calls.
2. Any HTTP error from `checkoutService.getStatus()` (e.g. a single transient network blip)
   propagates through `switchMap`/`takeWhile` and terminates the whole subscription permanently
   (`error: () => this.hasError.set(true)`). The only recovery action offered, the "Retry" button,
   is wired to `retry() => this.loadCart()` — which re-fetches the cart, not the checkout status —
   so a user whose order has already been placed (`checkoutId` is set) has no way to resume
   polling; they are stuck on an error screen unrelated to the actual failure.

**Fix:** Add `ngOnDestroy` (or `takeUntilDestroyed()`) to cancel the polling subscription, and give
the error handler a path back into `startPolling(id)` (or a dedicated "resume polling" retry
action) rather than reusing the unrelated `loadCart()`.

## Info

### IN-01: Dead code — `GetUserId` defined but never called in `CheckoutEndpoints`

**File:** `src/services/checkout/ECommerce.Checkout.API/Features/Checkout/CheckoutEndpoints.cs:80-83`
**Issue:** `GetUserId(ClaimsPrincipal user)` is defined but has no call sites in this file (ownership
checks happen downstream in Orders.API via the forwarded bearer token). Each of the three endpoint
handlers also injects an unused `ClaimsPrincipal user` parameter.
**Fix:** Remove the unused method and the unused `user` parameters, or add a comment explaining why
they're retained (e.g. future use), to avoid the reader assuming ownership is checked here.

### IN-02: `cart-page.component.scss` uses hardcoded pixel spacing instead of the design-token convention used elsewhere in this phase

**File:** `src/frontend/ecommerce-app/src/app/features/cart/cart-page/cart-page.component.scss:1-73`
**Issue:** `checkout-page.component.scss` and `order-detail.component.scss` (both added/touched in
this phase) consistently use `var(--space-xl)`, `var(--space-lg)`, etc. `cart-page.component.scss`
uses raw pixel literals (`32px`, `24px`, `16px`, `48px`) for the equivalent spacing throughout.
**Fix:** Align `cart-page.component.scss` with the `var(--space-*)` token convention for
consistency and easier future theming.

### IN-03: Spike `docker-compose.yml` hardcodes a database password in plaintext

**File:** `spikes/04-asb-scheduling-spike/docker-compose.yml:6`
**Issue:** `MSSQL_SA_PASSWORD: "SpikeOnly!Passw0rd"` is committed in plaintext. The spike is
explicitly local-only/throwaway per its own comments and the password gates a local SQL Edge
instance backing the ASB emulator, so the practical risk is low — but hardcoded credentials are an
easy pattern to copy-paste into a less throwaway context later.
**Fix:** Source the password from an environment variable / `.env` file excluded from source
control, even for spike code, to avoid establishing the hardcoded-credential pattern.

---

_Reviewed: 2026-08-12T00:00:00Z_
_Reviewer: Claude (gsd-code-reviewer)_
_Depth: standard_
