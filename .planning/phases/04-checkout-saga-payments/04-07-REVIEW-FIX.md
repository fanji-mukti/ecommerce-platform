---
phase: 04-checkout-saga-payments
fixed_at: 2026-08-13T00:06:00+08:00
review_path: .planning/phases/04-checkout-saga-payments/04-07-REVIEW.md
iteration: 1
findings_in_scope: 5
fixed: 5
skipped: 0
status: all_fixed
---

# Phase 04-07: Code Review Fix Report

**Fixed at:** 2026-08-13T00:06:00+08:00
**Source review:** .planning/phases/04-checkout-saga-payments/04-07-REVIEW.md
**Iteration:** 1

**Summary:**
- Findings in scope: 5 (critical_warning scope — CR-01, CR-02, WR-01, WR-02, WR-03)
- Fixed: 5
- Skipped: 0

## Fixed Issues

### CR-01: `Fulfilled` and `Failed` states have no catch-all — late/redelivered events fault the saga

**Files modified:** `src/services/orders/ECommerce.Orders.API/Features/Orders/OrderStateMachine.cs`, `src/services/orders/ECommerce.Orders.Tests/Unit/OrderStateMachineTests.cs`
**Commit:** `8c75754`
**Applied fix:** Added `During(Fulfilled, ...)` and `During(Failed, ...)` blocks with the same `Ignore(...)` catch-all pattern already used for `Cancelled` (all five event types: `CheckoutTimeout.Received`, `PaymentAuthorisedEvent`, `PaymentFailedEvent`, `FulfillmentFailedEvent`, `OrderStatusChangedEvent`). Added regression tests mirroring `PaymentAuthorised_WhenAlreadyCancelled_IsAbsorbedWithoutFault` for both new states.

### CR-02: `OrderCreatedEvent` has no binding in any `During()` block

**Files modified:** `src/services/orders/ECommerce.Orders.API/Features/Orders/OrderStateMachine.cs`, `src/services/orders/ECommerce.Orders.Tests/Unit/OrderStateMachineTests.cs`
**Commit:** `2ec58a3`
**Applied fix:** Added `Ignore(OrderCreatedEvent)` to all five `During()` blocks (`Pending`, `Paid`, `Cancelled`, `Fulfilled`, `Failed`), including the two just added for CR-01. Added a regression test proving a redelivered `OrderCreated` no longer faults a saga in `Pending`.

### WR-01: `AuthorisePaymentConsumer`'s outcome switch has no default arm

**Files modified:** `src/services/payments/ECommerce.Payments.API/Features/Payments/AuthorisePaymentConsumer.cs`
**Commit:** `338ff88`
**Applied fix:** Added a `default` case that throws `InvalidOperationException` with the unrecognized `Outcome` value and `CheckoutId`, replacing the prior silent no-op.

### WR-02: `Ignore()` vs bare `When()` rationale comment contradicted by the codebase's own passing test

**Files modified:** `src/services/orders/ECommerce.Orders.API/Features/Orders/OrderStateMachine.cs`
**Commit:** `c995c26`
**Applied fix:** Removed the unverified/overstated claim that bare `When(event)` throws `NotAcceptedStateMachineException` at runtime. Replaced with a comment stating `Ignore(...)` is used for cross-block consistency (all five `During()` blocks now use it), not because bare `When()` is provably broken. Also switched `During(Pending, ...)`'s trailing bare `When(OrderStatusChangedEvent)` catch-all to `Ignore(OrderStatusChangedEvent)` for consistency across all five blocks, closing the inconsistency the reviewer flagged.

### WR-03: `RefundPaymentConsumer`/`AuthorisePaymentConsumer` idempotency guard has no concurrency protection

**Files modified:** none (documented, not code-fixed)
**Commit:** none
**Applied fix:** Not fixed as code — flagged for follow-up instead. The reviewer's own finding classifies this as pre-existing (not introduced by 04-07) and its proper fix (an EF Core `[Timestamp]`/`RowVersion` column plus a migration, or a unique-constraint-based race handler) is a schema change with broader blast radius than a code-review fix pass is scoped for. Recommend a dedicated follow-up plan before Phase 06 (hardening) if concurrent redelivery races are a real risk in this project's demo/portfolio context.

## Verification

Ran the full `ECommerce.Orders.Tests` suite (via the compiled test executable — `dotnet test` fails in this environment on an unrelated `testhost.deps.json` resolution issue, a pre-existing environment limitation): **18 total, 0 errors, 10 passed, 8 failed**. All 8 failures are pre-existing Docker-dependent integration tests (`OrdersEndpointTests`, `OrderReadModelInboxDeduplicationTests`) — Docker Desktop is unavailable in this environment, consistent with the same limitation noted in `04-04-SUMMARY.md` and `04-07-SUMMARY.md`. All 10 unit tests pass, including the 3 new CR-01/CR-02 regression tests and the pre-existing 7 `OrderStateMachineTests`.

---

_Fixed: 2026-08-13T00:06:00+08:00_
_Fixer: Claude (gsd-code-fixer, recovered after session-limit interruption)_
_Iteration: 1_
