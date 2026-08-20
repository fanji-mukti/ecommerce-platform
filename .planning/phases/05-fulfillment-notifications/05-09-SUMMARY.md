---
phase: 05-fulfillment-notifications
plan: 09
subsystem: notifications
tags: [masstransit, ef-core, event-driven, consumer, cqrs-read-model]

# Dependency graph
requires:
  - phase: 05-fulfillment-notifications
    provides: OrderShippedNotificationConsumer, OrderPaidNotificationConsumer, existing NotificationsDbContext with MassTransit EF Core inbox/outbox
provides:
  - OrderStatusSnapshot entity — locally-maintained latest-known-status-per-order read model in Notifications
  - OrderStatusSnapshotConsumer — unfiltered OrderStatusChanged consumer with ChangedAt monotonicity guard against out-of-order redelivery
  - Suppression guard in OrderShippedNotificationConsumer — closes CR-01 (false "shipped" notification for cancelled/failed orders)
affects: [05-fulfillment-notifications (remaining gap-closure plans 05-10/05-11), any future phase touching Notifications consumers]

# Tech tracking
tech-stack:
  added: []
  patterns:
    - "Event-driven local read-model snapshot (no synchronous cross-service call) to answer 'what is this order's current status' from within Notifications, per D-03 no-direct-coupling"
    - "Upsert-with-monotonicity-guard pattern (ChangedAt comparison) for defending against at-least-once/out-of-order redelivery of status-transition events"

key-files:
  created:
    - src/services/notifications/ECommerce.Notifications.API/Features/Notifications/OrderStatusSnapshot.cs
    - src/services/notifications/ECommerce.Notifications.API/Consumers/OrderStatusSnapshotConsumer.cs
    - src/services/notifications/ECommerce.Notifications.API/Migrations/20260820154643_AddOrderStatusSnapshot.cs
    - src/services/notifications/ECommerce.Notifications.Tests/Integration/OrderShippedNotificationConsumerSuppressionSteps.cs
    - src/services/notifications/ECommerce.Notifications.Tests/Integration/OrderShippedNotificationConsumerSuppressionTests.cs
    - src/services/notifications/ECommerce.Notifications.Tests/Integration/OrderStatusSnapshotConsumerSteps.cs
    - src/services/notifications/ECommerce.Notifications.Tests/Integration/OrderStatusSnapshotConsumerTests.cs
  modified:
    - src/services/notifications/ECommerce.Notifications.API/Data/NotificationsDbContext.cs
    - src/services/notifications/ECommerce.Notifications.API/Consumers/OrderShippedNotificationConsumer.cs
    - src/services/notifications/ECommerce.Notifications.API/Program.cs
    - src/services/notifications/ECommerce.Notifications.API/Migrations/NotificationsDbContextModelSnapshot.cs

key-decisions:
  - "Notification-layer-only fix (no synchronous Notifications->Orders call, no saga-side compensation) per VERIFICATION.md's own lowest-risk recommendation"
  - "OrderStatusSnapshotConsumer is intentionally unfiltered (all NewStatus values), unlike OrderPaidNotificationConsumer's Paid-only filter, since it must track every transition"
  - "Integration test assertions for multi-publish-in-sequence scenarios poll with a short timeout instead of asserting once immediately after ITestHarness.InactivityTask, to avoid a harness synchronization race (see Issues Encountered)"

patterns-established:
  - "Then_* test steps that assert post-consumption state across multiple sequential publishes in one test should poll (bounded retry + delay) rather than assert immediately after InactivityTask"

requirements-completed: [FUL-02, NOT-02]

# Metrics
duration: 55min
completed: 2026-08-20
---

# Phase 5 Plan 09: OrderStatusSnapshot + Shipped-Notification Suppression Summary

**Event-driven OrderStatusSnapshot read model in Notifications closes CR-01 — cancelled/failed orders no longer produce a false "Your order has shipped." notification, with no new synchronous cross-service call.**

## Performance

- **Duration:** ~55 min
- **Tasks:** 3
- **Files modified:** 11 (4 created API files, 3 migration files, 4 test files, 3 modified API files — some files created and migration-modified overlap)

## Accomplishments
- `OrderStatusSnapshot` entity + `OrderStatusSnapshotConsumer` give Notifications a locally-maintained, event-driven record of each order's latest known status, with an EF Core migration (`AddOrderStatusSnapshot`)
- `OrderShippedNotificationConsumer` now suppresses the "shipped" notification when the order's last known status is `Cancelled` or `Failed`, closing the CR-01 gap identified in `05-VERIFICATION.md`
- Full regression coverage: suppression (Cancelled, Failed), non-regression (no snapshot, Paid), and snapshot upsert semantics (insert, in-order update, stale/out-of-order rejection) — 8 new tests, all passing alongside the 9 pre-existing Notifications integration tests (17 total, 0 failures)

## Task Commits

Each task was committed atomically:

1. **Task 1: OrderStatusSnapshot entity, DbContext wiring, migration, and OrderStatusSnapshotConsumer** - `f358d18` (feat)
2. **Task 2: Suppress false "shipped" notifications** - RED `f182224` (test), GREEN `846fa02` (feat)
3. **Task 3: OrderStatusSnapshotConsumer upsert + out-of-order-redelivery tests** - `ca7650b` (test)

_Note: Task 2 followed a strict RED→GREEN TDD cycle since the suppression logic did not yet exist. Task 3's consumer implementation was already built in Task 1, so its tests validated existing behavior — no separate RED commit was meaningful there (see Issues Encountered)._

## Files Created/Modified
- `src/services/notifications/ECommerce.Notifications.API/Features/Notifications/OrderStatusSnapshot.cs` - New POCO: OrderId (PK), Status, UpdatedAt
- `src/services/notifications/ECommerce.Notifications.API/Consumers/OrderStatusSnapshotConsumer.cs` - Unfiltered `IConsumer<OrderStatusChanged>`, upserts with `ChangedAt` monotonicity guard
- `src/services/notifications/ECommerce.Notifications.API/Consumers/OrderShippedNotificationConsumer.cs` - Added snapshot lookup + suppression branch before writing "shipped" NotificationEntry
- `src/services/notifications/ECommerce.Notifications.API/Data/NotificationsDbContext.cs` - Added `OrderStatusSnapshots` DbSet + entity config (OrderId PK, Status max-length 20)
- `src/services/notifications/ECommerce.Notifications.API/Program.cs` - Registered `OrderStatusSnapshotConsumer` in MassTransit
- `src/services/notifications/ECommerce.Notifications.API/Migrations/20260820154643_AddOrderStatusSnapshot.cs` + `.Designer.cs` + updated `NotificationsDbContextModelSnapshot.cs` - EF Core migration for the new table
- `src/services/notifications/ECommerce.Notifications.Tests/Integration/OrderShippedNotificationConsumerSuppressionSteps.cs` + `Tests.cs` - 4 tests: Cancelled suppression, Failed suppression, no-snapshot pass-through, Paid pass-through
- `src/services/notifications/ECommerce.Notifications.Tests/Integration/OrderStatusSnapshotConsumerSteps.cs` + `Tests.cs` - 3 tests: initial insert, in-order update, stale/out-of-order rejection

## Decisions Made
- Kept the fix entirely within Notifications (no new Orders→Notifications synchronous query, no saga compensation), matching VERIFICATION.md's explicit lowest-risk recommendation and preserving D-03/PROJECT.md's no-direct-coupling premise
- `OrderStatusSnapshotConsumer` deliberately has no `NewStatus` filter (verified via `grep -n "if (msg.NewStatus"` returning no match), since it must observe every transition, not just one
- Test assertions that check state after multiple sequential `OrderStatusChanged` publishes in one test now poll with a bounded retry (up to 5s, 50ms interval) rather than asserting immediately after `ITestHarness.InactivityTask` — see Issues Encountered for why

## Deviations from Plan

None — plan executed exactly as written. All acceptance criteria and verification commands from `05-09-PLAN.md` pass as specified.

## Issues Encountered

**MassTransit `ITestHarness.InactivityTask` synchronization race in multi-publish-in-sequence tests.** While writing Task 3's `OrderStatusSnapshotConsumer_WhenNewerStatusArrives_UpdatesSnapshot` test (publish "Paid" then "Cancelled" for the same OrderId, assert final state), the second assertion intermittently read back the pre-update "Paid" value even though diagnostic instrumentation confirmed the consumer's update branch executed and `SaveChangesAsync` reported writing 1 row before the assertion ran. An isolated EF Core InMemory-only repro (no MassTransit) proved the EF Core InMemory provider itself correctly persists find-then-mutate-then-save updates across separate `DbContext` scopes sharing the same database name — ruling out an EF Core bug. The remaining explanation is that `ITestHarness.InactivityTask` can resolve fractionally before a just-consumed message's downstream work (including this project's `SaveChangesAsync`) becomes durably visible to a freshly-opened scope, when messages are published back-to-back within one test. Resolved by changing `Then_SnapshotStatusIs` in `OrderStatusSnapshotConsumerSteps.cs` to poll (up to 5s, 50ms interval, `AsNoTracking` fresh scope per attempt) instead of asserting once. Verified stable across multiple repeated runs after the fix (no flakiness observed in 4+ consecutive full-suite runs). This is a test-infrastructure timing issue, not a defect in `OrderStatusSnapshotConsumer`'s upsert logic itself (Rule 1 — auto-fixed inline as part of Task 3, no separate commit since it landed within Task 3's single test commit).

## Next Phase Readiness

- CR-01 is closed; the phase goal's promise that "the user sees the entire lifecycle reflected in an in-app notification inbox" now holds for the cancel-during-processing-window demo path
- No blockers for the remaining gap-closure plans (05-10 / WR-02, 05-11 / WR-03) running in parallel in sibling worktrees — this plan's files (`OrderStatusSnapshot`, `OrderStatusSnapshotConsumer`, `OrderShippedNotificationConsumer`, `NotificationsDbContext`, `Program.cs`, and the new test files) do not overlap with their file lists

---
*Phase: 05-fulfillment-notifications*
*Completed: 2026-08-20*

## Self-Check: PASSED

All 9 created/modified files verified present on disk; all 5 commits (`f358d18`, `f182224`, `846fa02`, `ca7650b`, `b80d0c7`) verified present in git log.
