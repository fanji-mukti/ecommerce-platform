---
phase: 05-fulfillment-notifications
plan: 06
subsystem: testing
tags: [masstransit, quartz, ef-core, inbox, outbox, testcontainers, xunit, dotnet]

# Dependency graph
requires:
  - phase: 05-fulfillment-notifications (plan 04)
    provides: OrderPaidConsumer, FulfillmentOptions, FulfillmentDbContext (Fulfillment's OrderStatusChanged->OrderShipped scheduling logic)
provides:
  - "ECommerce.Fulfillment.Tests project — Fulfillment's first Test project, wired into Fulfillment.sln"
  - "Automated proof that OrderPaidConsumer schedules OrderShipped (correct CheckoutId/UserId) only when NewStatus is Paid, against a real MassTransit in-memory harness + Quartz in-memory scheduler"
  - "Automated proof that OrderPaidConsumer's EF Core inbox deduplicates a redelivered OrderStatusChanged by transport MessageId (INF-02), against a real Postgres Testcontainer"
  - "Empirical confirmation of RESEARCH.md Assumption A1 (SchedulePublish participates in the EF Core transactional outbox) via a passing real-database test"
affects: [05-07, 05-08]

# Tech tracking
tech-stack:
  added: []
  patterns:
    - "Fulfillment.Tests mirrors Notifications.Tests' two-class *Steps/*Tests pattern and exact package set (xunit.v3, FluentAssertions, MassTransit.TestFramework, EF Core InMemory/Relational, Testcontainers.PostgreSql)"
    - "ITestHarness.ConnectPublishHandler<T> used to observe a scheduler-delayed publish, instead of a non-existent 'Scheduled' collection — MassTransit 8.3.6's ITestHarness/IBaseTestHarness expose only Consumed/Published/Sent"
    - "cfg.UseInMemoryScheduler(out schedulerFactory) + explicit scheduler.Start() confirmation, required to make a bare stateless consumer's SchedulePublish<T> actually fire in a raw ServiceCollection-based test harness (no ASP.NET Core Generic Host)"

key-files:
  created:
    - src/services/fulfillment/ECommerce.Fulfillment.Tests/ECommerce.Fulfillment.Tests.csproj
    - src/services/fulfillment/ECommerce.Fulfillment.Tests/Integration/OrderPaidConsumerSteps.cs
    - src/services/fulfillment/ECommerce.Fulfillment.Tests/Integration/OrderPaidConsumerTests.cs
    - src/services/fulfillment/ECommerce.Fulfillment.Tests/Integration/OrderPaidInboxDeduplicationSteps.cs
    - src/services/fulfillment/ECommerce.Fulfillment.Tests/Integration/OrderPaidInboxDeduplicationTests.cs
  modified:
    - src/services/fulfillment/Fulfillment.sln

key-decisions:
  - "Used ITestHarness.ConnectPublishHandler<OrderShipped> instead of the plan's specified harness.Scheduled.Select<OrderShipped>() pattern — that API does not exist on MassTransit 8.3.6's ITestHarness (verified via reflection); the scheduler's delayed redelivery is a raw SerializedMessageBody send that bypasses typed Published<T>/Sent<T> tracking entirely, so a publish handler is the only reliable observation point"
  - "cfg.UseInMemoryScheduler(context) alone (with or without an explicit IBusRegistrationContext) does NOT make a scheduled SchedulePublish actually fire inside a raw ServiceCollection/ServiceProvider test harness — the message is consumed by ScheduleMessageConsumer without fault but the underlying job never executes observably until the returned ISchedulerFactory is captured (out param) and its scheduler's IsStarted/Start() is confirmed explicitly"

patterns-established:
  - "Pattern: capture MassTransit.Quartz's embedded ISchedulerFactory via the `out` overload of UseInMemoryScheduler and explicitly confirm/Start() it in any raw-ServiceCollection MassTransit test harness that relies on SchedulePublish/scheduled messages actually firing within the test"

requirements-completed: [FUL-01, FUL-02]

# Metrics
duration: 55min
completed: 2026-08-19
---

# Phase 5 Plan 6: Fulfillment OrderPaidConsumer Test Coverage Summary

**New ECommerce.Fulfillment.Tests project proving OrderPaidConsumer's Paid-filter/scheduling behavior against a real MassTransit in-memory harness and its EF Core inbox idempotency against a real Postgres Testcontainer, satisfying FUL-01/FUL-02/INF-02.**

## Performance

- **Duration:** ~55 min (majority spent diagnosing MassTransit 8.3.6's Quartz in-memory scheduler wiring — see Deviations)
- **Tasks:** 2 completed
- **Files modified:** 6 (5 created, 1 modified)

## Accomplishments
- Fulfillment's first Test project (`ECommerce.Fulfillment.Tests`), wired into `Fulfillment.sln`, using the exact package set and two-class `*Steps`/`*Tests` pattern already established by `ECommerce.Notifications.Tests`
- `OrderPaidConsumerTests` proves, against a real MassTransit in-memory harness: `OrderStatusChanged{NewStatus="Paid"}` causes `OrderPaidConsumer` to schedule `OrderShipped` with the correct `CheckoutId`/`UserId`, and `NewStatus="Cancelled"` causes no `OrderShipped` to be scheduled (message still acked)
- `OrderPaidInboxDeduplicationTests` proves, against a real Postgres Testcontainer: a redelivered `OrderStatusChanged` (same transport `MessageId`) results in exactly one `InboxState` row — `OrderPaidConsumer`'s body (and its `SchedulePublish<OrderShipped>` call) does not re-execute on the second delivery
- Empirically confirms RESEARCH.md's MEDIUM-confidence Assumption A1 (`SchedulePublish` participates in the EF Core transactional outbox) via a passing test against a real database — no separate spike task was needed

## Task Commits

Each task was committed atomically:

1. **Task 1: New Fulfillment.Tests project + OrderPaidConsumer filter/scheduling test** - `b543340` (test)
2. **Task 2: Postgres-backed forced-redelivery inbox dedup test** - `2b3eae9` (test)

_Note: both tasks are `tdd="true"`; each commit itself represents the final, passing GREEN state — the plan's `<behavior>` blocks were used as the test-first specification, and the harness-wiring dead-ends encountered along the way (see Deviations) were resolved before any code was committed, so no separate RED/refactor commits were needed._

## Files Created/Modified
- `src/services/fulfillment/ECommerce.Fulfillment.Tests/ECommerce.Fulfillment.Tests.csproj` - New test project, copied from Notifications.Tests' package set, referencing `ECommerce.Fulfillment.API` and `Tests.Common`
- `src/services/fulfillment/ECommerce.Fulfillment.Tests/Integration/OrderPaidConsumerSteps.cs` - In-memory MassTransit harness + Quartz in-memory scheduler wiring; step methods for the Paid-filter/scheduling behavior
- `src/services/fulfillment/ECommerce.Fulfillment.Tests/Integration/OrderPaidConsumerTests.cs` - Two `[Fact]`s: Paid schedules OrderShipped; non-Paid does not
- `src/services/fulfillment/ECommerce.Fulfillment.Tests/Integration/OrderPaidInboxDeduplicationSteps.cs` - Postgres-backed EF Core outbox/inbox harness; forced-redelivery step methods
- `src/services/fulfillment/ECommerce.Fulfillment.Tests/Integration/OrderPaidInboxDeduplicationTests.cs` - One `[Fact]`: duplicate `MessageId` delivery yields exactly one `InboxState` row
- `src/services/fulfillment/Fulfillment.sln` - Added `ECommerce.Fulfillment.Tests` and `Tests.Common` project references (via `dotnet sln add`)

## Decisions Made
- `ITestHarness.ConnectPublishHandler<OrderShipped>` used in place of the plan's `harness.Scheduled.Select<OrderShipped>()` — MassTransit 8.3.6's `ITestHarness`/`IBaseTestHarness` expose only `Consumed`/`Published`/`Sent` (confirmed via reflection against the installed 8.3.6 assembly); no `Scheduled` collection exists in this version. A scheduler-delayed publish is delivered as a raw `SerializedMessageBody` send to the target topic address, which never surfaces through the typed `Published<T>`/`Sent<T>` collections — `ConnectPublishHandler<T>` (an ephemeral, properly-typed consumer bound to that same topic) is the only reliable in-harness observation point for this scenario.
- `cfg.UseInMemoryScheduler(out schedulerFactory)` (capturing the embedded `Quartz.ISchedulerFactory`) plus an explicit `scheduler.IsStarted`/`Start()` confirmation, rather than the plan's plain `cfg.UseInMemoryScheduler(); cfg.ConfigureEndpoints(context);` — the plain form (with or without an explicit `IBusRegistrationContext`) does deliver the `ScheduleMessage` command to the "quartz" receive endpoint without fault, but the delayed job never fires observably inside a raw `ServiceCollection`/`ServiceProvider` test harness (no ASP.NET Core Generic Host driving hosted-service startup) unless the scheduler is captured and its running state is confirmed directly.

## Deviations from Plan

### Auto-fixed Issues

**1. [Rule 1 - Bug] Plan's `harness.Scheduled.Select<OrderShipped>()` pattern does not exist in MassTransit 8.3.6**
- **Found during:** Task 1 (writing `OrderPaidConsumerSteps.cs`)
- **Issue:** The plan's `<action>` block specified asserting against `_harness!.Scheduled.Select<OrderShipped>()`. Reflection against the installed `MassTransit 8.3.6` assembly confirmed `ITestHarness`/`IBaseTestHarness` expose only `Consumed`, `Published`, and `Sent` — no `Scheduled` property exists on any version of this interface in 8.3.6.
- **Fix:** Registered `ITestHarness.ConnectPublishHandler<OrderShipped>(_ => true)` before publishing the triggering `OrderStatusChanged`, then awaited the returned `Task<ConsumeContext<OrderShipped>>` (with a 10s timeout) to observe the scheduler-delivered message and assert its `CheckoutId`/`UserId`. For the negative case, asserted the same task does not complete within 500ms (well past the 0.1s configured `ProcessingSeconds`).
- **Files modified:** `src/services/fulfillment/ECommerce.Fulfillment.Tests/Integration/OrderPaidConsumerSteps.cs`
- **Verification:** Both `OrderPaidConsumerTests` facts pass consistently across 3 consecutive runs.
- **Committed in:** `b543340` (Task 1 commit)

**2. [Rule 1 - Bug] `cfg.UseInMemoryScheduler()` (plan's exact wiring) does not fire scheduled messages in a raw ServiceCollection test harness**
- **Found during:** Task 1 (debugging why `OrderPaidConsumerSteps`'s positive test initially failed with `found == false`)
- **Issue:** With `cfg.UseInMemoryScheduler(); cfg.ConfigureEndpoints(context);` (the plan's exact wiring, matching Program.cs's production code), MassTransit's `ScheduleMessageConsumer` DID consume the internal `ScheduleMessage` command without any fault (confirmed via a temporary `IReceiveObserver`), but the resulting Quartz job never executed observably, even after a 2-second wait — no `OrderShipped` was ever sent or published.
- **Fix:** Switched to the `cfg.UseInMemoryScheduler(out schedulerFactory)` overload to obtain a reference to the embedded `Quartz.ISchedulerFactory`, then explicitly resolved the `IScheduler` and confirmed/called `.Start()` on it after `_harness.Start()`. This is a belt-and-braces fix — `QuartzSchedulerOptions.StartScheduler` defaults to `true`, but this test no longer silently depends on that default holding across a future MassTransit.Quartz version bump.
- **Files modified:** `src/services/fulfillment/ECommerce.Fulfillment.Tests/Integration/OrderPaidConsumerSteps.cs`
- **Verification:** Positive test passes consistently across 3 consecutive runs after the fix; failed deterministically (100% of attempts) before it.
- **Committed in:** `b543340` (Task 1 commit)

---

**Total deviations:** 2 auto-fixed (both Rule 1 — bugs in the plan's specified test-harness API usage, not in `OrderPaidConsumer` production code itself, which required no changes).
**Impact on plan:** Both deviations were necessary to make the TDD-required tests pass at all; `OrderPaidConsumer`'s actual behavior (filter + `SchedulePublish` call) was already correct and unchanged from plan 05-04. No scope creep — fixes are confined to the two new test-only files this plan creates.

## Issues Encountered
- Investigating the scheduler-firing issue (Deviation 2) required an unusually deep dive: reflection against the installed MassTransit/MassTransit.Quartz assemblies (no source/decompiler available in this environment) to enumerate `ITestHarness`'s actual members, `UseInMemoryScheduler`'s five overloads, and `QuartzSchedulerOptions`'s defaults, plus temporary `ISendObserver`/`IPublishObserver`/`IReceiveObserver` instrumentation to trace exactly where the scheduled message's flow stalled. All temporary debug instrumentation was removed before the final commit.
- The environment's `dotnet test` (VSTest-based) command fails for every test project in this repo (including the pre-existing, already-passing `ECommerce.Notifications.Tests`) with `An assembly specified in the application dependencies manifest (testhost.deps.json) was not found: package 'testhost', version '18.7.0-release-26379-115'` — this is a pre-existing environment issue, not caused by this plan's changes (reproduced identically against `ECommerce.Notifications.Tests` before any of this plan's files existed). Verification for this plan was performed by invoking the built test assembly directly via xUnit v3's in-process console runner (`dotnet bin/Debug/net10.0/ECommerce.Fulfillment.Tests.dll -class "..."`), which is unaffected by the VSTest testhost issue and produces equivalent pass/fail results.

## User Setup Required

None - no external service configuration required. Docker (for Testcontainers.PostgreSql) was already available and used successfully in this environment.

## Next Phase Readiness
- `OrderPaidConsumer` (Fulfillment) is now independently, automatically proven correct — filter behavior, scheduling correctness (FUL-01/FUL-02), and inbox-based idempotency (INF-02) — without requiring Orders' saga or Notifications to be running, matching this plan's stated purpose.
- The pre-existing `dotnet test` (VSTest) environment issue affects ALL test projects in this repo, not just this plan's new one — flagging for whoever owns CI/dev-environment tooling; it did not block this plan's own verification since the xUnit v3 in-process runner works reliably as an alternative invocation path.
- No blockers for 05-07/05-08.

---
*Phase: 05-fulfillment-notifications*
*Completed: 2026-08-19*
