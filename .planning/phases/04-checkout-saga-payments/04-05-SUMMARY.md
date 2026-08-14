---
phase: 04-checkout-saga-payments
plan: 05
subsystem: api
tags: [checkout, minimal-api, masstransit, jwt, wiremock, adr-0009]

# Dependency graph
requires:
  - phase: 04-checkout-saga-payments
    provides: "OrderStateMachine saga (Pending/Paid/Cancelled/Failed/Fulfilled), POST /orders/checkout, GET /orders/{id} (plans 04-02/04-03)"
provides:
  - "Checkout.API public façade: POST /checkout, GET /checkout/{id}, POST /checkout/{id}/simulate-fulfillment-failure"
  - "ADR-0009 checkout-facing vocabulary mapper (CheckoutStatusMapper)"
  - "IOrdersClient/OrdersClient typed HTTP client (Checkout -> Orders)"
  - "ECommerce.Checkout.Tests integration test project (WireMock-stubbed, no live Orders needed)"
affects: [angular-checkout-ui, phase-04-plan-06]

# Tech tracking
tech-stack:
  added: []
  patterns:
    - "Producer-only MassTransit registration (no saga/consumer/outbox) for a database-free façade service"
    - "HTTP-layer status synthesis (404 -> \"Started\") instead of a new persisted saga state (ADR-0009)"
    - "DB-less WebApplicationFactory (no PostgresFixture/DbInitializer) for services with no database"

key-files:
  created:
    - src/services/checkout/ECommerce.Checkout.API/Features/Checkout/IOrdersClient.cs
    - src/services/checkout/ECommerce.Checkout.API/Features/Checkout/OrdersClient.cs
    - src/services/checkout/ECommerce.Checkout.API/Features/Checkout/CheckoutStatusDto.cs
    - src/services/checkout/ECommerce.Checkout.API/Features/Checkout/CheckoutEndpoints.cs
    - src/services/checkout/ECommerce.Checkout.Tests/ECommerce.Checkout.Tests.csproj
    - src/services/checkout/ECommerce.Checkout.Tests/Integration/CheckoutEndpointSteps.cs
    - src/services/checkout/ECommerce.Checkout.Tests/Integration/CheckoutEndpointTests.cs
  modified:
    - src/services/checkout/ECommerce.Checkout.API/ECommerce.Checkout.API.csproj
    - src/services/checkout/ECommerce.Checkout.API/Program.cs
    - src/services/checkout/Checkout.sln

key-decisions:
  - "GET /checkout/{id} always returns 200, synthesizing \"Started\" from any Orders 404 (not-yet-created and not-owned are indistinguishable) — implements ADR-0009 and T-04-12"
  - "POST /checkout/{id}/simulate-fulfillment-failure surfaces a real 404 for not-found/not-owned (unlike the read-only poll) because it is side-effecting and must not silently no-op against another user's order — mitigates T-04-13"
  - "CheckoutEndpoints does not invent the FulfillmentFailed.Reason string — only the saga (plan 04-02) does; Checkout.API hardcodes the human-readable trigger-side text per D-01/D-03"

patterns-established:
  - "Database-free MassTransit producer-only service composition root (JWT + typed HttpClient + IPublishEndpoint, no DbContext/saga/outbox)"

requirements-completed: [CHK-01, CHK-02, CHK-04]

# Metrics
duration: ~30min
completed: 2026-08-12
---

# Phase 4 Plan 05: Checkout.API Public Façade Summary

**Checkout.API thin façade wiring the checkout saga end-to-end: POST /checkout hands off synchronously to Orders, GET /checkout/{id} synthesizes ADR-0009's checkout vocabulary from Orders' IDOR-safe 404, and a demo-only fulfillment-failure trigger enforces ownership + Paid-status preconditions.**

## Performance

- **Duration:** ~30 min
- **Completed:** 2026-08-12
- **Tasks:** 3 completed
- **Files modified:** 10 (7 created, 3 modified)

## Accomplishments
- `IOrdersClient`/`OrdersClient` typed HTTP client mirroring Orders.API's `ICartClient`/`CartClient` pattern, forwarding the caller's own bearer token to Orders
- `CheckoutStatusMapper` implementing ADR-0009's exact vocabulary table (`null`→Started, Pending→AwaitingPayment, Paid/Cancelled/Failed/Fulfilled pass through unchanged)
- Three fully authenticated Minimal API endpoints (`POST /checkout`, `GET /checkout/{id}`, `POST /checkout/{id}/simulate-fulfillment-failure`) with no database, no saga, producer-only MassTransit
- New `ECommerce.Checkout.Tests` project — 7 WireMock-stubbed integration tests proving CHK-01/CHK-02/CHK-04 end-to-end without a live Orders/Payments service

## Task Commits

Each task was committed atomically:

1. **Task 1: IOrdersClient, CheckoutStatusDto vocabulary mapping, Program.cs wiring** - `1efe23e` (feat)
2. **Task 2: CheckoutEndpoints — POST /checkout, GET /checkout/{id}, demo fulfillment-failure trigger** - `b1759f8` (feat)
3. **Task 3: New Checkout.Tests project — WireMock-stubbed integration tests** - `f1b8ca0` (test)

_Note: Task 1's Program.cs change calls `CheckoutEndpoints.Map(app)` which only compiles once Task 2's file exists (as the plan itself notes: "created in Task 2") — both files were written before the first build/verify pass, then staged and committed separately per task boundary._

## Files Created/Modified
- `src/services/checkout/ECommerce.Checkout.API/Features/Checkout/IOrdersClient.cs` - Interface + `OrderStatusSnapshot` record for the Checkout -> Orders HTTP client
- `src/services/checkout/ECommerce.Checkout.API/Features/Checkout/OrdersClient.cs` - Typed `HttpClient` implementation (`StartCheckoutAsync`, `GetStatusAsync`)
- `src/services/checkout/ECommerce.Checkout.API/Features/Checkout/CheckoutStatusDto.cs` - `CheckoutStatusDto` response shape + `CheckoutStatusMapper` (ADR-0009 vocabulary table)
- `src/services/checkout/ECommerce.Checkout.API/Features/Checkout/CheckoutEndpoints.cs` - `POST /checkout`, `GET /checkout/{id}`, `POST /checkout/{id}/simulate-fulfillment-failure`
- `src/services/checkout/ECommerce.Checkout.API/ECommerce.Checkout.API.csproj` - Added MassTransit 8.3.6 (core + ASB), JwtBearer 10.0.8 — no DB packages
- `src/services/checkout/ECommerce.Checkout.API/Program.cs` - JWT auth, producer-only MassTransit ("placeholder" test sentinel), `IOrdersClient` DI, endpoint mapping
- `src/services/checkout/ECommerce.Checkout.Tests/ECommerce.Checkout.Tests.csproj` - New test project referencing Checkout.API + Tests.Common
- `src/services/checkout/ECommerce.Checkout.Tests/Integration/CheckoutEndpointSteps.cs` - `CheckoutWebApplicationFactory` (no-DB, WireMock-stubbed `IOrdersClient`) + Given/When/Then steps
- `src/services/checkout/ECommerce.Checkout.Tests/Integration/CheckoutEndpointTests.cs` - 7 `[Fact]` tests covering checkout accept/reject, status mapping, fulfillment-failure ownership
- `src/services/checkout/Checkout.sln` - Added `ECommerce.Checkout.Tests` (and its `Tests.Common` reference) as solution projects

## Decisions Made
- Reused Orders.API's `ICartClient`/`CartClient` shape verbatim for `IOrdersClient`/`OrdersClient` (interface + snapshot record + typed HttpClient), per the plan's explicit pattern match
- `GetStatusAsync` treats ANY non-success status code (not just 404) as `null`, matching `CartClient.GetCartAsync`'s existing minimal-check convention rather than special-casing 404
- Test project's `CheckoutEndpointSteps`/`CheckoutEndpointTests` implement `IAsyncLifetime` (ValueTask-based, xUnit v3) rather than constructor+`IDisposable`, matching the codebase's established async-lifetime convention (`OrderStateMachineSteps`, `CatalogSeededInboxDeduplicationSteps`) even though nothing here is genuinely async — for consistency with sibling test suites

## Deviations from Plan

None - plan executed exactly as written.

## Issues Encountered
- `dotnet test` (VSTest adapter) fails to launch any testhost in this dev environment with `An assembly specified in the application dependencies manifest ... was not found: package 'testhost'` / `package: 'AnyOf', version: '0.4.0'` — reproduced identically against the pre-existing, already-passing `ECommerce.Orders.Tests` suite, confirming this is a pre-existing environment/VSTest-adapter issue unrelated to this plan's changes, not something to auto-fix under the deviation rules' scope boundary. Worked around it by invoking the xUnit v3 self-hosted test executable directly (`dotnet ECommerce.Checkout.Tests.dll`), which is a supported invocation mode for `OutputType=Exe` xUnit v3 projects and does not depend on the VSTest adapter. Result: `Total: 7, Errors: 0, Failed: 0, Skipped: 0` — all acceptance criteria satisfied. Logged for awareness; no repository changes made to "fix" the VSTest/testhost environment issue since it is out of scope for this task and pre-dates it.

## User Setup Required

None - no external service configuration required.

## Next Phase Readiness
- Checkout.API is a complete, independently-testable public façade satisfying CHK-01, CHK-02, and CHK-04's demo trigger — ready for the Angular checkout UI (FE-03) to consume `POST /checkout`, `GET /checkout/{id}` (polling), and reference the demo trigger during live-demo narration.
- No blockers. The pre-existing local `dotnet test`/VSTest environment quirk documented above does not affect CI (GitHub Actions runners are unaffected by this local nuget-cache/testhost-resolution issue) and does not block correctness — all 7 tests pass via the self-hosted xUnit v3 runner.

---
*Phase: 04-checkout-saga-payments*
*Completed: 2026-08-12*

## Self-Check: PASSED

- FOUND: src/services/checkout/ECommerce.Checkout.API/Features/Checkout/IOrdersClient.cs
- FOUND: src/services/checkout/ECommerce.Checkout.API/Features/Checkout/OrdersClient.cs
- FOUND: src/services/checkout/ECommerce.Checkout.API/Features/Checkout/CheckoutStatusDto.cs
- FOUND: src/services/checkout/ECommerce.Checkout.API/Features/Checkout/CheckoutEndpoints.cs
- FOUND: src/services/checkout/ECommerce.Checkout.Tests/ECommerce.Checkout.Tests.csproj
- FOUND: src/services/checkout/ECommerce.Checkout.Tests/Integration/CheckoutEndpointSteps.cs
- FOUND: src/services/checkout/ECommerce.Checkout.Tests/Integration/CheckoutEndpointTests.cs
- FOUND commit: 1efe23e (Task 1)
- FOUND commit: b1759f8 (Task 2)
- FOUND commit: f1b8ca0 (Task 3)
