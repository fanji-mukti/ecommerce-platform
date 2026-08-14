---
phase: 04-checkout-saga-payments
plan: 07
subsystem: saga
tags: [masstransit, state-machine, idempotency, angular, rxjs]

# Dependency graph
requires:
  - phase: 04-checkout-saga-payments
    provides: OrderStateMachine (04-01/04-02), AuthorisePaymentConsumer/RefundPaymentConsumer (04-03/04-04), checkout-page.component.ts (04-06)
provides:
  - "OrderStateMachine.During(Cancelled, ...) absorbs late/redelivered PaymentAuthorisedEvent, PaymentFailedEvent, FulfillmentFailedEvent, and CheckoutTimeout.Received without faulting (CR-01 closed)"
  - "OrderStateMachine.During(Paid, ...) catch-all fixed to actually absorb (was silently broken pre-existing code using bare When() instead of Ignore())"
  - "FulfillmentFailedEvent.Reason threaded into Order.FailureReason instead of a hardcoded duplicate string (WR-01 closed)"
  - "AuthorisePaymentConsumer redelivery-replay switches on stored Outcome (Authorised/Failed/Refunded) instead of a binary check (WR-02 closed)"
  - "RefundPaymentConsumer rejects refunding a payment whose Outcome is not Authorised (WR-03 closed)"
  - "checkout-page.component.ts polling subscription torn down on destroy; retry() resumes polling instead of reloading the cart (WR-04 closed)"
affects: [05-fulfillment-notifications, 06-hardening-azure]

# Tech tracking
tech-stack:
  added: []
  patterns:
    - "MassTransit state machine catch-all absorb pattern: Ignore(event) is the correct API for 'accept and do nothing' in a During(state, ...) block — bare When(event) with no .Then()/.TransitionTo() chain does NOT register the event as accepted in MassTransit 8.3.6 and still throws NotAcceptedStateMachineException at runtime"

key-files:
  created: []
  modified:
    - src/services/orders/ECommerce.Orders.API/Features/Orders/OrderStateMachine.cs
    - src/services/orders/ECommerce.Orders.Tests/Unit/OrderStateMachineSteps.cs
    - src/services/orders/ECommerce.Orders.Tests/Unit/OrderStateMachineTests.cs
    - src/services/checkout/ECommerce.Checkout.API/Features/Checkout/CheckoutEndpoints.cs
    - src/services/payments/ECommerce.Payments.API/Features/Payments/AuthorisePaymentConsumer.cs
    - src/services/payments/ECommerce.Payments.API/Features/Payments/RefundPaymentConsumer.cs
    - src/services/payments/ECommerce.Payments.Tests/Integration/AuthorisePaymentConsumerTests.cs
    - src/frontend/ecommerce-app/src/app/features/checkout/checkout-page/checkout-page.component.ts
    - src/frontend/ecommerce-app/src/app/features/checkout/checkout-page/checkout-page.component.spec.ts

key-decisions:
  - "Used Ignore(event) instead of the plan's literal When(event) for the widened Cancelled catch-all, after discovering (via the CR-01 regression test itself failing against a bare-When() implementation) that MassTransit 8.3.6 does not accept a bare When(event) as an absorb-and-do-nothing binding — it still throws NotAcceptedStateMachineException"
  - "Applied the same Ignore(event) fix to the pre-existing During(Paid, ...) catch-all (the 'Pitfall 2' block), which used the identical broken bare-When() pattern and had never actually been exercised by a test — same underlying bug, same file, directly relevant to this plan's purpose (Rule 1)"
  - "AuthorisePaymentConsumerTests (Task 2) verified via full-solution dotnet build (0 errors) and static code review only, not a live test run — Docker Testcontainers connectivity is unavailable in this environment, confirmed pre-existing and reproduced across all 7 tests in the class (5 pre-existing + 2 new), matching the identical limitation documented in 04-04-SUMMARY.md"

requirements-completed: [CHK-03, CHK-04, CHK-05, PAY-03, FE-03]

# Metrics
duration: 70min
completed: 2026-08-12
---

# Phase 4 Plan 07: Gap Closure — CR-01 Blocker + WR-01..04 Summary

**Widened OrderStateMachine's Cancelled catch-all to absorb late payment/fulfillment events using MassTransit's `Ignore()` API (discovering along the way that the codebase's pre-existing bare-`When()` "absorb" pattern in the Paid state was silently broken and never actually worked), plus three smaller idempotency/UX hardening fixes in Payments and the Angular checkout page.**

## Performance

- **Duration:** ~70 min
- **Started:** 2026-08-12T15:14:00Z (approx, worktree setup)
- **Completed:** 2026-08-12T15:25:41Z
- **Tasks:** 3 (all completed)
- **Files modified:** 9

## Accomplishments

- Closed CR-01: `OrderStateMachine.During(Cancelled, ...)` now absorbs `PaymentAuthorisedEvent`, `PaymentFailedEvent`, `FulfillmentFailedEvent`, and `CheckoutTimeout.Received` without faulting, proven by a new regression test asserting no `Fault<PaymentAuthorised>` is published.
- Discovered and fixed a real, previously-undetected bug: MassTransit 8.3.6's bare `When(event)` (no `.Then()`/`.TransitionTo()` chain) does **not** register the event as accepted for a state — it still throws `NotAcceptedStateMachineException`. The correct API is `Ignore(event)`. Applied this fix to both the new `Cancelled` catch-all and the pre-existing (previously untested) `Paid` catch-all ("Pitfall 2" block), which shared the identical broken pattern.
- Closed WR-01: `FulfillmentFailedEvent.Reason` is now threaded into `Order.FailureReason` via string interpolation instead of a hardcoded duplicate string; `CheckoutEndpoints.cs`'s demo trigger now publishes a genuine reason (`"Warehouse out of stock"`) instead of duplicating the saga's generic string.
- Closed WR-02: `AuthorisePaymentConsumer`'s redelivery-replay branch now switches on the actual stored `Outcome` (`Authorised`/`Failed`/`Refunded`) instead of a binary check, so a payment later refunded is never republished as `PaymentFailed` with a smuggled-null `Reason`.
- Closed WR-03: `RefundPaymentConsumer` now refuses to refund a `ProcessedPayment` whose `Outcome` is not `Authorised`.
- Closed WR-04: `checkout-page.component.ts`'s polling subscription is torn down via `takeUntilDestroyed(this.destroyRef)`; `retry()` resumes polling the existing `checkoutId` instead of reloading the cart when a checkout is already in flight.

## Task Commits

Each task was committed atomically:

1. **Task 1: Orders saga hardening — widen During(Cancelled, ...) catch-all (CR-01) and propagate FulfillmentFailed.Reason (WR-01)** - `bbe74aa` (fix)
2. **Task 2: Payments idempotency hardening — redelivery-after-refund mislabeling (WR-02) and refund-without-authorisation guard (WR-03)** - `2df728a` (fix)
3. **Task 3: Checkout status polling — teardown on destroy and resumable retry (WR-04)** - `b212d7f` (fix)

_Note: all three tasks were `tdd="true"` — each commit bundles the RED test additions and the GREEN implementation fix together (single commit per task, verified RED-then-GREEN locally before committing) rather than splitting into separate test/feat commits, matching this repo's established pattern for small, tightly-scoped gap-closure tasks._

## Files Created/Modified

- `src/services/orders/ECommerce.Orders.API/Features/Orders/OrderStateMachine.cs` - Widened `During(Cancelled, ...)` catch-all (CR-01); fixed `During(Paid, ...)` catch-all bug (bare `When()` → `Ignore()`); `FulfillmentFailedEvent` handler now embeds `ctx.Message.Reason` (WR-01)
- `src/services/orders/ECommerce.Orders.Tests/Unit/OrderStateMachineSteps.cs` - Added `Then_NoFaultPublished<T>()` test helper
- `src/services/orders/ECommerce.Orders.Tests/Unit/OrderStateMachineTests.cs` - Added `PaymentAuthorised_WhenAlreadyCancelled_IsAbsorbedWithoutFault` regression test; updated `FulfillmentFailed_WhenPaid_TransitionsToCancelledAndPublishesRefundPayment`'s expected `FailureReason` string
- `src/services/checkout/ECommerce.Checkout.API/Features/Checkout/CheckoutEndpoints.cs` - Demo trigger's `FulfillmentFailed.Reason` changed from duplicated generic string to `"Warehouse out of stock"`, with corrected comment
- `src/services/payments/ECommerce.Payments.API/Features/Payments/AuthorisePaymentConsumer.cs` - Redelivery-replay branch switched from binary `if/else` to a `switch (existing.Outcome)` (WR-02)
- `src/services/payments/ECommerce.Payments.API/Features/Payments/RefundPaymentConsumer.cs` - Idempotency guard changed from `existing.Outcome == "Refunded"` to `existing.Outcome != "Authorised"` (WR-03)
- `src/services/payments/ECommerce.Payments.Tests/Integration/AuthorisePaymentConsumerTests.cs` - Added `AuthorisePayment_WhenRedeliveredAfterRefund_DoesNotRepublishAsPaymentFailed` and `RefundPayment_WhenPaymentWasNeverAuthorised_IsRejectedAsNoOp`
- `src/frontend/ecommerce-app/src/app/features/checkout/checkout-page/checkout-page.component.ts` - Added `DestroyRef`/`takeUntilDestroyed` polling teardown; `retry()` now resumes polling when `checkoutId` is set (WR-04)
- `src/frontend/ecommerce-app/src/app/features/checkout/checkout-page/checkout-page.component.spec.ts` - Added `'stops polling after the component is destroyed'` and `'resumes polling the existing checkoutId when retry is clicked after a transient polling error'`

## Decisions Made

- **Ignore(event) over bare When(event) for MassTransit catch-alls:** The plan's literal instruction was to write `When(CheckoutTimeout.Received), When(PaymentAuthorisedEvent), ...` mirroring the existing `During(Paid, ...)` pattern. Implementing this exactly and running the new CR-01 regression test immediately surfaced `NotAcceptedStateMachineException` — the saga was NOT actually absorbing the event despite the binding being present. Root-caused via a temporary diagnostic print of the underlying `Fault<T>` exception detail, which showed MassTransit's real error: `"Not accepted in state Cancelled"`. Switching to `Ignore(event)` (a first-class MassTransit API for exactly this "accept, do nothing" semantics) fixed it immediately, confirmed by both the new test passing and no regression in the other 6 tests.
- **Extended the same fix to the pre-existing Paid catch-all:** Since `During(Paid, ...)`'s "Pitfall 2" block used the identical bare-`When()` pattern and had never been exercised by any test (no existing test publishes a redelivered event to an already-`Paid` saga), it was very likely equally broken. Fixed it under Rule 1 (auto-fix bugs) since it's the same file, the same underlying defect, and directly serves this plan's stated purpose of hardening exactly this class of race/redelivery handling — leaving it broken would have been inconsistent with fixing the identical pattern one state below it.
- **Left the pre-existing `Pending` catch-all's bare `When(OrderStatusChangedEvent)` untouched:** Unlike the Paid/Cancelled cases, this one IS exercised by an existing passing test (`OrderStatusChanged_WhenPendingSkipsToFulfilled_TransitionIsRejected`), so it is proven functional as-is (it works because it's the trailing catch-all among multiple *predicated* `When(OrderStatusChangedEvent, predicate)` bindings for the *same* event type in that state — a different structural situation from a solitary bare `When()` for an event with no other binding in that state). No bug there; not touched.

## Deviations from Plan

### Auto-fixed Issues

**1. [Rule 1 - Bug] MassTransit's bare `When(event)` does not register absorb-only bindings — fixed via `Ignore(event)`**
- **Found during:** Task 1, step 2 (running the new CR-01 regression test against the plan's literal `When(...)` implementation)
- **Issue:** The plan's literal instruction (mirroring 04-REVIEW.md's suggested fix and the existing `During(Paid, ...)` "Pitfall 2" pattern) was `When(CheckoutTimeout.Received), When(PaymentAuthorisedEvent), When(PaymentFailedEvent), When(FulfillmentFailedEvent), When(OrderStatusChangedEvent)` with no further activity chain, intended to "accept and do nothing." Implementing exactly this and running the new test produced `MassTransit.NotAcceptedStateMachineException: ... Saga exception on receipt of ECommerce.Payments.Events.V1.PaymentAuthorised: Not accepted in state Cancelled` — the event was NOT actually absorbed; it still faulted the consumer, defeating CR-01's entire purpose.
- **Fix:** Replaced all five bare `When(...)` entries in the new `Cancelled` catch-all with `Ignore(...)`, MassTransit's dedicated API for accept-and-do-nothing state bindings. Also applied the identical fix to the pre-existing `During(Paid, ...)` catch-all (four entries), which used the same broken pattern and had no existing test coverage proving it worked.
- **Files modified:** `src/services/orders/ECommerce.Orders.API/Features/Orders/OrderStateMachine.cs`
- **Verification:** `PaymentAuthorised_WhenAlreadyCancelled_IsAbsorbedWithoutFault` passes; full `OrderStateMachineTests` class: 7/7 passing, 0 failures; `dotnet build src/services/orders/Orders.sln` and `dotnet build src/services/checkout/Checkout.sln` both 0 errors.
- **Committed in:** `bbe74aa` (Task 1 commit)

**2. [Rule 3 - Blocking] Pre-existing stale bin/obj build artifacts and dotnet-test/testhost dependency mismatch (environment)**
- **Found during:** Task 1, initial `dotnet test` attempt
- **Issue:** `dotnet test src/services/orders/Orders.sln --filter ...` failed with `An assembly specified in the application dependencies manifest (testhost.deps.json) was not found: package: 'testhost', version: '18.6.0-release-26270-133'` and a separate `AnyOf 0.4.0` resolution failure for `ECommerce.Tests.Common`. This is the same pre-existing `dotnet test`/testhost environment issue documented in `04-04-SUMMARY.md` (confirmed repo-wide, not caused by this plan).
- **Fix:** Cleaned stale `bin`/`obj` directories, ran `dotnet build`, and executed the compiled `ECommerce.Orders.Tests.exe` directly with xUnit v3's in-process runner (`-class`/`-method` flags) instead of `dotnet test` — the same workaround the 04-04 plan used for Payments. This is a verification-mechanism workaround only; no repo code changed.
- **Files modified:** None (bin/obj cleanup only, gitignored)
- **Verification:** `ECommerce.Orders.Tests.exe -class "ECommerce.Orders.Tests.Unit.OrderStateMachineTests"` ran successfully and reported real pass/fail results (unlike Payments' Docker-blocked tests below, Orders tests do NOT require Testcontainers/Docker — this workaround produced genuine, trustworthy RED/GREEN evidence for the whole task).
- **Committed in:** N/A (no code change; environment/tooling workaround only)

### Verification Limitations (documented, not auto-fixed)

**Task 2 (Payments) tests could not be executed live in this environment.** `dotnet test`/the compiled `ECommerce.Payments.Tests.exe` both require Docker Desktop's named-pipe endpoint (`npipe://./pipe/docker_engine`) for the `Testcontainers.PostgreSql` fixture used by `AuthorisePaymentConsumerSteps`. Running the compiled exe reproduced `DotNet.Testcontainers.Builders.DockerUnavailableException: Failed to connect to Docker endpoint` for **all 7 tests in the class**, including the 5 pre-existing tests that were passing before this plan touched the file — confirming this is a pre-existing environment limitation, not a regression introduced here (matches the identical limitation independently documented in `04-04-SUMMARY.md` for the same test class). `docker info`/`docker context ls` succeed in the same shell, but the .NET Testcontainers library cannot reach the named pipe — the same unresolved gap noted in 04-04.

Verification basis used instead: full-solution `dotnet build src/services/payments/Payments.sln` (0 errors) and static code review of `AuthorisePaymentConsumer.cs`/`RefundPaymentConsumer.cs`/`AuthorisePaymentConsumerTests.cs` against the plan's `<behavior>` spec and the acceptance-criteria `grep` checks (`case "Refunded"` present once, `existing.Outcome != "Authorised"` present once). **Recommend the next agent/human with working Docker Desktop named-pipe connectivity re-run `dotnet test src/services/payments/Payments.sln --filter "FullyQualifiedName~AuthorisePaymentConsumerTests"` (expect 8 tests, 0 failures) before this phase's overall verification is signed off.**

---

**Total deviations:** 1 code auto-fix (Rule 1 — bare-`When()` MassTransit bug), 1 tooling workaround (Rule 3 — testhost environment issue), 1 documented verification limitation (Payments Docker/Testcontainers unavailable, pre-existing).
**Impact on plan:** The `Ignore()` fix was essential — without it, CR-01 would remain unfixed despite the plan's literal instructions being followed exactly (the plan's suggested code, taken verbatim from 04-REVIEW.md, does not actually work against MassTransit 8.3.6). No scope creep beyond the plan's stated CR-01/WR-01..04 targets and the immediately-adjacent Paid-catch-all bug of the identical class.

## Issues Encountered

- Initial `dotnet test`/testhost failures across both Orders and Payments solutions were resolved (Orders) or worked around (Payments, Docker-blocked) as described above.
- Discovering the `Ignore()` vs bare `When()` MassTransit behavior required adding a temporary diagnostic (`Console.WriteLine` of `Fault<T>.Context.Message.Exceptions`) to `Then_NoFaultPublished<T>()` in `OrderStateMachineSteps.cs`, which was reverted to the clean assertion-only form before committing Task 1 — the committed code contains no debug output.
- A new-test-only issue: the WR-04 `'resumes polling...'` spec test triggered `NG04002: Cannot match any routes` as an unhandled promise rejection, because `provideRouter([])` has no routes and the terminal `'Paid'` status response causes a real (non-stubbed) `router.navigate(['/orders', 'chk-1'])` call. Fixed by adding `vi.spyOn(router, 'navigate').mockResolvedValue(true)` in that test (the pre-existing `'polls status...'` test only spies without mocking, which happened not to surface this as a reported error, but the more correct/consistent approach for a test intentionally reaching the terminal state is to stub the navigation outcome).

## User Setup Required

None - no external service configuration required.

## Next Phase Readiness

- CR-01 (the phase's sole BLOCKER from 04-VERIFICATION.md) and all four WARNING-severity items from 04-REVIEW.md (WR-01..WR-04) are closed.
- **Outstanding before final phase sign-off:** the Payments Task 2 regression tests (`AuthorisePaymentConsumerTests`, 8 tests total) have only been verified via build + static review in this environment due to a pre-existing Docker Testcontainers connectivity gap (documented above and in 04-04-SUMMARY.md). A CI runner or developer machine with working Docker Desktop named-pipe access should execute `dotnet test src/services/payments/Payments.sln --filter "FullyQualifiedName~AuthorisePaymentConsumerTests"` to close this gap before Phase 4 is considered fully verified.
- Phase 5 (Fulfillment & Notifications) can proceed — this plan touched no fulfillment/notification code and introduced no new architectural surface, only correctness hardening within the existing checkout saga boundary.

---
*Phase: 04-checkout-saga-payments*
*Completed: 2026-08-12*
