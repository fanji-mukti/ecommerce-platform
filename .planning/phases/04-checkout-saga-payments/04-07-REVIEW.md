---
phase: 04-checkout-saga-payments
reviewed: 2026-08-12T00:00:00Z
depth: standard
files_reviewed: 9
files_reviewed_list:
  - src/services/orders/ECommerce.Orders.API/Features/Orders/OrderStateMachine.cs
  - src/services/orders/ECommerce.Orders.Tests/Unit/OrderStateMachineSteps.cs
  - src/services/orders/ECommerce.Orders.Tests/Unit/OrderStateMachineTests.cs
  - src/services/checkout/ECommerce.Checkout.API/Features/Checkout/CheckoutEndpoints.cs
  - src/services/payments/ECommerce.Payments.API/Features/Payments/AuthorisePaymentConsumer.cs
  - src/services/payments/ECommerce.Payments.API/Features/Payments/RefundPaymentConsumer.cs
  - src/services/payments/ECommerce.Payments.Tests/Integration/AuthorisePaymentConsumerTests.cs
  - src/frontend/ecommerce-app/src/app/features/checkout/checkout-page/checkout-page.component.ts
  - src/frontend/ecommerce-app/src/app/features/checkout/checkout-page/checkout-page.component.spec.ts
findings:
  critical: 2
  warning: 3
  info: 3
  total: 8
status: issues_found
---

# Phase 04-07: Gap Closure Code Review Report

**Reviewed:** 2026-08-12T00:00:00Z
**Depth:** standard
**Files Reviewed:** 9
**Status:** issues_found

## Summary

Plan 04-07 closed CR-01 (`During(Cancelled, ...)` faulting on late/redelivered events),
WR-01 (discarded `FulfillmentFailed.Reason`), WR-02 (`AuthorisePaymentConsumer` post-refund
mislabeling), WR-03 (`RefundPaymentConsumer` missing defense-in-depth guard), and WR-04
(checkout-page polling subscription leak). The four Payments/Angular fixes are correct,
covered by targeted regression tests, and match the tests' expectations exactly.

However, the `OrderStateMachine` fix is **incomplete in a way that reproduces the exact
defect class CR-01 was supposed to eliminate**. I verified this empirically (not just by
reading the code) using MassTransit's in-memory saga test harness already present in this
repo:

- `Fulfilled` and `Failed` have **no `During()` block at all** — zero bindings for any of
  the saga's five event types. A late/redelivered `PaymentAuthorised`, `PaymentFailed`,
  `FulfillmentFailed`, `CheckoutTimeout.Received`, or `OrderStatusChanged` arriving after the
  saga reaches either state raises a fault. I added a temporary throwaway test reproducing
  exactly the `PaymentAuthorised_WhenAlreadyCancelled_IsAbsorbedWithoutFault` scenario but
  for `Fulfilled`/`Failed`, ran it against the real harness, watched it fail with
  `Fault<T>` published, then deleted the scratch file (no source files were left modified —
  `git status` is clean of any test-dir changes).
- `OrderCreatedEvent` has **no binding in any `During()` block**, including the states this
  plan touched (`Pending`, `Paid`, `Cancelled`). A redelivered `OrderCreated` for a saga
  instance that has already left the `Initial` pseudo-state faults even in `Pending` — I
  reproduced this the same way.

Both are documented below as Critical findings with reproduction steps, since they represent
exactly the kind of at-least-once-redelivery fault this gap-closure plan exists to close, just
left open for a wider surface than the CR-01 ticket described.

## Critical Issues

### CR-01: `Fulfilled` and `Failed` states have no catch-all — late/redelivered events fault the saga

**File:** `src/services/orders/ECommerce.Orders.API/Features/Orders/OrderStateMachine.cs:158-230`
**Issue:** The saga declares `During(Pending, ...)`, `During(Paid, ...)`, and
`During(Cancelled, ...)`, but **never declares `During(Fulfilled, ...)` or
`During(Failed, ...)`**. Both states are reachable (via `OrderStatusChanged` with
`NewStatus == "Fulfilled"` from `Paid`, and `NewStatus == "Failed"` from `Pending` or `Paid`).
Once a saga instance is in either state, MassTransit has no binding for any of the five
registered event types (`OrderCreatedEvent`, `OrderStatusChangedEvent`,
`PaymentAuthorisedEvent`, `PaymentFailedEvent`, `FulfillmentFailedEvent`,
`CheckoutTimeout.Received`) — any of these arriving (broker at-least-once redelivery of a
payment outcome that was already consumed, a stray `CheckoutTimeout.Received` that raced
`Unschedule`, etc.) is unhandled and raises a fault, exactly the CR-01 failure mode the rest of
this file was patched to eliminate for `Cancelled`.

I verified this is not theoretical: I temporarily added a probe test using the existing
`OrderStateMachineSteps`/`ISagaStateMachineTestHarness` fixture, drove a saga to `Fulfilled`
and separately to `Failed`, then re-delivered a late `PaymentAuthorised` / `PaymentFailed`
respectively. Both runs produced `Fault<T>` on the bus (`Then_NoFaultPublished<T>` failed):

```
ECommerce.Orders.Tests.Unit._TempVerifyProbe.Probe_LateEventWhileFulfilled_DoesItFault [FAIL]
  Expected ... to be False ... but found True.
ECommerce.Orders.Tests.Unit._TempVerifyProbe.Probe_LateEventWhileFailed_DoesItFault [FAIL]
  Expected ... to be False ... but found True.
```

The probe file was deleted after verification; no source files were left modified.

**Fix:** Add the same widened catch-all pattern already used for `Cancelled` to both terminal
states:

```csharp
During(Fulfilled,
    Ignore(CheckoutTimeout.Received),
    Ignore(PaymentAuthorisedEvent),
    Ignore(PaymentFailedEvent),
    Ignore(FulfillmentFailedEvent),
    Ignore(OrderStatusChangedEvent));

During(Failed,
    Ignore(CheckoutTimeout.Received),
    Ignore(PaymentAuthorisedEvent),
    Ignore(PaymentFailedEvent),
    Ignore(FulfillmentFailedEvent),
    Ignore(OrderStatusChangedEvent));
```

Add regression tests mirroring `PaymentAuthorised_WhenAlreadyCancelled_IsAbsorbedWithoutFault`
for both new states before closing this out.

### CR-02: `OrderCreatedEvent` has no binding in any `During()` block — redelivery faults every state, including the ones this plan "fixed"

**File:** `src/services/orders/ECommerce.Orders.API/Features/Orders/OrderStateMachine.cs:100-230`
**Issue:** `OrderCreatedEvent` is only bound inside `Initially(...)`. None of
`During(Pending, ...)`, `During(Paid, ...)`, or `During(Cancelled, ...)` bind or ignore it. A
redelivered `OrderCreated` message for a saga instance that has already transitioned out of the
`Initial` pseudo-state (i.e., the saga already exists and is `Pending`/`Paid`/`Cancelled`) is
unhandled and faults — even in `Pending`, one of the states this gap-closure plan explicitly
touched and added tests for.

Verified empirically the same way as CR-01: drove a saga to `Pending`, redelivered
`OrderCreated` for the same `orderId`, and observed `Fault<OrderCreated>` published:

```
ECommerce.Orders.Tests.Unit._TempVerifyProbe2.Probe_RedeliveredOrderCreatedWhilePending_DoesItFault [FAIL]
  Expected ... to be False ... but found True.
```

This shows the gap-closure's fix was scoped to the four event types enumerated in the CR-01
ticket rather than to "any late/redelivered event," leaving the same root defect open for a
fifth, very plausible one (the checkout-initiating event itself, published once per checkout
by Orders.API and subject to the same at-least-once broker semantics as everything else in
this saga).

**Fix:** Add `Ignore(OrderCreatedEvent)` (or an explicit no-op `When(OrderCreatedEvent)` binding
consistent with whatever pattern is chosen for CR-01) to every `During()` block, including the
two new ones from CR-01:

```csharp
During(Pending,
    ...,
    Ignore(OrderCreatedEvent));

During(Paid,
    ...,
    Ignore(OrderCreatedEvent));

During(Cancelled,
    ...,
    Ignore(OrderCreatedEvent));
```

## Warnings

### WR-01: `AuthorisePaymentConsumer`'s outcome `switch` has no default arm

**File:** `src/services/payments/ECommerce.Payments.API/Features/Payments/AuthorisePaymentConsumer.cs:26-42`
**Issue:** The `switch (existing.Outcome)` handles `"Authorised"`, `"Failed"`, and `"Refunded"`
explicitly with no `default` case. Any unexpected value (a future outcome added elsewhere
without updating this consumer, e.g. a hypothetical `"PartiallyRefunded"`, or a corrupted row)
silently falls through and does nothing — no publish, no log, no exception. For a payments
consumer this is a silent-failure risk: a redelivered `AuthorisePayment` for such a row would
just vanish with no observability signal.
**Fix:**
```csharp
switch (existing.Outcome)
{
    case "Authorised":
        ...
        break;
    case "Failed":
        ...
        break;
    case "Refunded":
        break;
    default:
        throw new InvalidOperationException(
            $"Unrecognized ProcessedPayment.Outcome '{existing.Outcome}' for checkout {msg.CheckoutId}.");
}
```

### WR-02: New `Ignore()` vs bare `When()` rationale comment is contradicted by this codebase's own passing test

**File:** `src/services/orders/ECommerce.Orders.API/Features/Orders/OrderStateMachine.cs:195-207`
**Issue:** The comment added by this plan claims a bare `When(event)` with no activity chain
"does NOT actually register the event as accepted ... it still throws
`NotAcceptedStateMachineException` at runtime" and cites this as the reason `During(Paid, ...)`
and `During(Cancelled, ...)` were switched from bare `When(...)` to `Ignore(...)`. But
`During(Pending, ...)`'s own trailing catch-all at line 156 (`When(OrderStatusChangedEvent))`,
structurally identical — a bare `When()` with no chain, following several filtered
`When(OrderStatusChangedEvent, filter)` bindings for the same event — was left untouched by
this plan, and the pre-existing test
`OrderStatusChanged_WhenPendingSkipsToFulfilled_TransitionIsRejected` (which exercises exactly
this catch-all) passes. I ran the full `OrderStateMachineTests` suite and confirmed all 7 tests
pass, including that one, with the bare `When()` pattern still in place. This means either the
comment's technical claim is incorrect/overstated, or there's a nuance (specific to
`.Schedule()`/`.Unschedule()` interaction, or to the particular set of events) that isn't
explained — either way, the result is an unexplained inconsistency between `During(Pending, ...)`
and the other two blocks that a future maintainer touching this file will trip over, plus a
misleading rationale in the comments.
**Fix:** Either verify and cite the specific MassTransit behavior/issue precisely (version,
conditions under which bare `When()` fails), or drop the unverified claim and make
`During(Pending, ...)`'s trailing catch-all consistent with the other two blocks (swap to
`Ignore(OrderStatusChangedEvent)` there too, for consistency's sake if nothing else).

### WR-03: `RefundPaymentConsumer`/`AuthorisePaymentConsumer` idempotency guard has no concurrency protection against duplicate concurrent redeliveries

**File:** `src/services/payments/ECommerce.Payments.API/Features/Payments/RefundPaymentConsumer.cs:16-24`
**Issue:** The WR-03 fix (defense-in-depth guard: `existing.Outcome != "Authorised"`) closes the
"refund something never authorised" gap, but the underlying read-check-then-write pattern
(`FindAsync` → mutate → `SaveChangesAsync`) has no optimistic concurrency token. Two concurrent
deliveries of the same `RefundPayment` message (a legitimate broker at-least-once scenario, the
exact scenario WR-03's own comment invokes) can both read `Outcome == "Authorised"` before
either commits, both flip it to `"Refunded"`, and both publish `PaymentRefunded` — defeating the
single-publish idempotency guarantee the existing `RefundPayment_AfterAuthorisedPayment_...`
test only verifies sequentially (`await` between each call), not concurrently. This is a
pre-existing pattern (not newly introduced by the WR-03 line change), but it sits directly in
the code path this plan touched and the deep-dive comment explicitly reasons about redelivery
races, so it's worth calling out as unresolved.
**Fix:** Add a concurrency token (e.g. an EF Core `[Timestamp]`/`RowVersion` column on
`ProcessedPayment`) and catch `DbUpdateConcurrencyException` to treat a lost race as a no-op,
or rely on a unique constraint plus `SaveChangesAsync` failure handling.

## Info

### IN-01: Dead code — unused `GetUserId` helper in `CheckoutEndpoints`

**File:** `src/services/checkout/ECommerce.Checkout.API/Features/Checkout/CheckoutEndpoints.cs:82-85`
**Issue:** `private static string GetUserId(ClaimsPrincipal user)` is defined but never called
anywhere in this file (or, per grep, anywhere in `src/services/checkout`). Pre-existing, not
introduced by this plan, but present in a file this plan modified.
**Fix:** Remove it, or wire it into the endpoints if ownership-scoping by user id was intended
to be used explicitly (currently ownership is enforced indirectly via the bearer token
forwarded to `IOrdersClient`).

### IN-02: Magic string for the demo fulfillment-failure reason

**File:** `src/services/checkout/ECommerce.Checkout.API/Features/Checkout/CheckoutEndpoints.cs:76`
**Issue:** `"Warehouse out of stock"` is a hardcoded literal at the call site. The sibling
`AuthorisePaymentConsumer` uses a named `private const string DeclinedReason` for its
equivalent literal; this file doesn't follow the same convention.
**Fix:**
```csharp
private const string DemoFulfillmentFailureReason = "Warehouse out of stock";
...
await publishEndpoint.Publish(new FulfillmentFailed(
    Guid.NewGuid(), id, Guid.Empty, DateTimeOffset.UtcNow, id,
    DemoFulfillmentFailureReason, DateTimeOffset.UtcNow), ct);
```

### IN-03: New "resumes polling after retry" test doesn't assert the eventual navigation

**File:** `src/frontend/ecommerce-app/src/app/features/checkout/checkout-page/checkout-page.component.spec.ts:191-237`
**Issue:** `resumes polling the existing checkoutId when retry is clicked after a transient
polling error` flushes a terminal `Paid` status after calling `retry()`, but the test only
calls `httpMock.verify()` afterward — it never asserts `navigateSpy` was called with
`['/orders', 'chk-1']`. It confirms the resumed poll hits the right endpoint, but not that the
component actually completes the checkout flow correctly after a retry, which is arguably the
more important behavior the test's name promises.
**Fix:**
```ts
expect(router.navigate).toHaveBeenCalledWith(['/orders', 'chk-1']);
```
(the test already spies on `router.navigate` via `vi.spyOn(router, 'navigate')` — just needs
the assertion.)

---

_Reviewed: 2026-08-12T00:00:00Z_
_Reviewer: Claude (gsd-code-reviewer)_
_Depth: standard_
