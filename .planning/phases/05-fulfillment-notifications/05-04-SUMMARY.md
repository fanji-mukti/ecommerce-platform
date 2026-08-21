---
phase: 05-fulfillment-notifications
plan: 04
subsystem: api
tags: [masstransit, ef-core, postgres, aspire, scheduling, outbox-inbox]

# Dependency graph
requires:
  - phase: 05-fulfillment-notifications (plan 01)
    provides: "OrderShipped contract (Contracts/Fulfillment/Events/V1/OrderShipped.cs) and OrderStatusChanged.UserId field, both already present at this plan's base commit"
provides:
  - "Fulfillment.API as a fully wired, stateless MassTransit consumer service (FUL-01/FUL-02)"
  - "OrderPaidConsumer: filters OrderStatusChanged to NewStatus==Paid, schedules OrderShipped via ConsumeContext.SchedulePublish"
  - "FulfillmentDbContext (outbox/inbox tables only) + DbInitializer + initial EF Core migration"
  - "FulfillmentOptions.ProcessingSeconds (double, default 45) config surface"
  - "AppHost postgres wiring fix for both Fulfillment and Notifications (RESEARCH.md Pitfall 3)"
affects: [05-05, 05-06, orders-saga-integration]

# Tech tracking
tech-stack:
  added: []
  patterns:
    - "Stateless MassTransit consumer + ConsumeContext.SchedulePublish<T> for linear delayed-publish flows (no saga needed)"
    - "Dual-branch scheduler wiring (ASB-native prod / Quartz in-memory test), copied verbatim from Orders.API/Program.cs"

key-files:
  created:
    - src/services/fulfillment/ECommerce.Fulfillment.API/Data/FulfillmentDbContext.cs
    - src/services/fulfillment/ECommerce.Fulfillment.API/Data/DbInitializer.cs
    - src/services/fulfillment/ECommerce.Fulfillment.API/Features/Fulfillment/FulfillmentOptions.cs
    - src/services/fulfillment/ECommerce.Fulfillment.API/Features/Fulfillment/OrderPaidConsumer.cs
    - src/services/fulfillment/ECommerce.Fulfillment.API/Migrations/20260814150228_InitialFulfillmentSchema.cs
  modified:
    - src/services/fulfillment/ECommerce.Fulfillment.API/Program.cs
    - src/services/fulfillment/ECommerce.Fulfillment.API/ECommerce.Fulfillment.API.csproj
    - src/ecommerce.AppHost/Program.cs

key-decisions:
  - "Used a temporary minimal builder.AddNpgsqlDbContext<FulfillmentDbContext> registration in Program.cs to unblock `dotnet ef migrations add` before Task 2's full MassTransit build-out replaced it — matches the plan's own documented contingency for this ordering."

patterns-established:
  - "Fulfillment-style stateless consumer + SchedulePublish pattern: reuse for any future linear wait-then-emit flow with no compensation branches."

requirements-completed: [FUL-01, FUL-02]

# Metrics
duration: 6min
completed: 2026-08-14
---

# Phase 5 Plan 4: Fulfillment Consumer & Scheduler Summary

**Fulfillment.API built out from a bare stub into a stateless MassTransit consumer that filters `OrderStatusChanged{NewStatus=="Paid"}` and schedules `OrderShipped` via `ConsumeContext.SchedulePublish`, using the same dual-branch (ASB-native prod / Quartz in-memory test) scheduler pattern as Orders.API.**

## Performance

- **Duration:** 6 min
- **Started:** 2026-08-14T23:02:59+08:00 (Task 1 commit)
- **Completed:** 2026-08-14T23:05:23+08:00 (Task 2 commit)
- **Tasks:** 2 completed
- **Files modified:** 8 (5 created, 3 modified)

## Accomplishments
- `FulfillmentDbContext` scaffolded with outbox/inbox tables only (no business `DbSet`) — MassTransit's own `InboxState` dedup is the sole idempotency mechanism, per RESEARCH.md Pattern 2
- `OrderPaidConsumer` implements FUL-01/FUL-02: filters the Paid transition, schedules `OrderShipped` carrying the same `CheckoutId`/`UserId` as the triggering event, with no thread held open for the delay (RESEARCH.md "Don't Hand-Roll")
- `Program.cs` fully wired with the exact dual-branch scheduler copied from `Orders.API` (ASB-native scheduler in production, Quartz `UseInMemoryScheduler()` in the in-memory/test branch)
- AppHost's pre-existing Pitfall 3 gap closed for both Fulfillment and Notifications — both now resolve `ConnectionStrings:postgres` outside of `WebApplicationFactory`-based tests

## Task Commits

Each task was committed atomically:

1. **Task 1: FulfillmentDbContext, DbInitializer, FulfillmentOptions, package references, migration** - `4308bb7` (feat)
2. **Task 2: OrderPaidConsumer, Program.cs full build-out, AppHost postgres-reference fix** - `fc46830` (feat)

_Note: Task 1's commit includes a temporary Program.cs edit (minimal DbContext registration) required to unblock `dotnet ef migrations add`; Task 2 replaced it with the full MassTransit build-out in the same file._

## Files Created/Modified
- `src/services/fulfillment/ECommerce.Fulfillment.API/Data/FulfillmentDbContext.cs` - Outbox/inbox tables only, copied verbatim from `NotificationsDbContext` (class rename only)
- `src/services/fulfillment/ECommerce.Fulfillment.API/Data/DbInitializer.cs` - `IHostedService` running `db.Database.MigrateAsync()` at startup
- `src/services/fulfillment/ECommerce.Fulfillment.API/Features/Fulfillment/FulfillmentOptions.cs` - `ProcessingSeconds` (double, default 45), mirrors `CheckoutOptions.TimeoutMinutes`
- `src/services/fulfillment/ECommerce.Fulfillment.API/Features/Fulfillment/OrderPaidConsumer.cs` - `IConsumer<OrderStatusChanged>`, filters to Paid, schedules `OrderShipped`
- `src/services/fulfillment/ECommerce.Fulfillment.API/Program.cs` - Full MassTransit build-out: DbContext registration, options binding, outbox, dual-branch scheduler, `DbInitializer` hosted service — no auth/HTTP client wiring
- `src/services/fulfillment/ECommerce.Fulfillment.API/ECommerce.Fulfillment.API.csproj` - Added `MassTransit`/`MassTransit.Azure.ServiceBus.Core`/`MassTransit.EntityFrameworkCore`/`MassTransit.Quartz` @ 8.3.6, `Aspire.Npgsql.EntityFrameworkCore.PostgreSQL` @ 13.4.4, `Microsoft.EntityFrameworkCore.Design` @ 10.0.9
- `src/services/fulfillment/ECommerce.Fulfillment.API/Migrations/20260814150228_InitialFulfillmentSchema.cs` (+ Designer.cs, ModelSnapshot.cs) - Initial migration for outbox/inbox tables
- `src/ecommerce.AppHost/Program.cs` - Added `.WithReference(postgres).WaitFor(postgres)` to both `fulfillment` and `notifications` project registrations

## Decisions Made
- Followed the plan's own documented contingency: added a temporary minimal `FulfillmentDbContext` registration in `Program.cs` to make the design-time build resolvable for `dotnet ef migrations add`, then replaced it with the full build-out in Task 2. No new decision beyond what the plan specified.

## Deviations from Plan

None — plan executed exactly as written. Package versions, class shapes, and wiring patterns all matched the plan's `<action>` blocks and RESEARCH.md's verified code examples exactly.

## Issues Encountered

**Cross-plan build dependency (not a defect in this plan's work):** `dotnet build src/ecommerce.AppHost/ecommerce.AppHost.sln` currently fails with 7 compile errors, all located in `OrderStateMachine.cs` (Orders) and `AuthorisePaymentConsumer.cs` (Payments) — neither file is in this plan's `files_modified` list, and neither was touched by this plan's work. Root cause: plan 05-01 (already merged into this wave-2 worktree base) added a required `UserId` parameter to `OrderStatusChanged`, `AuthorisePayment`, and `PaymentFailed` contract records; the call-site updates in Orders/Payments are explicitly owned by the parallel wave-2 plan **05-03** (confirmed via grep across phase plan files), which was not yet merged into this worktree at execution time. `Fulfillment.sln` (this plan's own solution) builds cleanly with zero errors — verified independently. This is a known, expected state for parallel wave-2 execution; the orchestrator's wave merge will resolve it once 05-03 lands. No action taken here per the deviation rules' scope boundary (pre-existing/unrelated-file failures are out of scope for auto-fix).

## User Setup Required

None - no external service configuration required.

## Next Phase Readiness
- `OrderPaidConsumer` and the dual-branch scheduler are ready for the Orders saga to bind `Event<OrderShipped>` in `During(Paid, ...)` (05-03's scope, per RESEARCH.md Code Examples)
- Fulfillment's AppHost wiring is complete; full-repo `dotnet build` on `ecommerce.AppHost.sln` will succeed once 05-03's Orders/Payments call-site updates land in the merged wave
- No blockers introduced by this plan

---
*Phase: 05-fulfillment-notifications*
*Completed: 2026-08-14*

## Self-Check: PASSED

All created files verified present on disk; all three commits (4308bb7, fc46830, 4282d6a) verified in git log.
