---
phase: 03-cart-orders-skeleton
plan: 03
subsystem: orders
tags: [masstransit, ef-core, postgres, outbox, inbox, cqrs, minimal-api, mapperly, wiremock, idor]

# Dependency graph
requires:
  - phase: 03-cart-orders-skeleton
    provides: "Plan 03-01's Cart.API (GET/DELETE /cart HTTP contract), Plan 03-02's Order/OrderReadModel/OrderStateMachine, OrderCreated/OrderStatusChanged Contracts, OrdersDbContext with MassTransit EF outbox/inbox"
provides:
  - OrderReadModelProjector (IConsumer<OrderCreated>/IConsumer<OrderStatusChanged>) idempotently materializing the CQRS read model
  - ICartClient/CartClient typed HTTP client forwarding the caller's bearer token to Cart.API
  - OrdersEndpoints (GET /orders, GET /orders/{id}, POST /orders/test-create-from-cart) — IDOR-safe, demo-marked test trigger
  - OrderBuilder (Tests.Common) fluent test-data builder for OrderReadModel
  - Orders integration test suite proving ORD-01/02/04 and CART-04 end-to-end against real Postgres + WireMock + MassTransit outbox/inbox
affects: ["phase-4 (checkout saga replaces POST /orders/test-create-from-cart with the real saga-driven /checkout flow)"]

# Tech tracking
tech-stack:
  added: [WireMock.Net (Cart HTTP stub in integration tests), Riok.Mapperly OrderMapper (DI-registered)]
  patterns:
    - "Minimal API service parameters (e.g. a Mapperly-generated mapper) MUST be registered in DI — otherwise ASP.NET Core's parameter-source inference misreads them as an implicit request body and crashes route registration for GET endpoints at host startup"
    - "MassTransit 'placeholder' connection-string sentinel branches Program.cs onto UsingInMemory transport in tests, UsingAzureServiceBus otherwise — established by Catalog, now also applied to a service with a live (not commented-out) AddMassTransit configuration"
    - "Shared PostgresFixture/database across all [Fact]s in an integration test class requires explicit clear-before-seed (or clear-before-assert) calls for count/order-sensitive tests, mirroring Catalog's ProductsEndpointSteps convention"
    - "HttpContent can only be read once per HttpResponseMessage — steps that need both a status/body assertion AND a follow-up assertion on the same response must return the parsed body from the first read and pass it forward, not re-invoke ReadFromJsonAsync"

key-files:
  created:
    - src/services/orders/ECommerce.Orders.API/Features/Orders/OrderReadModelProjector.cs
    - src/services/orders/ECommerce.Orders.API/Features/Orders/ICartClient.cs
    - src/services/orders/ECommerce.Orders.API/Features/Orders/CartClient.cs
    - src/services/orders/ECommerce.Orders.API/Features/Orders/OrdersEndpoints.cs
    - src/services/orders/ECommerce.Orders.API/Features/Orders/OrderDto.cs
    - src/services/orders/ECommerce.Orders.API/Features/Orders/OrderMapper.cs
    - src/services/orders/ECommerce.Orders.API/Features/Orders/PaginationHelper.cs
    - src/building-blocks/Tests.Common/Builders/OrderBuilder.cs
    - src/services/orders/ECommerce.Orders.Tests/Integration/OrdersEndpointTests.cs
    - src/services/orders/ECommerce.Orders.Tests/Integration/OrdersEndpointSteps.cs
    - src/services/orders/ECommerce.Orders.Tests/Integration/OrderReadModelInboxDeduplicationTests.cs
    - src/services/orders/ECommerce.Orders.Tests/Integration/OrderReadModelInboxDeduplicationSteps.cs
  modified:
    - src/services/orders/ECommerce.Orders.API/Program.cs
    - src/services/orders/ECommerce.Orders.Tests/ECommerce.Orders.Tests.csproj

key-decisions:
  - "OrderMapper (Mapperly-generated, stateless) registered as a DI singleton in Program.cs — required for Minimal API to recognize it as a service parameter rather than an inferred request body"
  - "OrdersWebApplicationFactory points ICartClient's typed HttpClient BaseAddress at a per-test WireMockServer instance instead of the real Cart.API, and replaces JwtBearer with the shared TestAuthHandler"
  - "GET /orders/{id} returns the identical 404 shape whether the order doesn't exist or belongs to another user — no branch reveals which (IDOR-safe, T-03-10)"
  - "Cart is cleared (DELETE /cart) only after OrderCreated's outbox-backed SaveChangesAsync completes — proven by an integration test asserting exactly one DELETE call happening after publish"

patterns-established:
  - "Register generated/stateless mapper classes in DI before wiring them as Minimal API handler parameters"
  - "Integration test steps that need to assert on a response body more than once must capture and pass the parsed DTO forward rather than re-reading HttpContent"

requirements-completed: [ORD-01, ORD-02, ORD-04, CART-04]

coverage:
  - id: D1
    description: "OrderReadModelProjector idempotently materializes OrderReadModel rows from OrderCreated/OrderStatusChanged, with a defense-in-depth guard alongside MassTransit's own inbox"
    requirement: "ORD-04"
    verification:
      - kind: integration
        ref: "src/services/orders/ECommerce.Orders.Tests/Integration/OrderReadModelInboxDeduplicationTests.cs#OrderReadModelProjector_WhenSameMessageIdDeliveredTwice_InboxAndReadModelEachContainExactlyOneRow"
        status: pass
    human_judgment: false
  - id: D2
    description: "GET /orders lists only the caller's orders, paginated and ordered by CreatedAt desc / Id asc, returning an empty 200 for a user with no orders"
    requirement: "ORD-01"
    verification:
      - kind: integration
        ref: "src/services/orders/ECommerce.Orders.Tests/Integration/OrdersEndpointTests.cs#GetOrders_ForUserWithMultipleOrders_ReturnsPagedListOrderedByCreatedAtDescending"
        status: pass
      - kind: integration
        ref: "src/services/orders/ECommerce.Orders.Tests/Integration/OrdersEndpointTests.cs#GetOrders_ForUserWithNoOrders_ReturnsEmptyItemsAndZeroCount"
        status: pass
    human_judgment: false
  - id: D3
    description: "GET /orders/{id} is IDOR-safe: identical 404 for a non-existent order and for an order owned by a different user"
    requirement: "ORD-02"
    verification:
      - kind: integration
        ref: "src/services/orders/ECommerce.Orders.Tests/Integration/OrdersEndpointTests.cs#GetOrderById_WhenOwnedByDifferentUser_Returns404NotFoundNotForbidden"
        status: pass
      - kind: integration
        ref: "src/services/orders/ECommerce.Orders.Tests/Integration/OrdersEndpointTests.cs#GetOrderById_WhenNotFound_Returns404"
        status: pass
    human_judgment: false
  - id: D4
    description: "GET /orders/{id} may briefly 404 immediately after creation and eventually returns 200 once OrderReadModelProjector processes OrderCreated (eventual consistency)"
    requirement: "ORD-04"
    verification:
      - kind: integration
        ref: "src/services/orders/ECommerce.Orders.Tests/Integration/OrdersEndpointTests.cs#GetOrderById_AfterTestCreateFromCart_EventuallyBecomesVisible"
        status: pass
    human_judgment: false
  - id: D5
    description: "POST /orders/test-create-from-cart rejects an empty cart with 400 and never calls DELETE /cart; a populated cart returns 202 + orderId and clears the cart exactly once, after OrderCreated is published"
    requirement: "CART-04"
    verification:
      - kind: integration
        ref: "src/services/orders/ECommerce.Orders.Tests/Integration/OrdersEndpointTests.cs#TestCreateFromCart_WhenCartIsEmpty_Returns400AndNeverCallsDelete"
        status: pass
      - kind: integration
        ref: "src/services/orders/ECommerce.Orders.Tests/Integration/OrdersEndpointTests.cs#TestCreateFromCart_WhenCartHasItems_Returns202AndClearsCartExactlyOnce"
        status: pass
    human_judgment: false

duration: ~25min (resumed session; Task 1 was executed and committed in a prior, separately-timed session)
completed: 2026-08-04
status: complete
---

# Phase 3 Plan 3: Orders Read API, Projector & Cart-to-Order Test Trigger Summary

**IDOR-safe Orders read API (GET /orders, GET /orders/{id}) backed by an asynchronously-populated CQRS read model, plus a demo-marked POST /orders/test-create-from-cart that snapshots a user's cart into a Pending order via the MassTransit outbox and clears the cart only after the publish durably lands**

## Performance

- **Duration:** ~25 min (this resumed session — a prior executor run completed and committed Task 1, then stopped mid-plan due to an unrelated authentication/session expiry; this session verified Task 1's on-disk state, completed Task 2's partially-written test suite, fixed two bugs it surfaced, and committed)
- **Tasks:** 2
- **Files modified:** 14 (7 new API/source files from Task 1 already committed at 4f3bd50; 5 new test files + 1 new builder + 2 modified files committed in this session at 9644667)

## Accomplishments
- `OrderReadModelProjector` consumes `OrderCreated`/`OrderStatusChanged` and idempotently materializes `OrderReadModel` (Task 1, already committed)
- `ICartClient`/`CartClient` typed HTTP client forwards the caller's own bearer token to Cart.API for the get-then-clear flow (Task 1, already committed)
- `OrdersEndpoints` expose `GET /orders`, `GET /orders/{id}` (IDOR-safe — identical 404 for "not found" and "belongs to someone else"), and the explicitly demo-marked `POST /orders/test-create-from-cart` (Task 1, already committed)
- Full integration test suite (`OrdersEndpointTests`/`Steps`, `OrderReadModelInboxDeduplicationTests`/`Steps`) proves ORD-01, ORD-02, ORD-04, and CART-04 end-to-end against a real Postgres Testcontainer, a WireMock-stubbed Cart, and MassTransit's EF Core outbox/inbox — including a polling-based eventual-consistency test and a forced-redelivery dedup test asserting both `InboxState` and `OrderReadModel` end up with exactly one row (Task 2, this session)
- Surfaced and fixed a real startup-crashing bug: `OrderMapper` was never registered in DI, so Minimal API's parameter-source inference misread it as an implicit request body on two GET routes and crashed host startup — only discoverable at runtime, invisible to `dotnet build`

## Task Commits

1. **Task 1: Read-model projector, Cart HTTP client, and Orders endpoints** - `4f3bd50` (feat) — completed and committed in a prior session before an unrelated auth/session interruption
2. **Task 2: Orders integration tests (eventual-consistency polling, IDOR, forced redelivery)** - `9644667` (test) — completed and committed in this resumed session, including the DI-registration and test-isolation bug fixes it surfaced

**Plan metadata:** (this commit)

## Files Created/Modified
- `src/services/orders/ECommerce.Orders.API/Features/Orders/OrderReadModelProjector.cs` - IConsumer<OrderCreated>/IConsumer<OrderStatusChanged>, defense-in-depth idempotency guard
- `src/services/orders/ECommerce.Orders.API/Features/Orders/ICartClient.cs` / `CartClient.cs` - Typed HTTP client, forwards caller's bearer token to Cart.API
- `src/services/orders/ECommerce.Orders.API/Features/Orders/OrdersEndpoints.cs` - GET /orders, GET /orders/{id}, POST /orders/test-create-from-cart
- `src/services/orders/ECommerce.Orders.API/Features/Orders/OrderDto.cs` / `OrderMapper.cs` / `PaginationHelper.cs` - Read-side DTOs, Mapperly mapper, Orders-local pagination clamp
- `src/services/orders/ECommerce.Orders.API/Program.cs` - Registers OrderMapper in DI (this session's fix); branches MassTransit onto in-memory transport when ConnectionStrings:messaging is the test "placeholder" sentinel
- `src/building-blocks/Tests.Common/Builders/OrderBuilder.cs` - Fluent OrderReadModelData test-data builder mirroring ProductBuilder
- `src/services/orders/ECommerce.Orders.Tests/Integration/OrdersEndpointTests.cs` / `OrdersEndpointSteps.cs` - WireMock-backed WebApplicationFactory suite: empty/populated cart, IDOR 404, pagination/ordering, eventual-consistency polling
- `src/services/orders/ECommerce.Orders.Tests/Integration/OrderReadModelInboxDeduplicationTests.cs` / `Steps.cs` - Forced-redelivery test mirroring Notifications' CatalogSeededInboxDeduplication pattern
- `src/services/orders/ECommerce.Orders.Tests/ECommerce.Orders.Tests.csproj` - Added Tests.Common project reference

## Decisions Made
- `OrderMapper` registered as a DI singleton (stateless Mapperly-generated partial class) rather than instantiated inline in each endpoint, so Minimal API's parameter-source inference resolves it as a service
- `OrdersWebApplicationFactory` points the typed `ICartClient` HttpClient at a per-test `WireMockServer` and removes `DbInitializer` from the test host, following `CatalogWebApplicationFactory`'s established pattern
- Test isolation: `Given_UserHasOrders` and a new `Given_NoOrdersExist` step clear the `OrderReadModels` table before seeding/asserting, since `PostgresFixture`'s container/database is shared across every `[Fact]` in the class (mirrors Catalog's `ProductsEndpointSteps` clear-before-seed convention)
- `Then_ResponseIs200WithPagedResult` now returns the parsed `PagedResult<OrderSummaryDto>` body so a follow-up ordering assertion can reuse it instead of re-reading `HttpContent` (which throws `ObjectDisposedException` on a second read)

## Deviations from Plan

### Auto-fixed Issues

**1. [Rule 1 - Bug] OrderMapper never registered in DI — crashed GET /orders and GET /orders/{id} route registration at host startup**
- **Found during:** Task 2, running the new integration tests for the first time (invisible to `dotnet build`, which only compiles — it does not exercise Minimal API's runtime endpoint-metadata inference)
- **Issue:** `OrdersEndpoints.cs` (Task 1) injects `OrderMapper mapper` into the `GET /orders` and `GET /orders/{id}` handlers, but `Program.cs` never registered `OrderMapper` with the DI container. ASP.NET Core's Minimal API parameter-source inference only recognizes a complex-type parameter as `[FromServices]` when `IServiceProviderIsService` confirms it's registered; otherwise it falls back to inferring an implicit request body. GET requests don't support inferred bodies, so the host threw `InvalidOperationException: Body was inferred but the method does not allow inferred body parameters` for both routes at every startup.
- **Fix:** Added `builder.Services.AddSingleton<OrderMapper>();` in `Program.cs` (Mapperly-generated mappers are stateless, so singleton is safe).
- **Files modified:** `src/services/orders/ECommerce.Orders.API/Program.cs`
- **Verification:** Ran the xunit.v3 self-contained test executable directly before/after — failure count dropped from 7/10 to 2/10 immediately after this fix; both remaining failures were unrelated (see #2, #3 below).
- **Committed in:** `9644667` (Task 2 commit)

**2. [Rule 1 - Bug] Shared PostgresFixture database leaked state between [Fact]s, breaking count-sensitive assertions**
- **Found during:** Task 2, test run after fix #1
- **Issue:** `GetOrders_ForUserWithMultipleOrders_...` and `GetOrders_ForUserWithNoOrders_...` both query `OrderReadModels` for the shared `DefaultUserId`. Because `PostgresFixture`'s Testcontainer/database is shared across every `[Fact]` in the class (an `IClassFixture`, not per-test), orders created by earlier tests in the class (`TestCreateFromCart_*`) leaked into these later count assertions — e.g. an expected count of 5 actually returned 7.
- **Fix:** Added a `Given_NoOrdersExist()` step (clears `OrderReadModels`) called at the start of the "no orders" test, and added the same clear to the start of `Given_UserHasOrders` before seeding — mirroring Catalog's `ProductsEndpointSteps` convention of clearing before seeding for test isolation against a shared fixture.
- **Files modified:** `src/services/orders/ECommerce.Orders.Tests/Integration/OrdersEndpointSteps.cs`, `OrdersEndpointTests.cs`
- **Verification:** Full suite run twice in a row, 10/10 pass both times.
- **Committed in:** `9644667` (Task 2 commit)

**3. [Rule 1 - Bug] Reading HttpContent twice from the same HttpResponseMessage threw ObjectDisposedException**
- **Found during:** Task 2, test run after fix #2
- **Issue:** `GetOrders_ForUserWithMultipleOrders_...` called `Then_ResponseIs200WithPagedResult(response, ...)` and then `Then_ResponseOrdersAreOrderedByCreatedAtDescending(response)`, both of which called `response.Content.ReadFromJsonAsync<...>()` on the same `HttpResponseMessage`. The underlying stream can only be consumed once; the second call threw `ObjectDisposedException: Cannot access a closed Stream`.
- **Fix:** Changed `Then_ResponseIs200WithPagedResult` to return the parsed `PagedResult<OrderSummaryDto>`, and changed `Then_ResponseOrdersAreOrderedByCreatedAtDescending` to accept the already-parsed body instead of re-reading the response.
- **Files modified:** `src/services/orders/ECommerce.Orders.Tests/Integration/OrdersEndpointSteps.cs`, `OrdersEndpointTests.cs`
- **Verification:** Full suite run twice in a row, 10/10 pass both times.
- **Committed in:** `9644667` (Task 2 commit)

---

**Total deviations:** 3 auto-fixed (3 bugs — one in Task 1's already-committed code surfaced by Task 2's tests, two in Task 2's own new test code)
**Impact on plan:** All three fixes were necessary for the integration tests to run and pass at all; no scope creep beyond what the plan's own acceptance criteria (`dotnet test ... passes with 0 failures`) required.

## Issues Encountered
- **Environment: `dotnet test src/services/orders/Orders.sln --configuration Release` fails with a VSTest `testhost.deps.json` package-version mismatch** (`package: 'testhost', version: '18.6.0-release-26270-133' ... not found`), identical to the environment issue documented in Plan 03-02's SUMMARY. Reproduced identically against the pre-existing, already-passing Catalog and Notifications test projects in this same session, confirming it is a repo-wide/sandbox-wide toolchain issue, not caused by this plan's code. **Verification workaround (same as 03-02):** ran the xunit.v3 self-contained native runner directly (`ECommerce.Orders.Tests.exe`), which bypasses the VSTest bridge entirely — confirmed `Total: 10, Errors: 0, Failed: 0, Skipped: 0` twice in a row. `dotnet build src/services/orders/Orders.sln --configuration Release` succeeds cleanly (0 errors), satisfying the plan's build-based acceptance criteria directly.
- Resumed mid-plan after a prior executor session was interrupted by an unrelated authentication/session expiry (not a code or logic failure). Verified Task 1's commit (`4f3bd50`) and the partially-written Task 2 files on disk against the plan's acceptance criteria before treating any of it as done — the uncommitted test files were substantially complete and correctly mirrored the established Notifications/Catalog integration-test patterns, but had never actually been run before this session (hence the three bugs above going undetected).

## User Setup Required

None - no external service configuration required. Docker Desktop must be running locally for the Postgres Testcontainer used by the integration tests (same as Plan 03-02).

## Next Phase Readiness
- Orders' full read-side vertical slice (list, detail, demo-marked cart-to-order trigger) is complete and proven end-to-end. Phase 4's checkout saga will replace `POST /orders/test-create-from-cart` with the real saga-driven `/checkout` flow, reusing `OrderReadModelProjector`, `OrdersDbContext`, and the `OrderCreated`/`OrderStatusChanged` contracts unchanged.
- No blockers. The `dotnet test`/testhost environment quirk should be re-verified in a non-sandboxed CI/dev environment before being treated as resolved project-wide, though it is confirmed (twice now, across two plans) unrelated to this project's code.

---
*Phase: 03-cart-orders-skeleton*
*Completed: 2026-08-04*

## Self-Check: PASSED

All created files verified present on disk; both task commit hashes (4f3bd50, 9644667) verified present in git log.
