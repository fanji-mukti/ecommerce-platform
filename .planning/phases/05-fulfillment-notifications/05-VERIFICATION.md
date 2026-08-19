---
phase: 05-fulfillment-notifications
verified: 2026-08-19T00:00:00Z
status: gaps_found
score: 3/4 must-haves verified
overrides_applied: 0
gaps:
  - truth: "After OrderShipped, the saga reaches Completed and the order detail page reflects Fulfilled status without manual intervention (Success Criterion 2)"
    status: partial
    reason: >
      The Paid->Fulfilled saga transition and read-model projection work correctly for the
      normal happy path (confirmed by static trace: OrderPaidConsumer.SchedulePublish ->
      OrderShippedEvent -> OrderStateMachine.TransitionTo(Fulfilled) ->
      OrderReadModelProjector -> order-detail polling). However, the code review
      (05-REVIEW.md CR-01, independently reproduced during this verification) found a genuine
      data-correctness gap: OrderPaidConsumer schedules a delayed OrderShipped publish the
      moment an order becomes Paid, and nothing cancels or supersedes that scheduled message
      if the order is subsequently cancelled via Checkout.API's existing
      "simulate-fulfillment-failure" demo endpoint (reachable while the order is Paid).
      OrderStateMachine correctly absorbs the late OrderShipped in During(Cancelled, ...)
      (Ignore(OrderShippedEvent)) so the saga itself does not transition back to Fulfilled or
      fault — but OrderShippedNotificationConsumer has no equivalent guard and unconditionally
      inserts "Your order has shipped." into the customer's notification feed for an order
      that was actually cancelled and refunded. "Without manual intervention" therefore holds
      for the saga/order-detail state itself, but the phase goal's parallel promise — "the user
      sees the entire lifecycle reflected in an in-app notification inbox" — is violated for
      this reachable path: the inbox shows a factually wrong shipped notification alongside
      (or instead of) the correct cancellation notification.
    artifacts:
      - path: "src/services/fulfillment/ECommerce.Fulfillment.API/Features/Fulfillment/OrderPaidConsumer.cs"
        issue: "SchedulePublish(delay, OrderShipped) at lines 24-31 has no corresponding CancelScheduledPublish and no status re-check immediately before the delayed publish fires"
      - path: "src/services/notifications/ECommerce.Notifications.API/Consumers/OrderShippedNotificationConsumer.cs"
        issue: "Unconditionally inserts 'Your order has shipped.' (lines 20-28) for any OrderShipped delivery, with no check of the order's current/actual status before writing a customer-facing notification"
    missing:
      - "Either: capture the ScheduledMessage<OrderShipped> token in OrderPaidConsumer and unschedule it when OrderStateMachine's FulfillmentFailedEvent handler fires (requires a new command/event path from Orders back to Fulfillment, or a saga-owned cancellation signal)"
      - "Or: have Fulfillment re-check the order's current status immediately before the scheduled OrderShipped actually publishes, skipping publish if no longer Paid"
      - "Or (simpler, notification-layer-only fix): have OrderShippedNotificationConsumer (or OrderStateMachine's OrderShippedEvent handling) suppress/skip the 'shipped' notification when the order is not in a state consistent with having shipped"
deferred: []
human_verification:
  - test: "Reproduce CR-01 end-to-end: place an order, let it reach Paid, then within the ~45s ProcessingSeconds window call POST /checkout/{id}/simulate-fulfillment-failure, then wait for the scheduled OrderShipped to fire and check GET /notifications"
    expected: "No 'Your order has shipped.' notification should appear for a cancelled/refunded order — only the cancellation/refund notification should be visible, and the order-detail page should show Cancelled consistently"
    why_human: "Requires running the full stack (ASB emulator or in-memory scheduler, live timers) to observe the actual race between the demo cancellation endpoint and the scheduled delayed publish — not verifiable by static code inspection alone once past confirming the missing guard exists"
  - test: "Visually confirm the /orders/:id 'Preparing your shipment…' spinner and the /notifications inbox list render correctly in a browser (icons, empty/loading/error states, routerLink navigation to /orders/:id)"
    expected: "Spinner shows only while status is Paid; notifications list shows icons per event type, links to the correct order, and empty/loading/error states render per UI-SPEC"
    why_human: "Visual rendering and Angular Material styling cannot be confirmed via static code reading alone"
---

# Phase 05: Fulfillment & Notifications Verification Report

**Phase Goal:** The saga reaches a fully-shipped terminal state and the user sees the entire lifecycle reflected in an in-app notification inbox.
**Verified:** 2026-08-19T00:00:00Z
**Status:** gaps_found
**Re-verification:** No — initial verification

## Goal Achievement

### Observable Truths

| # | Truth (Success Criterion) | Status | Evidence |
|---|---------|------------|----------|
| 1 | Fulfillment service consumes OrderPaid events, advances status through a timer-based simulation, and publishes OrderShipped — visible in the saga state and the order detail view. | VERIFIED | `OrderPaidConsumer.cs` filters `OrderStatusChanged.NewStatus == "Paid"`, schedules a delayed `OrderShipped` publish via `context.SchedulePublish` (configurable `FulfillmentOptions.ProcessingSeconds`, no polling table/BackgroundService). `OrderStateMachine.During(Paid, ...)` binds `Event<OrderShipped>` and publishes `OrderStatusChanged(NewStatus="Fulfilled")`. `OrderReadModelProjector.Consume(OrderStatusChanged)` generically sets `readModel.Status = msg.NewStatus`, so "Fulfilled" flows to the read model with no hardcoded status list. |
| 2 | After OrderShipped, the saga reaches Completed and the order detail page reflects Fulfilled status without manual intervention. | PARTIAL — see gap below | Happy-path wiring confirmed: `order-detail.component.ts` polls `GET /api/orders/{id}` every 1.5s via `interval(1500).pipe(switchMap(...), takeWhile(...))` until a terminal status (`Fulfilled`/`Cancelled`/`Failed`), matching plan 05-02's must-have. **However**, CR-01 (05-REVIEW.md, independently reproduced by static trace during this verification) shows a reachable path where a scheduled `OrderShipped` is never cancelled when the order is compensated/cancelled mid-flight, producing an incorrect state signal downstream in Notifications (see gap). The saga's own state machine correctly ignores the late event (`During(Cancelled, ...): Ignore(OrderShippedEvent)`), so the *order-detail* status itself stays correctly Cancelled — this truth's saga/order-detail half holds, but see Truth 4 for the inbox-side violation of the same underlying defect. |
| 3 | User can GET /notifications to view an in-app inbox containing entries for the saga lifecycle events they participated in (OrderPaid, OrderShipped, PaymentFailed). | VERIFIED | `NotificationsEndpoints.cs`: `GET /notifications`, `[Authorize]`, scoped via `.Where(n => n.UserId == userId)`, `OrderByDescending(n => n.OccurredAt)`. Three consumers (`OrderPaidNotificationConsumer`, `OrderShippedNotificationConsumer`, `PaymentFailedNotificationConsumer`) each insert a `NotificationEntry` with `UserId`, `OrderId`, human-readable `Message`, `EventType`, `OccurredAt`. Frontend `/notifications` route wired via `app.routes.ts` and nav link in `app.html` (`routerLink="/notifications"`); `NotificationsPageComponent` renders list, empty state, loading state (mat-progress-bar), and error state with retry — not stubs. |
| 4 | Notifications service idempotently consumes saga events from the producing-context topics and persists inbox entries, verified by a forced-redelivery test producing no duplicate inbox rows. | PARTIAL — see gap below | Idempotency is real at the infrastructure level: `Program.cs` wires `AddEntityFrameworkOutbox<NotificationsDbContext>` + `cfg.UseEntityFrameworkOutbox<NotificationsDbContext>(context)` for all three consumers' receive endpoints — MassTransit's EF Core inbox (InboxState) will deduplicate all three in production regardless of test coverage. But the *proof* required by this success criterion ("verified by a forced-redelivery test producing no duplicate inbox rows") only exists for one of the three consumers: `OrderPaidNotificationInboxDeduplicationTests.cs` asserts exactly one `InboxState` row and exactly one `NotificationEntry` row for a duplicate `MessageId`. `OrderShippedNotificationConsumerSteps.cs` and `PaymentFailedNotificationConsumerSteps.cs` build their harness with `AddMassTransitTestHarness` + in-memory DB and **no** `AddEntityFrameworkOutbox` — they test message-to-row mapping only, not redelivery dedup (matches 05-REVIEW.md WR-02 exactly, confirmed by direct file read). |

**Score:** 2/4 truths fully VERIFIED, 2/4 PARTIAL (same root cause: CR-01's uncancelled scheduled `OrderShipped` for Truth 2, and WR-02's asymmetric test coverage for Truth 4 — treated here as one blocking gap plus one coverage gap).

### Required Artifacts

| Artifact | Expected | Status | Details |
|----------|----------|--------|---------|
| `src/building-blocks/Contracts/Fulfillment/Events/V1/OrderShipped.cs` | New Fulfillment event carrying UserId/CheckoutId/ShippedAt | VERIFIED | Present, referenced by both Fulfillment and Orders/Notifications |
| `src/services/fulfillment/ECommerce.Fulfillment.API/Features/Fulfillment/OrderPaidConsumer.cs` | FUL-01/FUL-02 core logic | VERIFIED (wired) — but see CR-01 gap | Filters Paid, schedules delayed OrderShipped correctly; missing cancellation path |
| `src/services/orders/ECommerce.Orders.API/Features/Orders/OrderStateMachine.cs` | Paid -> Fulfilled transition on OrderShipped; catch-alls on all terminal states | VERIFIED | `When(OrderShippedEvent).TransitionTo(Fulfilled)` in `During(Paid,...)`; `Ignore(OrderShippedEvent)` in Cancelled/Fulfilled/Failed/Pending blocks |
| `src/services/notifications/ECommerce.Notifications.API/Features/Notifications/NotificationsEndpoints.cs` | GET /notifications, JWT-scoped | VERIFIED | `.RequireAuthorization()`, filters by `ClaimTypes.NameIdentifier`/`sub` claim |
| `src/services/notifications/ECommerce.Notifications.API/Consumers/{OrderPaidNotificationConsumer,OrderShippedNotificationConsumer,PaymentFailedNotificationConsumer}.cs` | Three consumers persisting NotificationEntry rows | VERIFIED (exists, substantive, wired via Program.cs AddConsumer + outbox) — OrderShippedNotificationConsumer additionally has the CR-01 correctness gap | All three insert rows with UserId/OrderId/Message/EventType/OccurredAt |
| `src/frontend/ecommerce-app/src/app/features/orders/order-detail/order-detail.component.ts` | Polling + "Preparing your shipment…" indicator | VERIFIED | `interval(1500)` polling to terminal status; `isShipping` computed signal gates the spinner in the template |
| `src/frontend/ecommerce-app/src/app/features/notifications/notifications-page/notifications-page.component.ts` | Notification inbox UI | VERIFIED | Renders list/empty/loading/error states; each entry links to `/orders/:id` |
| `src/ecommerce.AppHost/Program.cs` | fulfillment/notifications wired to Postgres | VERIFIED | Both `.WithReference(postgres).WaitFor(postgres)`; gateway references notifications |

### Key Link Verification

| From | To | Via | Status | Details |
|------|-----|-----|--------|---------|
| `OrderPaidConsumer.cs` | `OrderShipped` | `context.SchedulePublish<OrderShipped>(delay, ...)` | WIRED | Confirmed present, delay from `FulfillmentOptions.ProcessingSeconds` |
| `OrderStateMachine.cs During(Paid,...)` | `OrderShippedEvent` | `When(OrderShippedEvent).TransitionTo(Fulfilled)` | WIRED | Confirmed at lines 228-241 |
| `OrderReadModelProjector.cs` | `OrderReadModel.Status` | generic `readModel.Status = msg.NewStatus` | WIRED | No hardcoded status list — "Fulfilled" flows through automatically |
| `order-detail.component.ts` | `GET /api/orders/{id}` | `interval(1500).pipe(switchMap(...))` | WIRED | Confirmed; note WR-03 (review): a transient HTTP error permanently stops polling (not a phase-goal blocker, but a real defect) |
| `NotificationsEndpoints.cs GET /notifications` | `NotificationEntry` | `.Where(n => n.UserId == userId)` | WIRED | Confirmed, IDOR-safe |
| `OrderPaidNotificationConsumer / OrderShippedNotificationConsumer / PaymentFailedNotificationConsumer` | `NotificationEntry` | `db.NotificationEntries.Add(...)` | WIRED | All three confirmed |
| `notifications-page.component.ts` | `GET /api/notifications` | `NotificationsService.getNotifications()` | WIRED | Confirmed |
| `app.html toolbar` | `/notifications` | `routerLink="/notifications"` | WIRED | Confirmed |
| Fulfillment's scheduled `OrderShipped` | Order cancellation path | *(none — this is the gap)* | **NOT_WIRED** | No `CancelScheduledPublish`/status re-check exists between `FulfillmentFailedEvent`-triggered cancellation and the already-scheduled `OrderShipped` publish |

### Behavioral Spot-Checks / Test Execution

`dotnet test` was attempted against `Fulfillment.sln` and `Orders.sln` (fresh `bin`/`obj` clean + rebuild). Both aborted with an environment-level testhost/package resolution error (`testhost version 18.7.0-release-26379-115 not found`, `AnyOf 0.4.0 lib/net7.0/AnyOf.dll not found`) that reproduces identically on Orders' pre-existing (Phase 3/4) test project, confirming this is a pre-existing sandbox/tooling environment issue, not a regression introduced by Phase 05. GitHub Actions CI history shows all recent runs (including main-branch pushes from Phase 3 and Phase 4) failed to even start ("account is locked due to a billing issue"), so CI provides no corroborating signal either. Verification therefore relied on direct static/structural code reading (consumer logic, saga transitions, endpoint filters, EF outbox/inbox wiring, and existing test file contents) rather than executed test runs. This is a **tooling limitation of the verification environment**, not evidence against the phase's correctness — but it also means the dedup test claims for `OrderPaidNotificationConsumer` could not be confirmed by execution, only by reading the test source (which is well-formed and consistent with the existing `CatalogSeededInboxDeduplicationTests` pattern used successfully elsewhere in the repo).

### Requirements Coverage

| Requirement | Source Plan(s) | Description | Status | Evidence |
|-------------|-----------------|-------------|--------|----------|
| FUL-01 | 05-01, 05-04, 05-06 | Fulfillment consumes OrderPaid events and starts processing | SATISFIED | `OrderPaidConsumer.cs` filters `NewStatus == "Paid"` and schedules processing |
| FUL-02 | 05-01, 05-02, 05-03, 05-04, 05-06 | Fulfillment publishes OrderShipped after timer-based simulation | SATISFIED (with caveat) | Scheduled publish confirmed working for the happy path; CR-01 gap affects correctness of downstream consumption, not the publish itself |
| NOT-01 | 05-05, 05-07, 05-08 | User can view in-app notification inbox (GET /notifications) | SATISFIED | Endpoint + frontend page both confirmed wired and substantive |
| NOT-02 | 05-01, 05-03, 05-05, 05-07 | Notifications consumes saga events and persists inbox entries, idempotently | PARTIALLY SATISFIED | Consumption/persistence confirmed for all three event types; idempotency infrastructure (EF outbox/inbox) is wired for all three, but redelivery-proof test coverage exists for only one of three consumers (WR-02), and the OrderShipped consumer specifically has the CR-01 correctness defect (writes a false-positive notification, independent of duplication) |

No orphaned requirements — all four Phase 5 requirement IDs (FUL-01, FUL-02, NOT-01, NOT-02) are declared in at least one plan's `requirements:` frontmatter and cross-reference cleanly against REQUIREMENTS.md's Phase 5 traceability rows.

### Anti-Patterns Found

| File | Line | Pattern | Severity | Impact |
|------|------|---------|----------|--------|
| `src/services/fulfillment/ECommerce.Fulfillment.API/Features/Fulfillment/OrderPaidConsumer.cs` | 24-31 | Unconditional scheduled publish with no cancellation path (CR-01) | Blocker | False "shipped" notification for cancelled/refunded orders — reachable via existing demo endpoint |
| `src/services/notifications/ECommerce.Notifications.API/Consumers/OrderShippedNotificationConsumer.cs` | 20-28 | No status/consistency check before writing customer-facing message (CR-01) | Blocker | Same as above — the consumer-side half of the same defect |
| `src/services/notifications/ECommerce.Notifications.Tests/Integration/OrderShippedNotificationConsumerSteps.cs`, `PaymentFailedNotificationConsumerSteps.cs` | whole file | No `AddEntityFrameworkOutbox`/inbox dedup test (WR-02) | Warning | Redelivery bugs specific to these two consumers would not be caught by the test suite |
| `src/frontend/ecommerce-app/src/app/features/orders/order-detail/order-detail.component.ts` | 73-87 | Polling subscription has no `catchError` recovery (WR-03) | Warning | Transient HTTP error permanently stops live updates with no user-facing indication |
| No `TBD`/`FIXME`/`XXX` markers found in phase-modified files | — | — | — | Debt-marker gate: clean |

### Human Verification Required

See `human_verification` in frontmatter — end-to-end reproduction of CR-01 against a running stack, and visual confirmation of the order-detail/notifications UI, both require runtime execution beyond static verification's scope.

### Gaps Summary

The phase delivers a functionally correct happy-path implementation of both success criteria's mechanics: Fulfillment schedules and publishes `OrderShipped`, the saga transitions Paid->Fulfilled, the read model projects the new status generically, the order-detail page polls to reflect it, and the Notifications inbox is built, JWT-scoped, and wired to all three lifecycle events with real EF Core outbox/inbox idempotency infrastructure in place for all three consumers.

However, one Critical code-review finding (CR-01) represents a genuine, reachable violation of the phase goal's own wording — "the user sees the entire lifecycle reflected in an in-app notification inbox" is not true when the inbox can show a factually false "shipped" notification for an order that was actually cancelled and refunded, via an existing, already-authorized demo flow (no code changes needed to trigger it — just call the existing simulate-fulfillment-failure endpoint within the ~45s processing window). This was independently reproduced by tracing the code during this verification pass (not merely trusted from 05-REVIEW.md): `OrderPaidConsumer.SchedulePublish` has no corresponding unschedule call anywhere in the phase's diff, `OrderStateMachine`'s `FulfillmentFailedEvent` handler in `During(Paid,...)` does not touch the scheduled Fulfillment-side message, and `OrderShippedNotificationConsumer` performs no order-status check before writing its notification.

A secondary, non-blocking gap (WR-02) is the asymmetric inbox-deduplication test coverage: only `OrderPaidNotificationConsumer` has a forced-redelivery proof test; `OrderShippedNotificationConsumer` and `PaymentFailedNotificationConsumer` do not, despite Success Criterion 4 explicitly requiring "verified by a forced-redelivery test producing no duplicate inbox rows" for the notification lifecycle generally. The underlying idempotency wiring (EF Core outbox/inbox) is present and consistent for all three, so this is a test-coverage gap rather than a functional defect — included here because the success criterion's wording specifically calls out test-based verification, not just infrastructure presence.

---

_Verified: 2026-08-19T00:00:00Z_
_Verifier: Claude (gsd-verifier)_
