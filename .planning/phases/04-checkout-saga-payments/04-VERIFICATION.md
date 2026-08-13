---
phase: 04-checkout-saga-payments
verified: 2026-08-13T23:15:00Z
status: human_needed
score: 5/5 must-haves verified
overrides_applied: 0
re_verification:
  previous_status: gaps_found
  previous_score: 4/5
  gaps_closed:
    - "Fulfillment-failure and timeout compensation paths leave the saga in a consistent terminal state (SC4/SC5) — During(Cancelled, ...) now absorbs all four late/redelivered event types instead of only OrderStatusChangedEvent"
  gaps_remaining: []
  regressions: []
human_verification:
  - test: "Run the full Docker Compose / Aspire stack and click through Cart -> /checkout -> Place Order -> watch the mat-stepper update in real time -> auto-redirect to /orders/:id, for both a normal-priced cart (happy path) and a .99-ending cart (PaymentFailed demo trigger)"
    expected: "Stepper advances Started -> AwaitingPayment -> Paid (or -> Cancelled with a visible failure reason), page auto-navigates to /orders/:id with no manual refresh, matching SC1/SC2/SC3"
    why_human: "Sandboxed environment has no Docker/npipe access (confirmed again this pass — Testcontainers-based integration tests fail with DockerUnavailableException). Only build success, in-memory saga unit tests, Angular unit tests, and static code wiring were independently verifiable."
  - test: "Click 'simulate fulfillment failure' on a Paid order via the checkout/order-detail UI, twice in rapid succession, against a live running system (real ASB, not the in-memory harness)"
    expected: "Second delivery is absorbed by During(Cancelled, ...)'s Ignore(FulfillmentFailedEvent) — no dead-lettered message, no faulted consumer. This is now proven by an in-memory saga-harness unit test (PaymentAuthorised_WhenAlreadyCancelled_IsAbsorbedWithoutFault and siblings), but has not been observed against a real broker with real at-least-once redelivery timing."
    why_human: "In-memory MassTransit test harness proves the state-machine logic is correct; it does not prove ASB's actual redelivery/concurrency behavior matches the harness's synchronous delivery model."
  - test: "Leave a checkout un-actioned past the configured 15-minute timeout (or temporarily lower Checkout:TimeoutMinutes) on a live running system and observe whether any in-flight AuthorisePayment response arrives after the timeout has already cancelled the order"
    expected: "No UnhandledEventException / faulted consumer / dead-lettered message in Orders.API logs — During(Cancelled, ...) now ignores PaymentAuthorisedEvent/PaymentFailedEvent"
    why_human: "Primary real-world trigger for the original CR-01 gap; requires live timing-dependent observation against the real ASB emulator, not just the in-memory harness."
---

**Post-verification addendum (2026-08-13):** The WARNING-level "New Finding" below (`During(Pending, ...)` missing `Ignore(FulfillmentFailedEvent)`) was closed immediately after this verification pass, commit `a692bff` — the one-line `Ignore(FulfillmentFailedEvent)` was added to `During(Pending, ...)` along with a new regression test (`FulfillmentFailed_WhenPending_IsAbsorbedWithoutFault`). Full `OrderStateMachineTests` suite re-run: 11/11 unit tests pass (up from 10; only the same 8 pre-existing Docker-dependent integration tests fail). All five `During()` blocks now bind or ignore all six registered event types with zero remaining gaps. `WR-03` (concurrent-redelivery race protection) remains the sole explicitly-deferred item, documented in `04-07-REVIEW-FIX.md` as a recommended follow-up before Phase 6.

# Phase 4: Checkout Saga & Payments Verification Report

**Phase Goal:** A user clicks "Place Order" and the checkout saga orchestrates Order creation, simulated payment, and compensation paths end-to-end — the headline demo.
**Verified:** 2026-08-13T23:15:00Z
**Status:** human_needed
**Re-verification:** Yes — after gap closure (04-07 + 04-07-REVIEW-FIX)

**Context:** This is the third verification pass for this phase. The first pass (`04-VERIFICATION.md`, superseded by this file) found a BLOCKER: `During(Cancelled, ...)` faulted on late/redelivered `PaymentAuthorisedEvent`/`PaymentFailedEvent`/`FulfillmentFailedEvent` (CR-01). Plan 04-07 attempted to close this. A scoped code review of 04-07's own changes (`04-07-REVIEW.md`) then found the fix was *itself* incomplete — `During(Fulfilled, ...)` and `During(Failed, ...)` had **zero** event bindings at all (same defect class, wider surface), and `OrderCreatedEvent` was unbound in every `During()` block. Those second-order gaps were closed via `04-07-REVIEW-FIX.md` (commits `8c75754`, `2ec58a3`, `338ff88`, `c995c26`). This pass independently re-derives and re-verifies the current state of `OrderStateMachine.cs` from the actual file contents and a live test run — not from any of the prior narrative documents.

## Goal Achievement

### Observable Truths

| # | Truth | Status | Evidence |
|---|-------|--------|----------|
| 1 | SC1 — User can POST `/checkout` (202 + checkoutId) then poll `GET /checkout/{id}`, reflected live in Angular `/checkout` and `/orders/:id` | ✓ VERIFIED | `CheckoutEndpoints.cs` implements both routes; `checkout-page.component.ts` polls every 1500ms via `interval(1500).pipe(switchMap(...), takeWhile(...), takeUntilDestroyed(...))` and navigates to `/orders/:id` on terminal state; routes confirmed registered in `app.routes.ts` (`checkout`, `orders/:id`). `CheckoutStatusMapper` correctly translates Orders' persisted vocabulary (Pending/Paid/Cancelled/Failed/Fulfilled) to the checkout-facing vocabulary (Started/AwaitingPayment/Paid/Cancelled/Failed/Fulfilled). |
| 2 | SC2 — Happy path drives `Started -> AwaitingPayment -> Paid`, idempotent payment processing keyed by `checkoutId` | ✓ VERIFIED | `OrderStateMachine.Initially()` publishes `AuthorisePayment` and transitions `Pending`; `During(Pending, When(PaymentAuthorisedEvent)...)` transitions to `Paid`. `AuthorisePaymentConsumer` looks up `ProcessedPayment` by `CheckoutId` (PK) before ever inserting — redelivery replays the stored outcome via a full `switch` over `Outcome` (`Authorised`/`Failed`/`Refunded`/`default→throw`), never reprocessing (PAY-03). Confirmed by direct code read of `AuthorisePaymentConsumer.cs`. |
| 3 | SC3 — `.99`-ending cart deterministically triggers `PaymentFailed`; saga cancels the order | ✓ VERIFIED | `AuthorisePaymentConsumer.cs`: `cents == 99m` deterministic rule (line 56). `During(Pending, When(PaymentFailedEvent)...)` sets `FailureReason` and transitions to `Cancelled`. Regression test `PaymentFailed_WhenPending_TransitionsToCancelledWithFailureReason` executed live in this pass — **passed** (see Behavioral Spot-Checks). |
| 4 | SC4 — Fulfillment-failure compensation publishes `RefundPayment` + cancels the order, leaving a **consistent terminal state** | ✓ VERIFIED (gap closed) | `During(Paid, When(FulfillmentFailedEvent)...)` publishes `RefundPayment` then `OrderStatusChanged(NewStatus="Cancelled")`, transitions to `Cancelled`. `During(Cancelled, ...)` now `Ignore()`s all of `CheckoutTimeout.Received`, `PaymentAuthorisedEvent`, `PaymentFailedEvent`, `FulfillmentFailedEvent`, `OrderStatusChangedEvent`, `OrderCreatedEvent` — confirmed by direct read of `OrderStateMachine.cs:243-251`. Regression tests `PaymentAuthorised_WhenAlreadyCancelled_IsAbsorbedWithoutFault` and `PaymentAuthorised_WhenAlreadyFulfilled_IsAbsorbedWithoutFault` executed live in this pass — **passed**. |
| 5 | SC5 — ~15-minute timeout cascades the same compensation path, leaving no orphaned orders/payments | ✓ VERIFIED (gap closed) | `Schedule(CheckoutTimeout, ...)` set to `TimeSpan.FromMinutes(checkoutOptions.Value.TimeoutMinutes)`; `CheckoutOptions.TimeoutMinutes` defaults to `15` (`appsettings.json` and `CheckoutOptions.cs` both confirmed). `During(Pending, When(CheckoutTimeout.Received)...)` transitions to `Cancelled` with a distinct failure reason. A late `PaymentAuthorisedEvent`/`PaymentFailedEvent` arriving after this transition is now absorbed by `During(Cancelled, ...)`'s widened catch-all (same evidence as truth #4). Regression test `CheckoutTimeoutExpired_WhenPaymentOutcomeNeverArrives_TransitionsToCancelledWithFailureReason` executed live in this pass — **passed**. `During(Fulfilled, ...)` and `During(Failed, ...)` — previously **zero bindings at all** (04-07-REVIEW CR-01) — now both `Ignore()` all six event types identically to `During(Cancelled, ...)`; regression tests `PaymentAuthorised_WhenAlreadyFulfilled_IsAbsorbedWithoutFault` and `PaymentFailed_WhenAlreadyFailed_IsAbsorbedWithoutFault` executed live — **passed**. `OrderCreatedEvent` — previously unbound in every `During()` block (04-07-REVIEW CR-02) — now `Ignore()`d in all five; regression test `OrderCreated_WhenRedeliveredWhilePending_IsAbsorbedWithoutFault` executed live — **passed**. |

**Score:** 5/5 truths verified. The prior BLOCKER (CR-01) and its own second-order gaps (04-07-REVIEW's CR-01/CR-02) are closed and independently confirmed against the current file contents and a live test run, not just the narrative in 04-07-REVIEW-FIX.md.

### New Finding — Not a Blocker, Flagged for Attention

Independently auditing all five `During()` blocks against all six registered event types (`OrderCreatedEvent`, `OrderStatusChangedEvent`, `PaymentAuthorisedEvent`, `PaymentFailedEvent`, `FulfillmentFailedEvent`, `CheckoutTimeout.Received`) — the exact check this verification pass was asked to perform — turned up one remaining gap of the *same defect class*, not previously identified in any prior review:

**`During(Pending, ...)` (`OrderStateMachine.cs:100-168`) has no binding — not even an `Ignore(...)` — for `FulfillmentFailedEvent`.** Every other block (`Paid`, `Cancelled`, `Fulfilled`, `Failed`) binds or ignores all six event types; `Pending` covers five of six. If a `FulfillmentFailedEvent` message were ever delivered to a saga instance still in `Pending`, MassTransit would raise the same `UnhandledEventException`/fault that CR-01 and CR-02 both existed to eliminate.

**Reachability assessed and found currently unreachable in production code:** the only publisher of `FulfillmentFailed` in this codebase is `CheckoutEndpoints.cs`'s demo trigger (`POST /checkout/{id}/simulate-fulfillment-failure`), which reads the order snapshot from `IOrdersClient.GetStatusAsync` (the Orders read model) and rejects with `400` unless `snapshot.Status == "Paid"` (line 67-68). That read model is itself populated by consuming the saga's own `OrderStatusChanged(NewStatus="Paid")` event — so by construction, the read model cannot report `"Paid"` before the saga instance has already left `Pending`. No other code path publishes `FulfillmentFailed` (confirmed by `grep -rn "new FulfillmentFailed("` — only the demo endpoint and the test harness). This means the gap cannot currently be triggered by any exercised code path, unlike CR-01/CR-02 which were reachable through ordinary broker redelivery of already-wired publishers.

**Why this still matters:** Phase 5 (Fulfillment service) is expected to introduce a real publisher of `FulfillmentFailed` that will not necessarily be gated the same way the demo endpoint is, and the entire point of the CR-01/CR-02 fix cycle was to make every `During()` block defensively absorb every registered event regardless of whether a reachability analysis currently rules it out (that exact reasoning is what let CR-01/CR-02 slip through two review passes). This is classified as a **WARNING**, not a BLOCKER, because none of the 5 roadmap success criteria are violated by it today and no test demonstrates an actual fault — but it is a real, specific, and easily-closed gap (`Ignore(FulfillmentFailedEvent)` added to `During(Pending, ...)`, one line) that a future maintainer or Phase 5 planner should not have to rediscover.

### Required Artifacts

| Artifact | Expected | Status | Details |
|----------|----------|--------|---------|
| `src/services/orders/ECommerce.Orders.API/Features/Orders/OrderStateMachine.cs` | All 5 `During()` blocks bind/ignore all registered event types; CR-01/CR-02 fully closed | ⚠️ VERIFIED-WITH-WARNING | 4/5 blocks (`Paid`, `Cancelled`, `Fulfilled`, `Failed`) are complete; `Pending` is missing `Ignore(FulfillmentFailedEvent)` — see New Finding above. Not reachable via any current code path. |
| `src/services/orders/ECommerce.Orders.Tests/Unit/OrderStateMachineTests.cs` | 10 unit tests incl. 3 new CR-01/CR-02 regressions | ✓ VERIFIED | Compiled and executed directly in this pass: `Total: 10, Errors: 0, Failed: 0`. All 3 new regression tests (`PaymentAuthorised_WhenAlreadyFulfilled_IsAbsorbedWithoutFault`, `PaymentFailed_WhenAlreadyFailed_IsAbsorbedWithoutFault`, `OrderCreated_WhenRedeliveredWhilePending_IsAbsorbedWithoutFault`) present and passing. |
| `src/services/payments/ECommerce.Payments.API/Features/Payments/AuthorisePaymentConsumer.cs` | WR-01 default-arm throw, WR-02 full Outcome switch | ✓ VERIFIED | `switch (existing.Outcome)` has `case "Authorised"`, `case "Failed"`, `case "Refunded"`, `default: throw new InvalidOperationException(...)`. Confirmed at lines 26-49. |
| `src/services/payments/ECommerce.Payments.API/Features/Payments/RefundPaymentConsumer.cs` | WR-03 `existing.Outcome != "Authorised"` guard | ✓ VERIFIED | Confirmed at line 17. Concurrency-race hardening (optimistic concurrency token) explicitly deferred, not silently dropped — documented in `04-07-REVIEW-FIX.md` as a recommended follow-up plan before Phase 6. |
| `src/frontend/ecommerce-app/src/app/features/checkout/checkout-page/checkout-page.component.ts` | WR-04 `takeUntilDestroyed` + resumable retry | ✓ VERIFIED | `takeUntilDestroyed(this.destroyRef)` present as the final pipe operator (line 126); `retry()` branches on `checkoutId()` being set to resume polling instead of unconditionally reloading the cart (lines 81-93). Angular unit tests (6/6) executed live in this pass — passed. |
| `docs/adr/0009-checkout-saga-state-reconciliation.md` | MADR record of "Started" synthesis decision | ✓ VERIFIED | Exists (unchanged since prior pass, not re-read in full this pass — no code in this file was touched by 04-07/04-07-REVIEW-FIX). |
| `src/services/orders/ECommerce.Orders.API/Features/Orders/CheckoutOptions.cs` | Configurable `Checkout:TimeoutMinutes`, default 15 | ✓ VERIFIED | `public double TimeoutMinutes { get; set; } = 15;` confirmed; `appsettings.json` also sets `15`. |

### Key Link Verification

| From | To | Via | Status | Details |
|------|-----|-----|--------|---------|
| `During(Cancelled, ...)` | late `PaymentAuthorisedEvent`/`PaymentFailedEvent`/`FulfillmentFailedEvent`/`CheckoutTimeout.Received`/`OrderStatusChangedEvent`/`OrderCreatedEvent` | `Ignore(...)` catch-all | ✓ WIRED | Was `NOT_WIRED` in the prior pass (CR-01) — now confirmed wired for all 6 event types at `OrderStateMachine.cs:243-251`. |
| `During(Fulfilled, ...)` | all 6 event types | `Ignore(...)` catch-all | ✓ WIRED | Previously had **zero** bindings (04-07-REVIEW CR-01) — now confirmed wired at lines 260-268. |
| `During(Failed, ...)` | all 6 event types | `Ignore(...)` catch-all | ✓ WIRED | Previously had **zero** bindings (04-07-REVIEW CR-01) — now confirmed wired at lines 270-278. |
| `During(Pending, ...)` | `OrderCreatedEvent`, `OrderStatusChangedEvent`, `PaymentAuthorisedEvent`, `PaymentFailedEvent`, `CheckoutTimeout.Received` | `When(...)`/`Ignore(...)` | ✓ WIRED | 5 of 6 registered event types bound. |
| `During(Pending, ...)` | `FulfillmentFailedEvent` | (none — missing) | ✗ NOT_WIRED | New finding — see above. Not currently reachable, classified WARNING not BLOCKER. |
| `AuthorisePaymentConsumer` | `PaymentAuthorised`/`PaymentFailed` (redelivery replay) | `switch (existing.Outcome)` with `default: throw` | ✓ WIRED | Confirmed; WR-01/WR-02 both closed. |
| `RefundPaymentConsumer` | `ProcessedPayment.Outcome` guard | `existing.Outcome != "Authorised"` | ✓ WIRED | Confirmed; WR-03 closed for the sequential case. Concurrent-redelivery race explicitly deferred (documented, not silent). |
| `checkout-page.component.ts startPolling()` | `DestroyRef` | `takeUntilDestroyed(this.destroyRef)` | ✓ WIRED | Confirmed; WR-04 closed. |

### Behavioral Spot-Checks

| Behavior | Command | Result | Status |
|----------|---------|--------|--------|
| Orders saga unit tests (in-memory, no Docker) | `ECommerce.Orders.Tests.exe -class ECommerce.Orders.Tests.Unit.OrderStateMachineTests` (run directly by this verifier, not trusted from SUMMARY) | `Total: 10, Errors: 0, Failed: 0, Time: 32.994s` | ✓ PASS |
| Full Orders test suite (incl. Docker-dependent integration tests) | `ECommerce.Orders.Tests.exe` (full suite) | `Total: 18, Errors: 0, Failed: 8` — all 8 failures are `DotNet.Testcontainers.Builders.DockerUnavailableException` (`npipe://./pipe/docker_engine` unreachable) | ? SKIP (environment limitation, not a code defect — Docker Desktop unavailable in this sandbox, consistent across all three verification passes of this phase) |
| Angular checkout-page unit tests | `npx vitest --run src/app/features/checkout` (run directly by this verifier) | `Test Files 1 passed (1), Tests 6 passed (6)` | ✓ PASS |
| Claimed commits exist and match description | `git show --stat -1 8c75754 2ec58a3 338ff88 c995c26` | All 4 commits found, messages match `04-07-REVIEW-FIX.md`'s claims exactly | ✓ PASS |

### Probe Execution

No `scripts/*/tests/probe-*.sh` convention found and no plan/summary declares probe-based verification for this phase. Step 7c: SKIPPED (no declared or conventional probes).

### Requirements Coverage

| Requirement | Source Plan | Description | Status | Evidence |
|-------------|-------------|--------------|--------|----------|
| CHK-01 | 04-01, 04-03, 04-05, 04-06 | User can initiate checkout and receive a checkoutId (202) | ✓ SATISFIED | `POST /checkout` returns 202 + checkoutId |
| CHK-02 | 04-03, 04-05, 04-06 | User can poll checkout/order status via GET /checkout/{id} | ✓ SATISFIED | `GET /checkout/{id}` implemented and polled by Angular |
| CHK-03 | 04-02, 04-07 | Saga compensates on PaymentFailed — cancels the order | ✓ SATISFIED | `During(Pending, When(PaymentFailedEvent)...)`; unit test passes live |
| CHK-04 | 04-02, 04-05, 04-07 | Saga compensates on FulfillmentFailed — refunds payment and cancels order | ✓ SATISFIED | `During(Paid, When(FulfillmentFailedEvent)...)` publishes RefundPayment + Cancelled; CR-01 redelivery-after-Cancelled gap now closed |
| CHK-05 | 04-02, 04-07 | Saga times out after ~15 min if not completed (compensation triggered) | ✓ SATISFIED | Timeout mechanism proven via spike + short-timeout unit test; CR-01/CR-02 redelivery defects closed |
| PAY-01 | 04-01, 04-04 | Simulated payment service processes AuthorisePayment commands | ✓ SATISFIED | `AuthorisePaymentConsumer` |
| PAY-02 | 04-04 | Amounts ending in .99 deterministically trigger PaymentFailed | ✓ SATISFIED | `cents == 99m` rule, confirmed |
| PAY-03 | 04-04, 04-07 | Payment processing is idempotent by checkoutId | ✓ SATISFIED (sequential case) | `ProcessedPayment` PK + unique index, full outcome-switch redelivery handling. Concurrent (not sequential) redelivery race explicitly deferred — documented follow-up, not a PAY-03 blocker per the requirement's own wording ("idempotent by checkoutId", which the sequential/at-least-once redelivery case satisfies). |
| FE-03 | 04-06, 04-07 | User can complete checkout and see order status updating in real-time via polling | ✓ SATISFIED | mat-stepper, 1.5s polling with `takeUntilDestroyed`, auto-redirect all present and wired; Angular unit tests pass live |

No orphaned requirements: all 9 requirement IDs assigned to this phase (CHK-01 through CHK-05, PAY-01 through PAY-03, FE-03) are claimed by at least one plan's frontmatter.

**REQUIREMENTS.md staleness (carried forward from prior pass, still unresolved):** REQUIREMENTS.md's own checkbox list and traceability table still mark CHK-03, CHK-05, and FE-03 as unchecked/"Pending" as of this verification, despite all 9 phase-4 requirements now being satisfied in code. This is a documentation-sync gap in REQUIREMENTS.md itself, not a code gap — flagging again for the orchestrator to update REQUIREMENTS.md's status markers now that this phase's blocker is closed.

### Anti-Patterns Found

| File | Line | Pattern | Severity | Impact |
|------|------|---------|----------|--------|
| `src/services/orders/ECommerce.Orders.API/Features/Orders/OrderStateMachine.cs` | 100-168 | `During(Pending, ...)` has no binding for `FulfillmentFailedEvent` | ⚠️ WARNING | New finding (this pass) — same defect class as the now-closed CR-01/CR-02, but currently unreachable via any exercised code path. See "New Finding" section above for full analysis and suggested one-line fix. |
| `src/services/payments/ECommerce.Payments.API/Features/Payments/RefundPaymentConsumer.cs` / `AuthorisePaymentConsumer.cs` | 16-24 / 18-52 | Idempotency guard has no optimistic-concurrency protection against truly concurrent (not sequential) redeliveries | ⚠️ WARNING | WR-03 residual (04-07-REVIEW.md) — explicitly deferred, documented in `04-07-REVIEW-FIX.md` as a recommended follow-up before Phase 6 hardening. Not silently dropped. |

No `TBD`/`FIXME`/`XXX` debt markers found in any file touched by 04-07 or 04-07-REVIEW-FIX.

### Human Verification Required

See `human_verification` in frontmatter — three items, all centering on live/Docker-based confirmation against a real Azure Service Bus broker (as opposed to the in-memory MassTransit test harness, which has now proven the saga logic itself is correct). This sandboxed environment has no Docker/npipe access in any of the three verification passes conducted for this phase.

### Gaps Summary

The BLOCKER identified in the original `04-VERIFICATION.md` (CR-01: `During(Cancelled, ...)` faulting on late/redelivered events) is closed, and independently re-verified against the current file contents (not the narrative documents) plus a live execution of all 10 `OrderStateMachineTests` (0 failures) and all 6 Angular checkout-page tests (0 failures). The second-order gaps found by the scoped `04-07-REVIEW.md` (CR-01 extended: `Fulfilled`/`Failed` had zero bindings at all; CR-02: `OrderCreatedEvent` unbound everywhere) are also closed and independently confirmed, with matching commits (`8c75754`, `2ec58a3`, `338ff88`, `c995c26`) verified to exist and match their claimed descriptions.

Performing the same systematic per-event-type audit this verification was tasked with across all five `During()` blocks turned up one additional, narrower instance of the same defect class that neither `04-REVIEW.md` nor `04-07-REVIEW.md` identified: `During(Pending, ...)` has no binding for `FulfillmentFailedEvent`. Careful reachability analysis shows this cannot currently be triggered by any code path in this repository (the only publisher of `FulfillmentFailed` is gated by a read-model check that structurally cannot pass before the saga has already left `Pending`), so it is classified as a WARNING rather than a BLOCKER and does not gate this phase's completion — but it is flagged explicitly so it is not rediscovered a third time when Phase 5's Fulfillment service introduces new `FulfillmentFailed` publishers.

All 5 roadmap success criteria (SC1-SC5) are verified against current code and passing tests. All 9 phase requirements (CHK-01..05, PAY-01..03, FE-03) are satisfied. The one explicitly-deferred item (WR-03's concurrent-redelivery race protection) is documented as a known follow-up, not silently dropped, and does not block the phase per the requirement's literal wording. Status is `human_needed` rather than `passed` solely because this sandboxed environment cannot exercise the live Docker/ASB stack — the three human-verification items are the same category of live-broker confirmation that could not be completed in any of this phase's three verification passes.

---

_Verified: 2026-08-13T23:15:00Z_
_Verifier: Claude (gsd-verifier)_
