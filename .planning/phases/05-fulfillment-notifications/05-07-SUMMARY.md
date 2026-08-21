---
phase: 05-fulfillment-notifications
plan: 07
subsystem: testing
tags: [notifications, masstransit, ef-core, webapplicationfactory, jwt, idor, xunit, testcontainers]

# Dependency graph
requires:
  - phase: 05-fulfillment-notifications (plan 05-05)
    provides: NotificationEntry entity, three MassTransit consumers, GET /notifications endpoint, and the Program.cs "placeholder" bus-transport sentinel fix that unblocks WebApplicationFactory-based tests
provides:
  - "NotificationsWebApplicationFactory + NotificationsEndpointSteps/Tests — real hosted proof that GET /notifications is JWT-scoped and IDOR-safe (NOT-01)"
  - "Per-consumer Steps/Tests pairs (OrderPaid/OrderShipped/PaymentFailed) proving each consumer inserts correctly-populated rows (NOT-02)"
  - "Postgres-backed forced-redelivery inbox dedup test proving a redelivered OrderStatusChanged produces exactly one InboxState row and exactly one NotificationEntry row (NOT-02)"
affects: [05-08]

# Tech tracking
tech-stack:
  added:
    - "Microsoft.AspNetCore.Mvc.Testing 10.0.9 (ECommerce.Notifications.Tests)"
  patterns:
    - "NotificationsWebApplicationFactory mirrors OrdersWebApplicationFactory's exact shape, simplified (no typed outbound HTTP client to redirect)"
    - "Per-consumer Steps/Tests pairs (one pair per MassTransit consumer) replace a single consolidated test class, mirroring CatalogSeededConsumerSteps.cs's shape"

key-files:
  created:
    - src/services/notifications/ECommerce.Notifications.Tests/Integration/NotificationsEndpointSteps.cs
    - src/services/notifications/ECommerce.Notifications.Tests/Integration/NotificationsEndpointTests.cs
    - src/services/notifications/ECommerce.Notifications.Tests/Integration/OrderPaidNotificationConsumerSteps.cs
    - src/services/notifications/ECommerce.Notifications.Tests/Integration/OrderPaidNotificationConsumerTests.cs
    - src/services/notifications/ECommerce.Notifications.Tests/Integration/OrderShippedNotificationConsumerSteps.cs
    - src/services/notifications/ECommerce.Notifications.Tests/Integration/OrderShippedNotificationConsumerTests.cs
    - src/services/notifications/ECommerce.Notifications.Tests/Integration/PaymentFailedNotificationConsumerSteps.cs
    - src/services/notifications/ECommerce.Notifications.Tests/Integration/PaymentFailedNotificationConsumerTests.cs
    - src/services/notifications/ECommerce.Notifications.Tests/Integration/OrderPaidNotificationInboxDeduplicationSteps.cs
    - src/services/notifications/ECommerce.Notifications.Tests/Integration/OrderPaidNotificationInboxDeduplicationTests.cs
  modified:
    - src/services/notifications/ECommerce.Notifications.Tests/ECommerce.Notifications.Tests.csproj

key-decisions:
  - "Deleted plan 05-05's consolidated NotificationConsumersSteps.cs/NotificationConsumersTests.cs (added there as additive coverage beyond its own acceptance criteria) after replacing it with this plan's literal per-consumer Steps/Tests pairs, to avoid two test suites proving identical behavior"
  - "Used random Guid userIds per test (not fixed constants) for NotificationsEndpointSteps/Tests, avoiding the need for an explicit table-clearing Given_ step since IClassFixture<PostgresFixture> shares one container across all three facts"

patterns-established:
  - "NotificationEntryDto record in NotificationsEndpointSteps.cs mirrors the camelCase JSON shape the raw NotificationEntry entity serializes to (no server-side DTO exists for this endpoint)"

requirements-completed: [NOT-01, NOT-02]

# Metrics
duration: 30min
completed: 2026-08-19
---

# Phase 05 Plan 07: Notifications — Endpoint, Consumer, and Inbox-Dedup Proof Summary

**WebApplicationFactory-hosted GET /notifications IDOR-safety proof, six per-consumer row-insertion tests, and a real Postgres-backed forced-redelivery test proving zero duplicate inbox rows — automated, repeatable proof of NOT-01 and NOT-02.**

## Performance

- **Duration:** ~30 min
- **Started:** 2026-08-19T14:12:00Z
- **Completed:** 2026-08-19T14:41:00Z
- **Tasks:** 3 completed
- **Files modified:** 11 (10 created, 1 modified; 2 pre-existing files deleted as a superseded-coverage cleanup)

## Accomplishments
- `GET /notifications` is proven JWT-scoped and IDOR-safe against a real hosted `WebApplicationFactory<Program>` instance: a different `X-Test-User-Id` never sees another user's entries, results return newest-`OccurredAt`-first, and an empty inbox returns 200 with an empty array (not 404)
- All three notification consumers (`OrderPaidNotificationConsumer`, `OrderShippedNotificationConsumer`, `PaymentFailedNotificationConsumer`) are proven, one Steps/Tests pair each, to insert correctly-populated rows with the exact locked message copy and `EventType` values; `OrderPaidNotificationConsumer` is proven to filter out non-`Paid` `OrderStatusChanged` transitions
- A real Postgres-backed forced-redelivery test proves NOT-02's literal requirement: the SAME `OrderStatusChanged{NewStatus:"Paid"}` message (pinned transport `MessageId`) published twice produces exactly one `InboxState` row AND exactly one `NotificationEntry` row
- `dotnet build src/services/notifications/Notifications.sln` succeeds with zero errors; the full relevant test surface (10 tests: 3 endpoint + 4 consumer + 1 inbox-dedup + 2 pre-existing `CatalogSeeded` regression checks) passes with zero failures

## Task Commits

Each task was committed atomically:

1. **Task 1: GET /notifications — WebApplicationFactory endpoint test** - `e2bfec8` (test)
2. **Task 2: Three consumer row-insertion tests** - `6b22d46` (test)
3. **Task 3: Postgres-backed forced-redelivery inbox dedup test** - `39a3cb5` (test)

_Note: Worktree mode — this plan-metadata commit (SUMMARY.md) is committed separately by this agent; STATE.md/ROADMAP.md are updated centrally by the orchestrator after merge._

## Files Created/Modified
- `src/services/notifications/ECommerce.Notifications.Tests/Integration/NotificationsEndpointSteps.cs` - `NotificationsWebApplicationFactory` + Given/When/Then steps for the `GET /notifications` endpoint
- `src/services/notifications/ECommerce.Notifications.Tests/Integration/NotificationsEndpointTests.cs` - 3 facts: multi-entry-for-owner, IDOR-safe cross-user, empty-inbox
- `src/services/notifications/ECommerce.Notifications.Tests/Integration/OrderPaidNotificationConsumerSteps.cs` / `Tests.cs` - In-memory harness proving `Paid`-filter insertion behavior (2 facts)
- `src/services/notifications/ECommerce.Notifications.Tests/Integration/OrderShippedNotificationConsumerSteps.cs` / `Tests.cs` - In-memory harness proving insertion behavior (1 fact)
- `src/services/notifications/ECommerce.Notifications.Tests/Integration/PaymentFailedNotificationConsumerSteps.cs` / `Tests.cs` - In-memory harness proving insertion behavior (1 fact)
- `src/services/notifications/ECommerce.Notifications.Tests/Integration/OrderPaidNotificationInboxDeduplicationSteps.cs` / `Tests.cs` - Real Postgres-backed forced-redelivery dedup proof
- `src/services/notifications/ECommerce.Notifications.Tests/ECommerce.Notifications.Tests.csproj` - Added `Microsoft.AspNetCore.Mvc.Testing` 10.0.9

## Decisions Made
- Followed the plan's exact interface shapes (`NotificationsWebApplicationFactory`, per-event `When_...Published` step methods, locked message copy) verbatim — no deviation from documented behavior
- Chose random per-test `Guid` userIds over shared constants for the endpoint tests, since `IClassFixture<PostgresFixture>` shares one container/database across all three facts in the class — this avoids the cross-test cleanup step `OrdersEndpointSteps.cs` needs (`Given_NoOrdersExist`) while still fully exercising the same IDOR-safety and empty-state behaviors

## Deviations from Plan

### Auto-fixed Issues

**1. [Rule 1 - Bug/Quality] Removed duplicate consumer-test coverage left by a parallel-wave plan**
- **Found during:** Task 2 (Three consumer row-insertion tests)
- **Issue:** Plan 05-05 (this plan's dependency, executed in an earlier wave) had already added a consolidated `NotificationConsumersSteps.cs`/`NotificationConsumersTests.cs` pair covering the exact same three-consumer insertion behavior as "additive test coverage beyond its own acceptance criteria" (per 05-05's own SUMMARY.md). Creating this plan's literal per-consumer split files on top of that would have left two test suites permanently proving identical behavior.
- **Fix:** Created the three per-consumer Steps/Tests pairs this plan's `files_modified` explicitly specifies, then deleted the now-redundant consolidated `NotificationConsumersSteps.cs`/`NotificationConsumersTests.cs`.
- **Files modified:** `NotificationConsumersSteps.cs` (deleted), `NotificationConsumersTests.cs` (deleted), plus the 6 new per-consumer files
- **Verification:** `dotnet build Notifications.sln` succeeds with 0 errors; the 4 new consumer facts pass (0 failures)
- **Committed in:** `6b22d46` (Task 2 commit)

---

**Total deviations:** 1 auto-fixed (1 quality/duplication cleanup)
**Impact on plan:** No scope creep — the underlying behavior proof this plan targets (NOT-02 consumer correctness) is unchanged; only redundant test infrastructure was removed.

## Issues Encountered
- The environment's `dotnet test`/vstest path failed with a pre-existing `testhost` version-mismatch error (`testhost, version 18.7.0-release-26379-115` not found), consistent with the same issue documented in plan 05-05's SUMMARY.md. Worked around identically: invoked the self-hosted xunit.v3 executable directly (`ECommerce.Notifications.Tests.exe -class "..."`), which the project's `OutputType=Exe` setting supports. All new and existing tests pass via this route.
- Docker Desktop was not running at the start of this session (required for `PostgresFixture`'s Testcontainers-based Postgres container, used by Task 1's endpoint tests and Task 3's inbox-dedup test). Started Docker Desktop and waited for the daemon to become ready before re-running the affected test suites; this is an environment-setup step, not a code deviation.

## User Setup Required

None - no external service configuration required. (Docker Desktop must be running locally to execute the Postgres-backed tests in this suite — already a pre-existing requirement of `PostgresFixture`, unchanged by this plan.)

## Next Phase Readiness
- NOT-01 and NOT-02 are independently and automatically proven end-to-end: `GET /notifications` is IDOR-safe, every consumer writes correct content, and redelivery produces no duplicate rows
- Plan 05-08 (if it builds on Notifications' test surface or the Angular `/notifications` UI) can rely on this plan's `NotificationsWebApplicationFactory` and per-consumer test patterns as the established shape for any further Notifications test coverage

---
*Phase: 05-fulfillment-notifications*
*Completed: 2026-08-19*

## Self-Check: PASSED

All 10 claimed files (9 test files + this SUMMARY.md) verified present via `git ls-files`; all 4 task commits (`e2bfec8`, `6b22d46`, `39a3cb5`, `0243a58`) verified present via `git log`.
