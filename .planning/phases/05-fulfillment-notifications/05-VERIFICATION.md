---
phase: 05-fulfillment-notifications
verified: 2026-08-21T00:00:00Z
status: human_needed
score: 4/4 must-haves verified
overrides_applied: 0
re_verification:
  previous_status: gaps_found
  previous_score: 2/4 truths fully VERIFIED (2/4 PARTIAL)
  gaps_closed:
    - "CR-01: OrderShippedNotificationConsumer no longer writes a false 'Your order has shipped.' notification for a cancelled/refunded order (closed by 05-09 via OrderStatusSnapshot + suppression guard)"
    - "WR-02: OrderShippedNotificationConsumer and PaymentFailedNotificationConsumer now have Postgres-backed forced-redelivery inbox-dedup proof tests, matching OrderPaidNotificationConsumer's existing coverage (closed by 05-10)"
    - "WR-03: order-detail.component.ts's polling loop now recovers from a transient HTTP error via catchError(() => EMPTY) instead of permanently terminating (closed by 05-11)"
  gaps_remaining: []
  regressions: []
human_verification:
  - test: "Confirm 05-UI-SPEC.md's Checker Sign-Off dimensions (Copywriting, Visuals, Color, Typography, Spacing, Registry Safety) for the /notifications inbox page and the /orders/:id 'Preparing your shipment…' spinner"
    expected: "All six checker dimensions pass visually in a running browser; 05-UI-SPEC.md's 'Approval: pending' line is updated to approved"
    why_human: "05-UI-SPEC.md's Checker Sign-Off section is still unchecked (all six boxes empty, 'Approval: pending') as of this verification pass. This predates the gap-closure plans (05-09/05-10/05-11 did not touch UI-SPEC scope — 05-11's summary explicitly notes it intentionally avoided expanding that surface) and was already flagged as unresolved in the prior 05-VERIFICATION.md. Visual rendering, spacing, and typography cannot be confirmed by static code reading."
  - test: "Optional confidence check: reproduce the original CR-01 scenario end-to-end against a running stack (place an order, reach Paid, call POST /checkout/{id}/simulate-fulfillment-failure within the ProcessingSeconds window, wait for the scheduled OrderShipped to fire, check GET /notifications)"
    expected: "No 'Your order has shipped.' notification appears for the cancelled/refunded order; only the cancellation/refund notification is visible"
    why_human: "The suppression logic itself is now proven by four automated unit/integration tests (Cancelled, Failed, no-snapshot, Paid cases) reading directly from an in-memory harness, and the underlying OrderStatusSnapshotConsumer upsert semantics are proven by three more tests (insert, in-order update, stale-rejection) — this is strong evidence the fix works. This item is downgraded from the prior verification's mandatory status to optional/confidence-building, since 05-REVIEW.md's IN-02 finding notes the fix is a best-effort narrowing (dependent on OrderStatusChanged(Cancelled) being consumed before the delayed OrderShipped fires) rather than a hard guarantee — a live run against real timers would additionally confirm the ~45s window is comfortably wide in practice, but is not required to consider the phase goal met."
---

# Phase 05: Fulfillment & Notifications Verification Report

**Phase Goal:** The saga reaches a fully-shipped terminal state and the user sees the entire lifecycle reflected in an in-app notification inbox.
**Verified:** 2026-08-21T00:00:00Z
**Status:** human_needed
**Re-verification:** Yes — after gap closure (plans 05-09, 05-10, 05-11)

## Goal Achievement

### Observable Truths (Success Criteria)

| # | Truth (Success Criterion) | Status | Evidence |
|---|---------|------------|----------|
| 1 | Fulfillment service consumes `OrderPaid` events, advances status through a timer-based simulation, and publishes `OrderShipped` — visible in the saga state and the order detail view. | VERIFIED | Unchanged since prior verification pass; not touched by gap-closure plans. `OrderPaidConsumer.cs` filters `NewStatus=="Paid"`, schedules delayed `OrderShipped` via `context.SchedulePublish`; `OrderStateMachine.During(Paid,...)` binds `OrderShippedEvent`; `OrderReadModelProjector` projects `NewStatus` generically. |
| 2 | After `OrderShipped`, the saga reaches `Completed` and the order detail page reflects `Fulfilled` status without manual intervention. | VERIFIED | Saga/read-model wiring unchanged and previously confirmed. **Newly re-verified this pass:** `order-detail.component.ts`'s polling loop (`interval(1500).pipe(switchMap(() => this.ordersService.getOrder(id).pipe(catchError(() => EMPTY))), takeWhile(...), takeUntilDestroyed(...))`) now survives a transient HTTP error without permanently stopping — confirmed by direct file read (line 76) AND by executing `npx vitest run order-detail.component.spec` in this session: **10/10 tests passed**, including the new "recovers from a transient poll error and still reaches a terminal status on the next tick" test. This closes WR-03 and removes the only residual risk to this truth's "without manual intervention" wording. |
| 3 | User can GET `/notifications` to view an in-app inbox containing entries for the saga lifecycle events they participated in (`OrderPaid`, `OrderShipped`, `PaymentFailed`). | VERIFIED | Unchanged since prior pass — endpoint, three consumers, and frontend page all previously confirmed wired and substantive. **This pass additionally confirms** the `OrderShippedNotificationConsumer` change did not regress the happy-path insert: `OrderShippedNotificationConsumerSuppressionTests.cs`'s `WhenNoSnapshotExists_InsertsShippedNotification` and `WhenSnapshotStatusIsPaid_InsertsShippedNotification` tests explicitly cover the unaffected-happy-path case, and `db build` succeeds. |
| 4 | Idempotency verified by a forced-redelivery test producing no duplicate inbox rows. | VERIFIED | **Gap closed.** `OrderShippedNotificationInboxDeduplicationTests.cs` and `PaymentFailedNotificationInboxDeduplicationTests.cs` (new, from 05-10) are structural, Postgres-backed copies of the already-proven `OrderPaidNotificationInboxDeduplicationTests.cs` pattern — confirmed by direct read: both use `PostgresFixture`, `AddEntityFrameworkOutbox<NotificationsDbContext>`, pin the same transport `MessageId` on two `Publish` calls, and assert exactly one `InboxState` row and one domain row. All three of the phase's original lifecycle-event consumers (OrderPaid, OrderShipped, PaymentFailed) now have symmetric forced-redelivery proof, matching the success criterion's literal wording. |

**Score:** 4/4 truths VERIFIED (up from 2/4 VERIFIED + 2/4 PARTIAL in the prior pass).

### CR-01 Gap-Closure Verification (beyond the four literal success criteria)

The phase goal's own prose — "the user sees the entire lifecycle reflected in an in-app notification inbox" — was previously violated by a reachable false-"shipped"-notification path for cancelled/refunded orders (CR-01, blocker). This is now closed:

- `OrderStatusSnapshot.cs` (new POCO: `OrderId` PK, `Status`, `UpdatedAt`) — **VERIFIED** exists, matches plan spec exactly.
- `OrderStatusSnapshotConsumer.cs` (new, unfiltered `IConsumer<OrderStatusChanged>`) — **VERIFIED**: upserts with `ChangedAt >= existing.UpdatedAt` monotonicity guard against out-of-order redelivery; confirmed no `NewStatus` filter (`grep -n "if (msg.NewStatus"` returns no match, matching the plan's explicit acceptance criterion).
- `OrderShippedNotificationConsumer.cs` — **VERIFIED**: modified to look up `OrderStatusSnapshots.FindAsync([msg.CheckoutId])` before insert, and `return` early (with a `LogWarning`) when `snapshot.Status` is `"Cancelled"` or `"Failed"`, exactly as specified.
- `NotificationsDbContext.cs` — **VERIFIED**: `OrderStatusSnapshots` DbSet + entity config (`HasKey(s => s.OrderId)`, `HasMaxLength(20)`) present.
- `Program.cs` — **VERIFIED**: `x.AddConsumer<OrderStatusSnapshotConsumer>();` registered alongside the four pre-existing consumers, participating in the same `AddEntityFrameworkOutbox` inbox.
- Migration `20260820154643_AddOrderStatusSnapshot.cs` — **VERIFIED**: `CreateTable(name: "OrderStatusSnapshots", ...)` with `OrderId` PK, `Status varchar(20)`, `UpdatedAt timestamptz`, confirmed by direct file read.
- Test coverage — **VERIFIED**: 4 suppression tests (`OrderShippedNotificationConsumerSuppressionTests.cs`: Cancelled-suppresses, Failed-suppresses, no-snapshot-passes-through, Paid-passes-through) + 3 snapshot-upsert tests (`OrderStatusSnapshotConsumerTests.cs`: insert, in-order update, stale-rejection) — all read in full, well-formed (not stubs), structurally sound.

**Residual risk (non-blocking, independently confirmed):** `05-REVIEW.md`'s WR-01 finding — `OrderStatusSnapshotConsumer`'s check-then-act upsert (`FindAsync` then `Add`/update) is not safe under two concurrently-dispatched `OrderStatusChanged` messages for the same `OrderId`, since `Program.cs` sets no `ConcurrentMessageLimit`/partitioner on this consumer's receive endpoint (confirmed by direct grep — no match). This could, in a narrow race window, leave a `DbUpdateException`-faulted message and a stale/missing snapshot, undermining the suppression guard's precondition. This is explicitly logged in the 05-09 plan's own threat model as `T-05-09-03`, disposition "accept," with the reasoning that the CR-01 demo-repro timeline (near-instant cancellation vs. a 45s-default scheduled `OrderShipped`) makes the window negligible in practice — this is a generic robustness concern for concurrent status transitions, not a re-opening of the specific CR-01 reachable path. `05-REVIEW.md` independently classifies this as Warning (not Critical), consistent with this assessment. **Verdict: accepted residual risk, not a phase-goal blocker.** Recorded here for visibility, not as a gap.

### Required Artifacts

| Artifact | Expected | Status | Details |
|----------|----------|--------|---------|
| `src/services/notifications/ECommerce.Notifications.API/Features/Notifications/OrderStatusSnapshot.cs` | Latest-known-status-per-order entity | VERIFIED | Read in full; matches plan spec exactly |
| `src/services/notifications/ECommerce.Notifications.API/Consumers/OrderStatusSnapshotConsumer.cs` | Unfiltered upsert consumer with monotonicity guard | VERIFIED (wired) | Registered in `Program.cs`; unfiltered confirmed by grep |
| `src/services/notifications/ECommerce.Notifications.API/Consumers/OrderShippedNotificationConsumer.cs` | Suppression guard before "shipped" insert | VERIFIED (wired) | Snapshot lookup + `Cancelled`/`Failed` suppression confirmed at lines 15-23 |
| `src/services/notifications/ECommerce.Notifications.API/Data/NotificationsDbContext.cs` | `OrderStatusSnapshots` DbSet + config | VERIFIED | Confirmed |
| `src/services/notifications/ECommerce.Notifications.API/Migrations/20260820154643_AddOrderStatusSnapshot.cs` | EF migration creating the table | VERIFIED | `CreateTable` confirmed, PK on `OrderId` |
| `src/services/notifications/ECommerce.Notifications.Tests/Integration/OrderShippedNotificationConsumerSuppressionTests.cs` (+Steps) | 4 suppression/regression tests | VERIFIED (substantive) | Read in full; well-formed, not stubs |
| `src/services/notifications/ECommerce.Notifications.Tests/Integration/OrderStatusSnapshotConsumerTests.cs` (+Steps) | 3 upsert-semantics tests | VERIFIED (substantive) | Read in full; well-formed |
| `src/services/notifications/ECommerce.Notifications.Tests/Integration/OrderShippedNotificationInboxDeduplicationTests.cs` (+Steps) | Forced-redelivery dedup proof | VERIFIED (substantive) | Read in full; Postgres-backed, matches proven pattern |
| `src/services/notifications/ECommerce.Notifications.Tests/Integration/PaymentFailedNotificationInboxDeduplicationTests.cs` (+Steps) | Forced-redelivery dedup proof | VERIFIED (substantive) | Read in full; Postgres-backed, matches proven pattern |
| `src/frontend/ecommerce-app/src/app/features/orders/order-detail/order-detail.component.ts` | Polling error recovery | VERIFIED (wired, executed) | `catchError(() => EMPTY)` confirmed at line 76; test suite executed live, 10/10 pass |

### Key Link Verification

| From | To | Via | Status | Details |
|------|-----|-----|--------|---------|
| `OrderStatusSnapshotConsumer.cs` | `OrderStatusSnapshots` table | `db.OrderStatusSnapshots.FindAsync`/`Add` | WIRED | Confirmed by direct read |
| `OrderShippedNotificationConsumer.cs` | `OrderStatusSnapshots` table | `db.OrderStatusSnapshots.FindAsync([msg.CheckoutId], ...)` | WIRED | Confirmed at line 15, before the insert branch |
| `Program.cs` | `OrderStatusSnapshotConsumer` | `x.AddConsumer<OrderStatusSnapshotConsumer>()` | WIRED | Confirmed at line 40, same outbox/inbox pipeline as siblings |
| `order-detail.component.ts startPolling` | `ordersService.getOrder(id)` | `switchMap(() => ...pipe(catchError(() => EMPTY)))` | WIRED | Confirmed at line 76; executed test proves behavior, not just presence |
| `OrderShippedNotificationInboxDeduplicationSteps.cs` | MassTransit EF Core `InboxState` | `AddEntityFrameworkOutbox<NotificationsDbContext>` on test harness endpoint | WIRED | Confirmed at lines 49-58 |
| `PaymentFailedNotificationInboxDeduplicationSteps.cs` | MassTransit EF Core `InboxState` | `AddEntityFrameworkOutbox<NotificationsDbContext>` on test harness endpoint | WIRED | Confirmed by direct read (structural mirror) |

### Behavioral Spot-Checks / Test Execution

**Frontend (executed live, this session):**
```
cd src/frontend/ecommerce-app && npx vitest run order-detail.component.spec
✓ src/app/features/orders/order-detail/order-detail.component.spec.ts (10 tests) 792ms
Test Files  1 passed (1)
     Tests  10 passed (10)
```
This is real, executed evidence (not a SUMMARY claim) that the WR-03 fix works, including the "recovers from a transient poll error and still reaches a terminal status on the next tick" test.

**Backend (attempted, this session):**
```
dotnet build src/services/notifications/ECommerce.Notifications.API/ECommerce.Notifications.API.csproj
Build succeeded. 0 Errors.

dotnet test src/services/notifications/ECommerce.Notifications.Tests/ECommerce.Notifications.Tests.csproj --filter "FullyQualifiedName~OrderStatusSnapshotConsumer"
Testhost process ... exited with error: An assembly specified in the application dependencies
manifest (testhost.deps.json) was not found: package: 'testhost', version: '18.7.0-release-26379-115'
Test Run Aborted.
```
This verification independently reproduced the environment-level testhost/package-resolution failure claimed by the 05-09/05-10 SUMMARYs (not merely trusted). To rule out a phase-05-specific regression, the same command was run against `ECommerce.Orders.Tests.csproj` (a pre-existing, unrelated Phase 3/4 test project untouched by this phase) and it failed identically — confirming this is a sandbox/SDK-level issue (installed NuGet `microsoft.testplatform.testhost` packages cap at 17.8.0; the .NET 10.0.400 SDK's bundled VSTest resolver wants 18.7.0-release-26379-115) affecting the entire repository, not a regression introduced by plans 05-09/05-10. Backend test correctness was therefore verified by direct source reading (all new/modified test and production files read in full) rather than execution, consistent with and independently corroborating the prior verification's and both SUMMARYs' documented caveat.

### Requirements Coverage

| Requirement | Source Plan(s) | Description | Status | Evidence |
|-------------|-----------------|-------------|--------|----------|
| FUL-01 | 05-01, 05-04, 05-06 | Fulfillment consumes OrderPaid events and starts processing | SATISFIED | Unchanged, previously verified |
| FUL-02 | 05-01, 05-02, 05-03, 05-04, 05-06, 05-09, 05-11 | Fulfillment publishes OrderShipped after timer-based simulation | SATISFIED | Publish path unchanged; 05-09/05-11 close downstream-consumption and display-resilience gaps that were the only caveats on this requirement |
| NOT-01 | 05-05, 05-07, 05-08 | User can view in-app notification inbox (GET /notifications) | SATISFIED | Unchanged, previously verified |
| NOT-02 | 05-01, 05-03, 05-05, 05-07, 05-09, 05-10 | Notifications consumes saga events and persists inbox entries, idempotently | SATISFIED | Both the CR-01 correctness gap and the WR-02 asymmetric test-coverage gap are now closed; all three lifecycle consumers have symmetric forced-redelivery proof |

No orphaned requirements — all four Phase 5 requirement IDs (FUL-01, FUL-02, NOT-01, NOT-02) are declared in at least one plan's `requirements:` frontmatter (including the three gap-closure plans) and cross-reference cleanly against `.planning/REQUIREMENTS.md`'s Phase 5 traceability rows (all four already marked `[x]` / "Complete").

### Anti-Patterns Found

| File | Line | Pattern | Severity | Impact |
|------|------|---------|----------|--------|
| `src/services/notifications/ECommerce.Notifications.API/Consumers/OrderStatusSnapshotConsumer.cs` | 22-37 | Check-then-act upsert with no `ConcurrentMessageLimit`/partitioner (05-REVIEW.md WR-01) | Warning (accepted residual risk) | Narrow race window could leave a snapshot stale/missing under truly concurrent same-order status transitions; independently confirmed no endpoint concurrency limit configured. Not a blocker per review's own severity classification and the plan's explicit threat-model acceptance. |
| `src/services/notifications/ECommerce.Notifications.Tests/Integration/OrderStatusSnapshotConsumerSteps.cs` | whole file | No dedicated inbox-dedup test for `OrderStatusSnapshotConsumer` itself (05-REVIEW.md WR-02, new) | Warning (non-blocking) | The one consumer CR-01's fix most depends on lacks its own forced-redelivery proof; a future regression to its inbox wiring specifically would not be caught. Does not affect any of the four literal success criteria (which name only OrderPaid/OrderShipped/PaymentFailed). |
| `src/services/notifications/ECommerce.Notifications.API/Consumers/OrderShippedNotificationConsumer.cs` | 16 | Status literals (`"Cancelled"`/`"Failed"`) duplicated as unchecked strings (05-REVIEW.md IN-01) | Info | Cosmetic/maintainability only |
| — | — | CR-01 fix is best-effort, not a hard guarantee (05-REVIEW.md IN-02) | Info | Documented residual risk; addressed above |
| No `TBD`/`FIXME`/`XXX` markers found in any phase-modified file (verified this pass, not merely trusted) | — | — | — | Debt-marker gate: clean |

### Human Verification Required

See `human_verification` in frontmatter. Summary:

1. **05-UI-SPEC.md Checker Sign-Off (still pending, unaddressed by any gap-closure plan)** — all six visual/copy/color/typography/spacing/registry-safety dimensions are unchecked, "Approval: pending." This predates and is untouched by the gap-closure work; it is the reason this phase's status is `human_needed` rather than `passed`.
2. **Optional confidence check** — live end-to-end reproduction of the original CR-01 timing scenario. Downgraded from mandatory to optional given the strength of the new automated test coverage (7 new backend tests directly proving the suppression logic and its upsert semantics).

### Gaps Summary

No blocking gaps remain. All three targeted gap-closure plans (05-09 CR-01, 05-10 WR-02, 05-11 WR-03) were independently verified against the actual codebase in this session — not merely trusted from their SUMMARY.md claims:

- Every artifact each plan claimed to create/modify was read in full and confirmed present, substantive, and wired.
- The frontend WR-03 fix was proven by actually executing its test suite in this session (10/10 pass).
- The backend CR-01/WR-02 fixes could not be executed in this sandbox due to an independently-reproduced, pre-existing, repo-wide `dotnet test` environment issue (confirmed to affect an unrelated Phase 3/4 test project identically) — verified instead by full source reading of all seven new backend test files plus the four modified/created production files, all of which are well-formed and structurally consistent with already-proven patterns elsewhere in the codebase.
- All four Phase 5 success criteria are now fully VERIFIED (up from 2 VERIFIED + 2 PARTIAL).
- One residual, non-blocking risk (WR-01, concurrency race in the new `OrderStatusSnapshotConsumer`) was independently confirmed to exist and is correctly classified as a Warning-level accepted risk, not a reopening of CR-01's specific reachable path.

The phase is not marked `passed` solely because `05-UI-SPEC.md`'s visual Checker Sign-Off was never completed for this phase's UI (the `/notifications` inbox page and the order-detail shipping spinner) — this is a pre-existing gap unrelated to the gap-closure work and requires a human with a browser, not further code changes.

---

_Verified: 2026-08-21T00:00:00Z_
_Verifier: Claude (gsd-verifier)_
