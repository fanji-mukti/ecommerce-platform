---
phase: 05-fulfillment-notifications
plan: 05
subsystem: api
tags: [notifications, masstransit, ef-core, jwt, minimal-api, dotnet, idor]

# Dependency graph
requires:
  - phase: 05-fulfillment-notifications (plan 05-01)
    provides: OrderShipped contract (FUL-02) and UserId (D-03) added to OrderStatusChanged/PaymentFailed
provides:
  - "NotificationEntry inbox entity + EF Core migration"
  - "Three MassTransit consumers (OrderPaid/OrderShipped/PaymentFailed) writing locked-copy notification rows"
  - "GET /notifications — JWT-scoped, IDOR-safe list endpoint"
  - "Program.cs 'placeholder' bus-transport sentinel fix, unblocking WebApplicationFactory-based tests"
affects: [05-07, 05-08]

# Tech tracking
tech-stack:
  added:
    - "Microsoft.AspNetCore.Authentication.JwtBearer 10.0.8 (ECommerce.Notifications.API)"
  patterns:
    - "Notifications now follows Orders' AddJwtBearer/AddAuthorization + UseAuthentication/UseAuthorization wiring exactly"
    - "Placeholder connection-string sentinel branch (UsingInMemory vs UsingAzureServiceBus) applied to Notifications, matching Orders/Payments"

key-files:
  created:
    - src/services/notifications/ECommerce.Notifications.API/Features/Notifications/NotificationEntry.cs
    - src/services/notifications/ECommerce.Notifications.API/Features/Notifications/NotificationsEndpoints.cs
    - src/services/notifications/ECommerce.Notifications.API/Consumers/OrderPaidNotificationConsumer.cs
    - src/services/notifications/ECommerce.Notifications.API/Consumers/OrderShippedNotificationConsumer.cs
    - src/services/notifications/ECommerce.Notifications.API/Consumers/PaymentFailedNotificationConsumer.cs
    - src/services/notifications/ECommerce.Notifications.API/Migrations/20260814150207_AddNotificationEntry.cs
    - src/services/notifications/ECommerce.Notifications.Tests/Integration/NotificationConsumersSteps.cs
    - src/services/notifications/ECommerce.Notifications.Tests/Integration/NotificationConsumersTests.cs
  modified:
    - src/services/notifications/ECommerce.Notifications.API/Data/NotificationsDbContext.cs
    - src/services/notifications/ECommerce.Notifications.API/Program.cs
    - src/services/notifications/ECommerce.Notifications.API/ECommerce.Notifications.API.csproj

key-decisions:
  - "Task 2 consumer behavior was proven with an in-memory MassTransit-harness test suite (RED/GREEN) even though the plan's own acceptance criteria were grep-only — genuinely exercises the locked message copy and the NewStatus filter"
  - "Task 3 (JWT auth, GET /notifications, placeholder sentinel) was implemented without a new WebApplicationFactory test — plan 05-07's Task 1 explicitly owns that WebApplicationFactory endpoint test suite (adds Microsoft.AspNetCore.Mvc.Testing to the Tests project, which is outside this plan's files_modified list); duplicating it here would collide with 05-07's planned deliverable"

patterns-established:
  - "NotificationEntry POCO mirrors ProcessedPayment.cs's plain-mutable-entity convention (not a record)"
  - "GetUserId(ClaimsPrincipal) helper copied verbatim from OrdersEndpoints.cs for every new JWT-scoped list endpoint"

requirements-completed: [NOT-01, NOT-02]

# Metrics
duration: 35min
completed: 2026-08-14
---

# Phase 05 Plan 05: Notifications Inbox — Entity, Consumers, JWT-Scoped Endpoint Summary

**NotificationEntry inbox entity + migration, three MassTransit consumers writing locked-copy rows for OrderPaid/OrderShipped/PaymentFailed, and a JWT-scoped IDOR-safe GET /notifications endpoint — Notifications' first authenticated HTTP surface.**

## Performance

- **Duration:** 35 min
- **Started:** 2026-08-14T22:58:00Z
- **Completed:** 2026-08-14T23:33:00Z
- **Tasks:** 3 completed
- **Files modified:** 11 (8 created, 3 modified, plus 2 migration designer/snapshot files)

## Accomplishments
- `NotificationEntry` entity persisted via EF Core with a `UserId` index for the primary `GET /notifications` read pattern; `AddNotificationEntry` migration generated and verified
- Three new MassTransit consumers (`OrderPaidNotificationConsumer`, `OrderShippedNotificationConsumer`, `PaymentFailedNotificationConsumer`) insert correctly-populated rows with the exact D-04 locked message copy and locked `EventType` strings (`"OrderPaid"`, `"OrderShipped"`, `"PaymentFailed"`); `OrderPaidNotificationConsumer` correctly filters `OrderStatusChanged` to only `NewStatus == "Paid"`
- Proved consumer behavior with a genuine RED/GREEN TDD cycle: 6 new tests (`NotificationConsumersTests.cs`) covering insertion for all three events plus the no-op filter for `Cancelled`/`Failed`/`Fulfilled` — all passing against MassTransit's InMemory test harness
- `GET /notifications` is JWT-scoped and IDOR-safe, mirroring `OrdersEndpoints`'s `GET /orders` pattern and `GetUserId` helper verbatim; returns entries newest-first, unpaginated per D-05/RESEARCH.md Open Question 2
- Fixed a pre-existing gap in `Program.cs`: MassTransit bus wiring now branches on the `"placeholder"` connection-string sentinel (matching Orders/Payments), so `WebApplicationFactory`-based tests can start a working host — this directly unblocks plan 05-07's endpoint test suite
- `dotnet build src/services/notifications/Notifications.sln` succeeds with zero errors; full existing Notifications test suite (8 tests total, including 2 pre-existing `CatalogSeeded` tests) passes with zero failures/regressions

## Task Commits

Each task was committed atomically:

1. **Task 1: NotificationEntry entity, DbContext, migration** - `10d1b4b` (feat)
2. **Task 2: Three notification consumers** - `90b156c` (test, RED) → `d8d3a51` (feat, GREEN)
3. **Task 3: JWT auth, GET /notifications, register consumers, fix placeholder-sentinel bus wiring** - `3bf14da` (feat)

_Note: Worktree mode — final plan-metadata commit (SUMMARY.md) is committed separately by this agent; STATE.md/ROADMAP.md are updated centrally by the orchestrator after merge._

## Files Created/Modified
- `src/services/notifications/ECommerce.Notifications.API/Features/Notifications/NotificationEntry.cs` - Inbox entity (Id, UserId, OrderId, Message, EventType, OccurredAt)
- `src/services/notifications/ECommerce.Notifications.API/Data/NotificationsDbContext.cs` - Exposes `NotificationEntries` DbSet with a `UserId` index
- `src/services/notifications/ECommerce.Notifications.API/Migrations/20260814150207_AddNotificationEntry.cs` (+Designer.cs, snapshot) - EF Core migration for the new table
- `src/services/notifications/ECommerce.Notifications.API/Consumers/OrderPaidNotificationConsumer.cs` - Filters `OrderStatusChanged` to `NewStatus == "Paid"`, inserts `EventType="OrderPaid"` row
- `src/services/notifications/ECommerce.Notifications.API/Consumers/OrderShippedNotificationConsumer.cs` - Inserts `EventType="OrderShipped"` row unconditionally
- `src/services/notifications/ECommerce.Notifications.API/Consumers/PaymentFailedNotificationConsumer.cs` - Inserts `EventType="PaymentFailed"` row unconditionally
- `src/services/notifications/ECommerce.Notifications.API/Features/Notifications/NotificationsEndpoints.cs` - `GET /notifications`, JWT-scoped, IDOR-safe
- `src/services/notifications/ECommerce.Notifications.API/Program.cs` - AddJwtBearer/AddAuthorization/UseAuthentication/UseAuthorization wiring, consumer registration, placeholder-sentinel branch fix
- `src/services/notifications/ECommerce.Notifications.API/ECommerce.Notifications.API.csproj` - Added `Microsoft.AspNetCore.Authentication.JwtBearer` 10.0.8
- `src/services/notifications/ECommerce.Notifications.Tests/Integration/NotificationConsumersSteps.cs` - InMemory-harness test steps for all three consumers
- `src/services/notifications/ECommerce.Notifications.Tests/Integration/NotificationConsumersTests.cs` - 6 tests: 3 insertion behaviors + 3 no-op filter cases

## Decisions Made
- Followed the plan's exact field shapes, message copy, and `EventType` string values verbatim (D-04) — no deviation from documented content
- For Task 2 (tdd="true"), wrote a genuine RED/GREEN test cycle even though the plan's acceptance criteria were grep-only, since the `<behavior>` block described concretely testable outcomes within this plan's own scope
- For Task 3 (tdd="true"), did NOT add a new `WebApplicationFactory`-based endpoint test — plan 05-07's Task 1 explicitly owns that exact deliverable (including adding `Microsoft.AspNetCore.Mvc.Testing` to the Tests project, which is outside this plan's `files_modified` list). Verified Task 3 via `dotnet build` + the plan's own grep-based acceptance criteria instead, consistent with the plan's own scoping language ("unblocking WebApplicationFactory-based endpoint tests in plan 05-07")

## Deviations from Plan

None - plan executed exactly as written. (Task 2's test suite is additive test coverage beyond the plan's literal acceptance criteria, not a deviation from the specified behavior.)

## Issues Encountered
- `dotnet test`/`vstest` failed with a `testhost` version-mismatch error (`testhost, version 18.6.0-release-26270-133` not found) — this is a pre-existing environment issue unrelated to this plan's changes (confirmed by reproducing the same failure against the pre-existing `CatalogSeededConsumerTests`). Worked around by invoking the xunit.v3 self-hosted test executable directly (`ECommerce.Notifications.Tests.exe -class "..."`), which the project's `OutputType=Exe` setting supports. All new and existing tests pass via this route.

## User Setup Required

None - no external service configuration required.

## Next Phase Readiness
- NOT-01 and NOT-02 are fully implemented: `GET /notifications` is JWT-scoped/IDOR-safe, and all three producing events write correctly-populated, locked-copy rows
- Plan 05-07 (wave 3) can now build its `WebApplicationFactory`-based `GET /notifications` endpoint test and its Postgres-backed forced-redelivery inbox-dedup test directly against this plan's output — the placeholder-sentinel fix in `Program.cs` is the prerequisite it needed
- `docs/adr/0006-masstransit-outbox-inbox.md`'s EF Core inbox dedup is exercised implicitly (MassTransit `InboxState` dedup by transport `MessageId`) but not yet proven by a forced-redelivery test in this plan — that proof is plan 05-07's Task 3, as designed

---
*Phase: 05-fulfillment-notifications*
*Completed: 2026-08-14*
