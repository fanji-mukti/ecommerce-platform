---
phase: 03-cart-orders-skeleton
plan: 02
subsystem: orders
tags: [masstransit, saga, ef-core, postgres, outbox, inbox, jwt, cqrs]

requires:
  - phase: 02-identity-catalog-gateway
    provides: MassTransit outbox/inbox pattern (Notifications consumer), CatalogDbContext/DbInitializer conventions, OpenIddict Identity server, IMessageEnvelope contract shape
provides:
  - Order write aggregate doubling as the first MassTransit saga instance in the codebase
  - OrderStateMachine enforcing Pending -> Paid -> Fulfilled/Cancelled/Failed with real guard logic (ORD-03)
  - OrdersDbContext (write table + read-model table + MassTransit outbox/inbox), applied InitialOrdersSchema migration
  - OrderCreated/OrderStatusChanged domain event contracts replacing the Orders Events placeholder
  - JWT bearer auth wired into Orders.API against Identity's OpenIddict Authority
affects: [03-03 (Orders API + read-model projector), phase-4 (checkout saga wiring the Paid/Fulfilled/Cancelled/Failed triggers)]

tech-stack:
  added: [MassTransit 8.3.6 (saga + outbox), MassTransit.Azure.ServiceBus.Core 8.3.6, MassTransit.EntityFrameworkCore 8.3.6, Aspire.Npgsql.EntityFrameworkCore.PostgreSQL 13.4.4, Riok.Mapperly 4.3.1, Microsoft.EntityFrameworkCore.Design 10.0.9, Microsoft.AspNetCore.Authentication.JwtBearer 10.0.8, MassTransit.TestFramework 8.3.6]
  patterns:
    - "MassTransit saga state machine with trailing catch-all When() combinators per state to reject invalid transitions without throwing"
    - "Single shared owned CLR type (OrderLineItem) configured independently via two OwnsMany calls for write-side and read-side tables"
    - "In-memory MassTransit saga test harness (AddSagaStateMachine<T,TInstance>().InMemoryRepository()) for saga unit tests, no live Postgres/ASB required"

key-files:
  created:
    - src/services/orders/ECommerce.Orders.API/Features/Orders/Order.cs
    - src/services/orders/ECommerce.Orders.API/Features/Orders/OrderLineItem.cs
    - src/services/orders/ECommerce.Orders.API/Features/Orders/OrderReadModel.cs
    - src/services/orders/ECommerce.Orders.API/Features/Orders/OrderStateMachine.cs
    - src/services/orders/ECommerce.Orders.API/Data/OrdersDbContext.cs
    - src/services/orders/ECommerce.Orders.API/Data/DbInitializer.cs
    - src/building-blocks/Contracts/Orders/Events/V1/OrderCreated.cs
    - src/building-blocks/Contracts/Orders/Events/V1/OrderStatusChanged.cs
    - src/services/orders/ECommerce.Orders.API/Migrations/20260722103856_InitialOrdersSchema.cs
    - src/services/orders/ECommerce.Orders.Tests/ (new project: ECommerce.Orders.Tests.csproj, Unit/OrderStateMachineTests.cs, Unit/OrderStateMachineSteps.cs)
  modified:
    - src/services/orders/ECommerce.Orders.API/ECommerce.Orders.API.csproj
    - src/services/orders/ECommerce.Orders.API/Program.cs
    - src/services/orders/Orders.sln

key-decisions:
  - "Order aggregate doubles as the MassTransit saga instance — CorrelationId IS the OrderId, no separate Id property (D-06)"
  - "OrderLineItem is the one and only line-item CLR type, configured as an owned type independently by both Order and OrderReadModel via two separate OwnsMany calls, producing two distinct tables (Orders_LineItems, OrderReadModels_LineItems)"
  - "Every valid-transition During() block ends with a trailing, unfiltered When(event) catch-all with no .TransitionTo() — absorbs invalid NewStatus values as handled-but-ignored, keeping the saga in its current state without an unhandled-event fault (ORD-03, T-03-08)"
  - "JWT bearer auth pinned identically across Orders.API: Authority=http://identity, RequireHttpsMetadata=false, ValidateAudience=false"

patterns-established:
  - "Saga state machine catch-all guard pattern: During(state, When(evt, filter).TransitionTo(next), ..., When(evt)) — trailing unfiltered When() must be last"
  - "Shared owned-type dual-table CQRS pattern: one CLR type, two independent OwnsMany configurations under different owner entities in the same DbContext"

requirements-completed: [ORD-03]

coverage:
  - id: D1
    description: "OrderStateMachine rejects the invalid Pending -> Fulfilled transition (CurrentState remains Pending) while correctly transitioning Pending -> Paid"
    requirement: "ORD-03"
    verification:
      - kind: unit
        ref: "src/services/orders/ECommerce.Orders.Tests/Unit/OrderStateMachineTests.cs#OrderStatusChanged_WhenPendingSkipsToFulfilled_TransitionIsRejected"
        status: pass
      - kind: unit
        ref: "src/services/orders/ECommerce.Orders.Tests/Unit/OrderStateMachineTests.cs#OrderStatusChanged_WhenPendingToPaid_TransitionSucceeds"
        status: pass
    human_judgment: false
  - id: D2
    description: "OrdersDbContext schema (Orders, Orders_LineItems, OrderReadModels, OrderReadModels_LineItems, InboxState, OutboxMessage, OutboxState) applied to a live Postgres instance via InitialOrdersSchema migration"
    verification:
      - kind: other
        ref: "dotnet ef migrations list --project src/services/orders/ECommerce.Orders.API/ECommerce.Orders.API.csproj shows InitialOrdersSchema as applied; psql \\dt confirmed all 8 tables"
        status: pass
    human_judgment: false

duration: 55min
completed: 2026-07-22
status: complete
---

# Phase 3 Plan 2: Orders Write-Side Domain & State Machine Summary

**MassTransit saga state machine (Order/OrderStateMachine) enforcing Pending -> Paid -> Fulfilled/Cancelled/Failed with a trailing-catch-all guard pattern, OrdersDbContext with dual-table CQRS via one shared owned line-item type, and the applied InitialOrdersSchema migration**

## Performance

- **Duration:** 55 min
- **Tasks:** 3
- **Files modified:** 18 (11 new source files, 3 migration files, 3 new test project files, 3 modified: csproj, Program.cs, Orders.sln — some files touched across multiple counts)

## Accomplishments
- First MassTransit saga state machine in the codebase (ADR-0005): `Order`/`OrderStateMachine` enforcing the full Pending -> Paid -> Fulfilled/Cancelled/Failed transition graph, with unit tests proving both the valid path and the rejected invalid-skip path (ORD-03)
- `OrdersDbContext` wired with MassTransit's EF Core outbox/inbox, plus a single shared `OrderLineItem` owned type configured independently for both the write-side `Order` aggregate and the read-side `OrderReadModel`, producing two distinct owned tables from one CLR class
- Orders' first live EF Core migration (`InitialOrdersSchema`) generated and applied against a local Postgres instance — verified via `\dt` and `dotnet ef migrations list`
- `Program.cs` activates the previously-commented MassTransit outbox pattern for real: saga + `AddEntityFrameworkOutbox` + `UseBusOutbox()` + Azure Service Bus transport, plus JWT bearer auth against Identity's OpenIddict Authority
- `OrderCreated`/`OrderStatusChanged` domain event Contracts replace the Orders Events placeholder, following the exact `CatalogSeeded`-style envelope shape

## Task Commits

1. **Task 1: Order aggregate, OrderStateMachine, OrdersDbContext, and domain event contracts** - `e7734ff` (feat)
2. **Task 2: [BLOCKING] Generate and apply the initial Orders EF Core migration** - `40d3f34` (feat)
3. **Task 3: OrderStateMachine transition-guard unit tests** - `343eff3` (test)

## Files Created/Modified
- `src/services/orders/ECommerce.Orders.API/Features/Orders/Order.cs` - Write aggregate + saga instance (SagaStateMachineInstance)
- `src/services/orders/ECommerce.Orders.API/Features/Orders/OrderLineItem.cs` - Single shared owned line-item type
- `src/services/orders/ECommerce.Orders.API/Features/Orders/OrderReadModel.cs` - CQRS read projection shape
- `src/services/orders/ECommerce.Orders.API/Features/Orders/OrderStateMachine.cs` - Saga guard logic (ORD-03)
- `src/services/orders/ECommerce.Orders.API/Data/OrdersDbContext.cs` - EF Core context, outbox/inbox + dual OwnsMany config
- `src/services/orders/ECommerce.Orders.API/Data/DbInitializer.cs` - Migration-on-startup hosted service
- `src/services/orders/ECommerce.Orders.API/Program.cs` - Aspire Npgsql DbContext, MassTransit saga+outbox, JWT bearer auth
- `src/services/orders/ECommerce.Orders.API/ECommerce.Orders.API.csproj` - MassTransit/EF Core/JwtBearer package references pinned
- `src/building-blocks/Contracts/Orders/Events/V1/OrderCreated.cs` - Domain event + OrderLineItemData
- `src/building-blocks/Contracts/Orders/Events/V1/OrderStatusChanged.cs` - Domain event (audit trail: PreviousStatus/NewStatus)
- `src/services/orders/ECommerce.Orders.API/Migrations/20260722103856_InitialOrdersSchema.cs` - Applied EF Core migration
- `src/services/orders/ECommerce.Orders.Tests/ECommerce.Orders.Tests.csproj` - New test project (xunit.v3, MassTransit.TestFramework)
- `src/services/orders/ECommerce.Orders.Tests/Unit/OrderStateMachineSteps.cs` - In-memory saga harness Given/When/Then steps
- `src/services/orders/ECommerce.Orders.Tests/Unit/OrderStateMachineTests.cs` - Invalid-skip and valid-transition Facts
- `src/services/orders/Orders.sln` - Registered ECommerce.Orders.Tests via `dotnet sln add`

## Decisions Made
- Order aggregate IS the saga instance (`CorrelationId` = OrderId) — no separate Id property, per D-06
- `OrderLineItem` remains the single line-item CLR type across write and read sides; two independent `OwnsMany` calls in `OrdersDbContext` (one under `Order`, one under `OrderReadModel`) produce `Orders_LineItems` and `OrderReadModels_LineItems` as separate tables — no duplicate class
- Every `During(state, ...)` block ends with a trailing, unfiltered `When(OrderStatusChangedEvent)` catch-all (no `.TransitionTo()`) so an invalid `NewStatus` is absorbed as handled-but-ignored rather than raising an unhandled-event fault — this is what makes ORD-03's "invalid transitions must be rejected" testable and true
- JWT bearer auth (`Authority = "http://identity"`, `RequireHttpsMetadata = false`, `ValidateAudience = false`) wired directly per the plan's explicit Program.cs guidance, since Plan 03-01 (Cart) had not yet landed in this wave-1 worktree to copy from — confirmed no existing JWT bearer pattern anywhere else in the codebase at execution time, so these exact settings (matching the plan text) are the source of truth
- `Microsoft.AspNetCore.Authentication.JwtBearer` pinned to `10.0.8` to match the existing `Microsoft.AspNetCore.OpenApi` version convention used across all services, since no prior consumer of this package existed yet to copy a version from

## Deviations from Plan

### Auto-fixed Issues

**1. [Rule 1 - Bug] Comment wording collided with the acceptance-criteria grep**
- **Found during:** Task 1 acceptance-criteria verification
- **Issue:** An explanatory code comment in `OrderStateMachine.cs` literally contained the substring `UnhandledEventException`, which the plan's automated acceptance check (`grep -c "UnhandledEventException" ... returns 0`) matched as a false positive even though the exception is never referenced in code, only described in prose.
- **Fix:** Reworded the comment to describe the behavior ("without an unhandled-event fault being raised") without using the literal exception type name.
- **Files modified:** src/services/orders/ECommerce.Orders.API/Features/Orders/OrderStateMachine.cs
- **Verification:** `grep -c "UnhandledEventException" OrderStateMachine.cs` returns 0; rebuild still succeeds.
- **Committed in:** e7734ff (Task 1 commit)

---

**Total deviations:** 1 auto-fixed (1 bug)
**Impact on plan:** Cosmetic fix only — no behavioral change. No scope creep.

## Issues Encountered
- **Environment: `dotnet ef` and the xunit.v3 in-process runner initially blocked by Windows Smart App Control ("Application Control policy has blocked this file", 0x800711C7 / ERROR_VIRUS_INFECTED).** This is a machine-level reputation-check block on freshly-compiled, unsigned local binaries in this sandboxed execution environment — unrelated to the code itself. It cleared after a short delay for the Release build in most cases (confirmed by a subsequent successful direct run and by `dotnet ef` succeeding once `--configuration Release` was used). Not a code defect; no fix needed in source.
- **Environment: `dotnet test src/services/orders/Orders.sln --configuration Release` fails with a VSTest `testhost.deps.json` package-version mismatch** (`package: 'testhost', version: '18.6.0-release-26270-133' ... not found`). Reproduced identically against the pre-existing, already-passing `ECommerce.Catalog.Tests` project, confirming this is a repo-wide/environment-wide toolchain issue in this sandbox, not something introduced by this plan. **Verification workaround:** ran the xunit.v3 self-contained native runner directly (`ECommerce.Orders.Tests.exe -noLogo`), which bypasses the VSTest bridge entirely — confirmed `Total: 2, Errors: 0, Failed: 0, Skipped: 0` for both OrderStateMachine Facts. `dotnet build src/services/orders/Orders.sln --configuration Release` succeeds cleanly (0 errors) and `dotnet ef migrations list` confirms `InitialOrdersSchema` is applied, satisfying the other two `<verification>` items directly.
- Local Postgres for Task 2's migration was provided via an ad hoc `docker run postgres:17-alpine` container (Docker Desktop was not initially running in this environment and had to be started); the container was removed after `dotnet ef database update` completed successfully, leaving no persistent local dependency for future plans (Plan 03-03's Aspire AppHost run will provision its own Postgres resource).

## User Setup Required

None - no external service configuration required. Docker Desktop must be running locally (or an Aspire-provisioned Postgres available) for anyone re-running `dotnet ef database update` against this migration in the future; this was handled ad hoc during this plan's execution and does not require ongoing setup.

## Next Phase Readiness
- Plan 03-03 (Orders API endpoints + read-model projector + `POST /orders/test-create-from-cart`) can now build directly on `OrdersDbContext`, `Order`, `OrderReadModel`, `OrderStateMachine`, and the applied schema.
- `OrderCreated`/`OrderStatusChanged` Contracts are ready to be published from Plan 03-03's endpoint and consumed by a same-service `OrderReadModelProjector` (not yet implemented — Program.cs explicitly notes `OrdersEndpoints.Map(app)`, `OrderReadModelProjector`, and `ICartClient` are Plan 03-03's additions).
- No blockers. The `dotnet test`/testhost environment quirk noted above should be re-verified in a non-sandboxed CI/dev environment before being treated as resolved project-wide, though it is confirmed unrelated to this plan's code.

---
*Phase: 03-cart-orders-skeleton*
*Completed: 2026-07-22*
