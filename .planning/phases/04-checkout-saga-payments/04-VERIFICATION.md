---
phase: 04-checkout-saga-payments
verified: 2026-08-12T00:00:00Z
status: gaps_found
score: 4/5 must-haves verified
overrides_applied: 0
gaps:
  - truth: "Fulfillment-failure and timeout compensation paths leave the saga in a consistent terminal state (SC4/SC5) — no unhandled faults on late/redelivered events"
    status: failed
    reason: >
      OrderStateMachine.cs's `During(Cancelled, ...)` only absorbs a redelivered
      OrderStatusChangedEvent. It does NOT absorb PaymentAuthorisedEvent,
      PaymentFailedEvent, or FulfillmentFailedEvent — all three of which can
      legitimately arrive after the saga has already transitioned to Cancelled
      (broker redelivery of an idempotent Payments outcome, a CHK-05 timeout that
      fires before an in-flight AuthorisePayment resolves, or a double-clicked
      demo fulfillment-failure trigger racing the read-model's eventual
      consistency). When any of these three events arrives while the saga is in
      Cancelled, MassTransit throws UnhandledEventException, faulting the
      consumer and routing the message to the retry/dead-letter path instead of
      leaving the saga in the "consistent terminal state" SC4/SC5 explicitly
      require. `During(Paid, ...)` already defends against the equivalent race
      (see the "Pitfall 2" catch-all at lines 195-203) — `During(Cancelled, ...)`
      was left with only a partial catch-all. Reproduction path confirmed by
      direct code read (OrderStateMachine.cs:215-216) and independently
      identified by 04-REVIEW.md (CR-01, critical severity). No unit test exists
      for this scenario — the existing 6 saga unit tests
      (OrderStateMachineTests.cs) cover only the direct/happy paths for
      CHK-03/CHK-04/CHK-05, not post-Cancelled redelivery.
    artifacts:
      - path: "src/services/orders/ECommerce.Orders.API/Features/Orders/OrderStateMachine.cs"
        issue: "During(Cancelled, ...) at lines 215-216 lacks When(PaymentAuthorisedEvent), When(PaymentFailedEvent), When(FulfillmentFailedEvent) — throws UnhandledEventException on redelivery/race"
    missing:
      - "Extend During(Cancelled, ...) to also absorb CheckoutTimeout.Received, PaymentAuthorisedEvent, PaymentFailedEvent, and FulfillmentFailedEvent (mirroring the During(Paid, ...) catch-all), per 04-REVIEW.md CR-01's suggested fix"
      - "A regression unit test that publishes PaymentAuthorisedEvent (or PaymentFailedEvent / FulfillmentFailedEvent) to a saga instance already in Cancelled and asserts no exception is thrown and state remains Cancelled"
human_verification:
  - test: "Run the full Docker Compose / Aspire stack and click through Cart -> /checkout -> Place Order -> watch the mat-stepper update in real time -> auto-redirect to /orders/:id, for both a normal-priced cart (happy path) and a .99-ending cart (PaymentFailed demo trigger)"
    expected: "Stepper advances Started -> AwaitingPayment -> Paid (or -> Cancelled with a visible failure reason), page auto-navigates to /orders/:id with no manual refresh, matching SC1/SC2/SC3"
    why_human: "Sandboxed environment has no Docker/npipe access — Testcontainers-based integration tests and the live Aspire/ASB stack could not be exercised end-to-end in this verification pass. Only build success, in-memory saga unit tests, and static code wiring were verifiable."
  - test: "Click 'simulate fulfillment failure' on a Paid order via the checkout/order-detail UI, twice in rapid succession"
    expected: "Second click either no-ops cleanly or is rejected — no dead-lettered message, no crashed consumer"
    why_human: "Confirms/refutes the CR-01 double-click reproduction path (CheckoutEndpoints.cs's Paid-status check races the read model) against a live running system; cannot be proven by static analysis alone, only that the code path exists as described."
  - test: "Leave a checkout un-actioned past the configured production timeout (or temporarily lower Checkout:TimeoutMinutes) and observe whether any in-flight AuthorisePayment response arrives after the timeout has already cancelled the order"
    expected: "No UnhandledEventException / faulted consumer / dead-lettered message in Orders.API logs"
    why_human: "This is the primary real-world trigger for the CR-01 gap (Pending -> timeout -> Cancelled, then a late PaymentAuthorised/PaymentFailed arrives) and requires live timing-dependent observation."
---

# Phase 4: Checkout Saga & Payments Verification Report

**Phase Goal:** A user clicks "Place Order" and the checkout saga orchestrates Order creation, simulated payment, and compensation paths end-to-end — the headline demo.
**Verified:** 2026-08-12T00:00:00Z
**Status:** gaps_found
**Re-verification:** No — initial verification

**Note on phase mode:** ROADMAP.md marks this phase `Mode: mvp`, but the phase goal text ("A user clicks..." — not "As a [role], I want to [capability], so that [outcome].") fails `user-story.validate` (`valid: false`). Per the MVP-mode protocol this would normally require refusing verification and asking for `/gsd mvp-phase 04`. Since the launching orchestrator invoked this verification as a standard goal-backward pass (not MVP-narrowed) and full goal-backward verification is strictly more thorough than the MVP-narrowed variant, this report proceeds with standard verification rather than refusing outright. Flagging the ROADMAP mode/goal-format mismatch for the user's attention — the ROADMAP.md entry should either be updated with a proper User Story goal or have `Mode: mvp` removed.

## Goal Achievement

### Observable Truths

| # | Truth | Status | Evidence |
|---|-------|--------|----------|
| 1 | SC1 — User can POST `/checkout` (202 + checkoutId) then poll `GET /checkout/{id}`, reflected live in Angular `/checkout` and `/orders/:id` | ✓ VERIFIED | `CheckoutEndpoints.cs` implements both routes; `checkout-page.component.ts` polls every 1500ms via `interval(1500).pipe(switchMap(...))` and navigates to `/orders/:id` on terminal state; `order-detail.component.ts` renders status + failure reason; routes registered in `app.routes.ts`. (See WARNING below — polling subscription is never torn down on navigate-away, a robustness gap, not a functional failure of the happy path.) |
| 2 | SC2 — Happy path drives `Started -> AwaitingPayment -> Paid`, idempotent payment processing keyed by `checkoutId` | ✓ VERIFIED | `OrderStateMachine.Initially()` publishes `AuthorisePayment` immediately on `OrderCreated` and transitions `Pending`; `PaymentAuthorisedEvent` transitions `Pending -> Paid`. `AuthorisePaymentConsumer` looks up `ProcessedPayment` by `CheckoutId` (EF `HasKey(p => p.CheckoutId)` + unique index) before ever inserting — redelivery replays the stored outcome instead of reprocessing (PAY-03). Confirmed by direct code read of `PaymentsDbContext.cs` and `AuthorisePaymentConsumer.cs`. |
| 3 | SC3 — `.99`-ending cart deterministically triggers `PaymentFailed`; saga cancels the order | ✓ VERIFIED | `AuthorisePaymentConsumer.cs`: `cents == 99m` deterministic rule; `OrderStateMachine`'s `During(Pending, When(PaymentFailedEvent)...)` sets `FailureReason` and transitions to `Cancelled`. Unit test `PaymentFailed_WhenPending_TransitionsToCancelledWithFailureReason` executed live in this verification pass (in-memory, no Docker required) — **passed**. |
| 4 | SC4/SC5 — Fulfillment-failure and 15-minute-timeout compensation both cascade `RefundPayment`/cancel and leave the system in a **consistent terminal state** | ✗ FAILED | `During(Cancelled, ...)` (`OrderStateMachine.cs:215-216`) only absorbs `OrderStatusChangedEvent` — a redelivered `PaymentAuthorisedEvent`, `PaymentFailedEvent`, or `FulfillmentFailedEvent` arriving after the saga reaches `Cancelled` throws `UnhandledEventException`, faulting the consumer instead of remaining terminal/consistent. Confirmed by direct code read; independently identified as CR-01 (critical) in `04-REVIEW.md`. The happy-path compensation flow itself (single `FulfillmentFailed` -> `RefundPayment` + `Cancelled`, and single timeout -> `Cancelled`) IS proven correct by passing unit tests and a live ASB-emulator spike (`SPIKE-RESULT: PASS`, `spikes/04-asb-scheduling-spike/Program.cs`, observed 2026-08-08) — the gap is specifically the redelivery/race edge case both SC4 and SC5's "consistent terminal state" language commits to. |
| 5 | The Azure Service Bus emulator's scheduled-message delivery (CHK-05's mechanism) is proven, not assumed, before being relied on | ✓ VERIFIED | Standalone spike (`spikes/04-asb-scheduling-spike/Program.cs`) directly exercises `ServiceBusClient`/`ScheduledEnqueueTime` against the real emulator image, independent of the saga/HTTP stack. Documented result in `04-RESEARCH.md`: `SPIKE-RESULT: PASS — message was delivered on or after the scheduled time`. This satisfies plan 04-02's own must-have about resolving Open Question 1 with an observed outcome. |

**Score:** 4/5 truths verified (1 FAILED — see gap above; classified BLOCKER per adversarial stance since it is a correctness defect directly in the compensation path that is this phase's headline demo mechanic)

### Required Artifacts

| Artifact | Expected | Status | Details |
|----------|----------|--------|---------|
| `src/building-blocks/Contracts/Checkout/Commands/V1/StartCheckout.cs` | Internal HTTP contract Checkout.API -> Orders.API | ✓ VERIFIED | Exists, used by `OrdersClient`/`OrdersEndpoints` |
| `src/building-blocks/Contracts/Payments/Commands/V1/AuthorisePayment.cs` | Saga -> Payments command | ✓ VERIFIED | Published from `OrderStateMachine.Initially()`, consumed by `AuthorisePaymentConsumer` |
| `src/building-blocks/Contracts/Payments/Events/V1/PaymentFailed.cs` | Payments -> saga event (CHK-03) | ✓ VERIFIED | Published by `AuthorisePaymentConsumer`, consumed by `OrderStateMachine` |
| `src/building-blocks/Contracts/Fulfillment/Events/V1/FulfillmentFailed.cs` | Demo trigger -> saga event (CHK-04) | ✓ VERIFIED | Published by `CheckoutEndpoints`'s demo trigger, consumed by `OrderStateMachine` |
| `src/services/orders/ECommerce.Orders.API/Features/Orders/OrderStateMachine.cs` | Extended saga: typed events, Schedule/Unschedule timeout, Paid->Cancelled compensation | ⚠️ VERIFIED-WITH-DEFECT | Exists, substantive, wired, happy paths pass unit tests — but `During(Cancelled, ...)` is an incomplete catch-all (see gap #1) |
| `src/services/orders/ECommerce.Orders.API/Features/Orders/CheckoutOptions.cs` | Configurable `Checkout:TimeoutMinutes` | ✓ VERIFIED | Consumed by `OrderStateMachine` constructor via `IOptions<CheckoutOptions>`; test override to 0.05 min proven by `CheckoutTimeoutExpired_WhenPaymentOutcomeNeverArrives_TransitionsToCancelledWithFailureReason` (passed live) |
| `docs/adr/0009-checkout-saga-state-reconciliation.md` | MADR record of "Started" synthesis decision | ✓ VERIFIED | File exists (listed in 04-REVIEW.md's files_reviewed) |
| `spikes/04-asb-scheduling-spike/Program.cs` | Standalone ASB scheduling proof | ✓ VERIFIED | Exists, runnable, documented PASS result |
| `src/services/orders/ECommerce.Orders.API/Features/Orders/OrdersEndpoints.cs` | `POST /orders/checkout` (replaces test-create-from-cart), `GET /orders/{id}` + FailureReason | ✓ VERIFIED | `test-create-from-cart` fully removed (`grep -c` returns 0 in repo); `POST /orders/checkout` present; `OrderDto.FailureReason` present and populated by `OrderReadModelProjector` |
| `src/services/payments/ECommerce.Payments.API/Features/Payments/ProcessedPayment.cs` | Idempotency-key entity, unique index on CheckoutId | ✓ VERIFIED | `HasKey(p => p.CheckoutId)` + `HasIndex(...).IsUnique()` in `PaymentsDbContext.OnModelCreating` |
| `src/services/payments/ECommerce.Payments.API/Features/Payments/AuthorisePaymentConsumer.cs` | PAY-01/02/03 processing logic | ⚠️ VERIFIED-WITH-WARNING | Core PAY-01/02/03 logic correct and idempotent (never double-processes); WR-02 (04-REVIEW.md) — replay after a "Refunded" outcome mislabels the event as `PaymentFailed` with a null `Reason` smuggled into a non-nullable field. Edge case, not on the primary demo path; not a truth-blocking defect but a real bug. |
| `src/services/payments/ECommerce.Payments.API/Features/Payments/RefundPaymentConsumer.cs` | Idempotent refund processing | ⚠️ VERIFIED-WITH-WARNING | WR-03 (04-REVIEW.md) — will refund a `Failed` (never-authorised) payment if reached; unreachable through the current saga's normal flow (RefundPayment only published from Paid, which requires prior authorisation), so low practical risk but no defense-in-depth guard |
| `src/services/checkout/ECommerce.Checkout.API/Features/Checkout/CheckoutEndpoints.cs` | `POST /checkout`, `GET /checkout/{id}`, demo fulfillment-failure trigger | ✓ VERIFIED | All three routes present, ownership-checked, `RequireAuthorization()` |
| `src/frontend/ecommerce-app/src/app/features/checkout/checkout-page/checkout-page.component.ts` | mat-stepper, ~1.5s polling, demo toggle, auto-redirect | ⚠️ VERIFIED-WITH-WARNING | All functional behavior present and wired; WR-04 (04-REVIEW.md) — polling subscription has no `ngOnDestroy`/`takeUntilDestroyed()` cleanup and the error-recovery button re-fetches the cart rather than resuming polling. Confirmed by direct code read (no `OnDestroy` implemented in the class). |
| `src/frontend/ecommerce-app/src/app/features/orders/order-detail/order-detail.component.ts` | Route param -> `GET /api/orders/{id}` -> status + failureReason | ✓ VERIFIED | Confirmed by direct code read; `showFailureReason` computed signal correctly gates on `FAILURE_STATUSES` |
| `src/frontend/ecommerce-app/src/app/app.routes.ts` | `/checkout` and `/orders/:id` routes | ✓ VERIFIED | Both routes present |

### Key Link Verification

| From | To | Via | Status | Details |
|------|-----|-----|--------|---------|
| `OrderStateMachine.Initially()` | `AuthorisePayment` | `Publish(ctx => new AuthorisePayment(...))` | ✓ WIRED | Confirmed at line 77 |
| `OrderStateMachine During(Pending)` | `CheckoutTimeout` schedule | `Schedule(CheckoutTimeout, ...)`/`.Unschedule(CheckoutTimeout)` | ✓ WIRED | Confirmed; Unschedule called on both `PaymentAuthorisedEvent` and `PaymentFailedEvent` |
| `spikes/04-asb-scheduling-spike/Program.cs` | RESEARCH.md Open Question 1 | Observed PASS/FAIL recorded back into RESEARCH.md | ✓ WIRED | `SPIKE-RESULT: PASS` recorded with observation date/method in `04-RESEARCH.md` |
| `POST /orders/checkout` | `OrderCreated` | `IPublishEndpoint.Publish` + `SaveChangesAsync` (outbox) | ✓ WIRED | Confirmed |
| `GET /orders/{id}` | `OrderDto.FailureReason` | `OrderMapper.ToDto` | ✓ WIRED | Confirmed |
| `AuthorisePaymentConsumer` | `ProcessedPayment` | Unique index on CheckoutId, look-up-before-decide | ✓ WIRED | Confirmed |
| `AuthorisePaymentConsumer` | `PaymentAuthorised`/`PaymentFailed` | `IPublishEndpoint.Publish` before `SaveChangesAsync` (outbox) | ✓ WIRED | Confirmed |
| `POST /checkout` | `POST /orders/checkout` | `IOrdersClient.StartCheckoutAsync` (sync HTTP, bearer forwarded) | ✓ WIRED | Confirmed |
| `GET /checkout/{id}` | `GET /orders/{id}` | `IOrdersClient.GetStatusAsync` — 404 mapped to synthetic "Started" | ✓ WIRED | Confirmed |
| `POST /checkout/{id}/simulate-fulfillment-failure` | `FulfillmentFailed` | `IPublishEndpoint.Publish` | ✓ WIRED | Confirmed |
| `cart-page.component.html` | `/checkout` | `routerLink="/checkout"` | ✓ WIRED | Confirmed, enabled button |
| `checkout-page.component.ts` | `GET /api/checkout/{id}` | `interval(1500).pipe(switchMap(...))` | ✓ WIRED | Confirmed |
| `checkout-page.component.ts` terminal state | `/orders/:id` | `router.navigate(['/orders', id])` | ✓ WIRED | Confirmed |
| `During(Cancelled, ...)` | late `PaymentAuthorisedEvent`/`PaymentFailedEvent`/`FulfillmentFailedEvent` | (none — missing) | ✗ NOT_WIRED | This is the CR-01 gap: the link the "Paid" state already has (see Pitfall 2 catch-all) does not exist for "Cancelled" |

### Behavioral Spot-Checks

| Behavior | Command | Result | Status |
|----------|---------|--------|--------|
| Orders solution builds cleanly | `dotnet build src/services/orders/Orders.sln` | 0 errors, 67 warnings (pre-existing NuGet advisories + Mapperly unmapped-member warnings, unrelated to this phase) | ✓ PASS |
| Saga unit tests (in-memory, no Docker) | `ECommerce.Orders.Tests.exe -class ECommerce.Orders.Tests.Unit.OrderStateMachineTests` | `Total: 6, Errors: 0, Failed: 0` | ✓ PASS |
| Integration tests requiring Testcontainers/Postgres | `ECommerce.Orders.Tests.exe` (full suite) | `Total: 14, Failed: 8` — all 8 failures are `DockerUnavailableException` (`npipe://./pipe/docker_engine` unreachable in this sandbox) | ? SKIP (environment limitation, not a code defect — consistent with the note provided for this verification pass) |
| Angular unit/component tests | Not re-run in this pass | Per task brief, already run live during execution: 16/16 passed, `tsc --noEmit` clean | ? SKIP (accepted on the executor's reported live run; independently confirmed by direct code read of the components it covers) |

### Probe Execution

No `scripts/*/tests/probe-*.sh` convention found and no plan/summary declares probe-based verification for this phase. Step 7c: SKIPPED (no declared or conventional probes).

### Requirements Coverage

| Requirement | Source Plan | Description | Status | Evidence |
|-------------|-------------|--------------|--------|----------|
| CHK-01 | 04-01, 04-03, 04-05, 04-06 | User can initiate checkout and receive a checkoutId (202) | ✓ SATISFIED | `POST /checkout` returns 202 + checkoutId |
| CHK-02 | 04-03, 04-05, 04-06 | User can poll checkout/order status via GET /checkout/{id} | ✓ SATISFIED | `GET /checkout/{id}` implemented and polled by Angular |
| CHK-03 | 04-02 | Saga compensates on PaymentFailed — cancels the order | ✓ SATISFIED (happy path) | `During(Pending, When(PaymentFailedEvent)...)`, passing unit test |
| CHK-04 | 04-02, 04-05 | Saga compensates on FulfillmentFailed — refunds payment and cancels order | ⚠️ SATISFIED WITH DEFECT | Happy path works and is tested; redelivery-after-Cancelled path (CR-01) throws `UnhandledEventException` — see gap #1 |
| CHK-05 | 04-02 | Saga times out after ~15 min if not completed (compensation triggered) | ⚠️ SATISFIED WITH DEFECT | Timeout mechanism proven via spike + short-timeout unit test; same CR-01 redelivery defect applies to this path too (a late payment outcome after a timeout-triggered Cancelled) |
| PAY-01 | 04-01, 04-04 | Simulated payment service processes AuthorisePayment commands | ✓ SATISFIED | `AuthorisePaymentConsumer` |
| PAY-02 | 04-04 | Amounts ending in .99 deterministically trigger PaymentFailed | ✓ SATISFIED | `cents == 99m` rule, confirmed |
| PAY-03 | 04-04 | Payment processing is idempotent by checkoutId | ✓ SATISFIED (core case) | `ProcessedPayment` PK + unique index on `CheckoutId`, look-up-before-decide; WR-02's mislabeling edge case (post-refund replay) does not cause double-processing, only a mislabeled event on redelivery — documented as a warning, not a PAY-03 blocker |
| FE-03 | 04-06 | User can complete checkout and see order status updating in real-time via polling | ✓ SATISFIED (functionally) | mat-stepper, 1.5s polling, auto-redirect all present and wired; WR-04's subscription-leak is a robustness gap on navigate-away, not a failure of the core real-time-update behavior |

No orphaned requirements: all 9 requirement IDs assigned to this phase (CHK-01 through CHK-05, PAY-01 through PAY-03, FE-03) are claimed by at least one plan's frontmatter and independently confirmed against REQUIREMENTS.md's Checkout & Saga / Payments / Angular Frontend sections.

**Note on REQUIREMENTS.md staleness:** REQUIREMENTS.md's own checkbox list and traceability table still mark CHK-03, CHK-05, and FE-03 as unchecked/"Pending" (not yet updated to reflect Phase 4 completion) while CHK-01, CHK-02, CHK-04, PAY-01/02/03 are marked complete. This is a documentation-sync gap in REQUIREMENTS.md itself (likely not yet updated post-phase), not a code gap — flagging for the orchestrator to update REQUIREMENTS.md's status markers once this phase's gaps are closed.

### Anti-Patterns Found

| File | Line | Pattern | Severity | Impact |
|------|------|---------|----------|--------|
| `src/services/orders/ECommerce.Orders.API/Features/Orders/OrderStateMachine.cs` | 215-216 | Incomplete catch-all in `During(Cancelled, ...)` | 🛑 BLOCKER | CR-01 — `UnhandledEventException` on redelivered payment/fulfillment events after Cancelled (see gap #1) |
| `src/services/orders/ECommerce.Orders.API/Features/Orders/OrderStateMachine.cs` | 172-173 | `FulfillmentFailedEvent` handler discards `ctx.Message.Reason`, hardcodes a generic string | ⚠️ WARNING | WR-01 — future real Fulfillment failure reasons silently lost |
| `src/services/payments/ECommerce.Payments.API/Features/Payments/AuthorisePaymentConsumer.cs` | 19-36 | Binary `Authorised`/else branch on redelivery, ignores `"Refunded"` outcome | ⚠️ WARNING | WR-02 — mislabels a refunded payment as `PaymentFailed` with a smuggled-null `Reason` on redelivery |
| `src/services/payments/ECommerce.Payments.API/Features/Payments/RefundPaymentConsumer.cs` | 16-21 | Idempotency guard allows refunding a `Failed` (never-authorised) payment | ⚠️ WARNING | WR-03 — no defense-in-depth guard against refunding money never taken; unreachable via current saga flow |
| `src/frontend/ecommerce-app/src/app/features/checkout/checkout-page/checkout-page.component.ts` | 103-126 | Polling subscription never torn down; error handler has no path back to resumed polling | ⚠️ WARNING | WR-04 — duplicate concurrent polling loops possible on navigate-away-and-back; users stuck on unrelated error screen after a transient network blip |
| `src/services/checkout/ECommerce.Checkout.API/Features/Checkout/CheckoutEndpoints.cs` | 80-83 | Dead code — `GetUserId` defined, never called | ℹ️ INFO | IN-01 — no functional impact |
| `src/frontend/ecommerce-app/src/app/features/cart/cart-page/cart-page.component.scss` | 1-73 | Hardcoded pixel spacing instead of `var(--space-*)` tokens | ℹ️ INFO | IN-02 — cosmetic/consistency only |
| `spikes/04-asb-scheduling-spike/docker-compose.yml` | 6 | Hardcoded plaintext DB password | ℹ️ INFO | IN-03 — spike-only, local-only, low risk |

No `TBD`/`FIXME`/`XXX` debt markers found in any file modified by this phase.

### Human Verification Required

See `human_verification` in frontmatter — three items, all centering on live/Docker-based confirmation of (a) the full happy-path demo flow end-to-end and (b) the two concrete reproduction paths for the CR-01 critical gap (double-click on the demo trigger; a payment outcome arriving after a timeout-triggered cancellation).

### Gaps Summary

The phase's happy-path saga orchestration (order creation -> payment authorisation -> Paid) and both single-shot compensation paths (explicit PaymentFailed, explicit FulfillmentFailed, and a single un-raced timeout) are real, wired, and proven by passing unit tests plus a live ASB-emulator spike — this is not a stub or placeholder phase. However, the saga's `Cancelled` terminal state has an incomplete event catch-all: any of the three newly-introduced typed events (`PaymentAuthorisedEvent`, `PaymentFailedEvent`, `FulfillmentFailedEvent`) arriving after the saga has already reached `Cancelled` throws `UnhandledEventException`, faulting the MassTransit consumer. This is directly reachable through ordinary demo/production conditions this phase itself introduces (a timeout racing an in-flight payment response; a double-clicked fulfillment-failure demo button racing the eventually-consistent read model) — not a contrived edge case. Because compensation-path robustness is explicitly named in both SC4 ("leaving the system in a consistent terminal state") and SC5 (identical language) of this phase's own success criteria, and because compensation paths are half of this phase's headline demo goal, this is classified as a BLOCKER, matching 04-REVIEW.md's own CR-01 critical classification. The fix is small and precisely scoped (extend `During(Cancelled, ...)` to mirror `During(Paid, ...)`'s existing catch-all, per 04-REVIEW.md's suggested fix) and does not require replanning the phase's architecture.

The four review warnings (FulfillmentFailed.Reason discarded, AuthorisePaymentConsumer post-refund mislabeling, RefundPaymentConsumer missing defense-in-depth, checkout-page polling subscription leak) do not block the headline demo path and are documented above as WARNING-severity anti-patterns for the closure plan to address alongside CR-01, but are not independently gating this verification's status.

---

_Verified: 2026-08-12T00:00:00Z_
_Verifier: Claude (gsd-verifier)_
