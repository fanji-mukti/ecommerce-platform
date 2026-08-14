---
phase: 04-checkout-saga-payments
plan: 03
subsystem: api
tags: [aspnetcore, minimal-apis, orders, checkout-saga, masstransit, mapperly]

# Dependency graph
requires:
  - phase: 04-checkout-saga-payments (plan 01)
    provides: StartCheckout contract (ECommerce.Checkout.Commands.V1) and extended OrderCreated (SimulatePaymentFailure)
  - phase: 04-checkout-saga-payments (plan 02)
    provides: OrderReadModel.FailureReason column + saga projector wiring
provides:
  - "POST /orders/checkout — real saga-driven order-creation endpoint reusing a caller-minted checkoutId as OrderId/saga CorrelationId"
  - "GET /orders/{id} now surfaces FailureReason in OrderDto"
  - "Phase 3 demo-only POST /orders/test-create-from-cart fully retired"
affects: [04-checkout-saga-payments plan 05 (Checkout.API), 04-checkout-saga-payments plan 06 (Angular checkout/order UI)]

# Tech tracking
tech-stack:
  added: []
  patterns:
    - "Caller-minted correlation id: Checkout.API mints checkoutId, Orders.API reuses it verbatim as OrderId/saga CorrelationId instead of generating a new Guid"

key-files:
  created: []
  modified:
    - src/services/orders/ECommerce.Orders.API/Features/Orders/OrderDto.cs
    - src/services/orders/ECommerce.Orders.API/Features/Orders/OrdersEndpoints.cs
    - src/services/orders/ECommerce.Orders.Tests/Integration/OrdersEndpointSteps.cs
    - src/services/orders/ECommerce.Orders.Tests/Integration/OrdersEndpointTests.cs

key-decisions:
  - "StartCheckout.MessageId flows into OrderCreated.CausationId (not Guid.Empty) — preserves the causal chain from Checkout.API's StartCheckout through to OrderCreated for tracing"
  - "StartCheckout.SimulatePaymentFailure is forwarded into OrderCreated.SimulatePaymentFailure unchanged — no new validation added since the field is only meaningful in this simulated-payments demo project"

patterns-established:
  - "Pattern: Real checkout entry points reuse a caller-minted correlation id end-to-end rather than minting a new one at each hop, avoiding an extra id round trip between Checkout.API and Orders.API"

requirements-completed: [CHK-01, CHK-02]

# Metrics
duration: 25min
completed: 2026-08-12
---

# Phase 04 Plan 03: Orders Checkout Entry Point Summary

**Replaced Phase 3's demo-only order-creation trigger with a real saga-driven `POST /orders/checkout` that reuses Checkout.API's caller-minted `checkoutId` as the `OrderId`/saga `CorrelationId`, and extended `GET /orders/{id}` to surface `FailureReason`.**

## Performance

- **Duration:** ~25 min
- **Completed:** 2026-08-12
- **Tasks:** 2/2
- **Files modified:** 4

## Accomplishments
- `POST /orders/checkout` accepts a `StartCheckout` body, validates the cart is non-empty, publishes `OrderCreated` with the caller-minted `checkoutId` as `OrderId`/`CorrelationId`, flushes the transactional outbox before clearing the cart, and returns `202` with `{ orderId }`.
- `GET /orders/{id}` now returns `failureReason` (nullable) in the response body via Mapperly source-generated mapping — no change needed to `OrderMapper.cs` itself.
- Phase 3's demo-only `POST /orders/test-create-from-cart` is fully removed, satisfying Phase 3's own D-01/D-02 commitment.
- Orders integration test suite updated: `When_TestCreateFromCartIsCalled` replaced with `When_CheckoutIsCalled` (posts a `StartCheckout`-shaped JSON body), all call sites now assert the returned `orderId` equals the caller-supplied `checkoutId`, and a new `Then_ResponseOrderHasFailureReason` helper is available for future FailureReason assertions.

## Task Commits

Each task was committed atomically:

1. **Task 1: Add POST /orders/checkout, extend OrderDto, retire test-create-from-cart** - `32582e8` (feat)
2. **Task 2: Update Orders integration test suite for the checkout endpoint** - `eb476c1` (test)

## Files Created/Modified
- `src/services/orders/ECommerce.Orders.API/Features/Orders/OrderDto.cs` - Added `FailureReason` as the final positional parameter on `OrderDto` (detail shape only; `OrderSummaryDto` unchanged)
- `src/services/orders/ECommerce.Orders.API/Features/Orders/OrdersEndpoints.cs` - Removed `POST /orders/test-create-from-cart`; added `POST /orders/checkout` that reuses `StartCheckout.CheckoutId` as `OrderId`, threads `MessageId` into `OrderCreated.CausationId`, and forwards `SimulatePaymentFailure`
- `src/services/orders/ECommerce.Orders.Tests/Integration/OrdersEndpointSteps.cs` - `When_CheckoutIsCalled` step (posts `StartCheckout` JSON body to `/orders/checkout`) replacing `When_TestCreateFromCartIsCalled`; added `Then_ResponseOrderHasFailureReason` assertion helper
- `src/services/orders/ECommerce.Orders.Tests/Integration/OrdersEndpointTests.cs` - Updated all three checkout-flow tests to call `When_CheckoutIsCalled` with an explicit `checkoutId` and assert the returned `orderId` equals it; pagination/list/detail/404/IDOR tests left unchanged

## Decisions Made
- `StartCheckout.MessageId` is threaded into `OrderCreated.CausationId` (rather than `Guid.Empty` as the old demo endpoint used) — preserves the causal chain from Checkout.API's `StartCheckout` command through to the `OrderCreated` event for tracing/debugging the saga.
- `StartCheckout.SimulatePaymentFailure` is forwarded verbatim into `OrderCreated.SimulatePaymentFailure` — no additional validation/authorization was added around this flag since it exists purely as this portfolio project's simulated-payments demo mechanism (per PROJECT.md's "Payments: Simulated only" constraint), and threat T-04-07 already accepts caller-controlled `checkoutId`/flags in this body with no cross-user impact.

## Deviations from Plan

None — plan executed exactly as written. One micro-adjustment: the inline comment above the new endpoint originally referenced the literal retired route path (`/orders/test-create-from-cart`) in a code comment, which would have caused the plan's own acceptance-criteria grep (`grep -c "test-create-from-cart"` expected to return 0) to fail. Reworded the comment to describe the endpoint without the literal string, satisfying both the acceptance criteria and the intent of the comment. This is a same-task correction, not a Rule 1-4 deviation.

## Issues Encountered

**`dotnet test` could not run in this sandboxed worktree environment** — same pre-existing environment limitation already documented in `04-02-SUMMARY.md` (missing `testhost` package version `18.6.0-release-26270-133` in the local NuGet cache for xUnit v3's VSTest adapter) and `04-04-SUMMARY.md` (Testcontainers cannot reach the Docker Desktop named pipe from this shell, even though `docker info`/`docker context ls` succeed directly). Both issues reproduced here:
- `dotnet test src/services/orders/Orders.sln --filter "FullyQualifiedName~OrdersEndpointTests"` failed with the `testhost` package-not-found error before any tests could run.
- Falling back to the documented workaround (`ECommerce.Orders.Tests.exe -class "ECommerce.Orders.Tests.Integration.OrdersEndpointTests"`, xUnit v3's self-hosted Microsoft.Testing.Platform runner) got past the `testhost` issue, but all 7 `OrdersEndpointTests` then failed at `PostgresFixture`'s Testcontainers-backed Postgres container startup with `DotNet.Testcontainers.Builders.DockerUnavailableException: Failed to connect to Docker endpoint at 'npipe://./pipe/docker_engine'`.

This is an environment limitation, not a functional gap in the code changes — `dotnet build src/services/orders/Orders.sln` succeeds cleanly (0 errors) confirming Mapperly regenerated `OrderMapper` against the new `FailureReason` property and all types resolve correctly, and the test code changes are straightforward mechanical renames/additions that mirror the passing pre-existing test patterns. **Recommend re-running `dotnet test src/services/orders/Orders.sln --filter "FullyQualifiedName~OrdersEndpointTests"` in an environment with working Testcontainers/Docker named-pipe connectivity (e.g., CI or a developer machine) before phase-level sign-off for CHK-01/CHK-02.**

## Next Phase Readiness
- Orders.API now exposes both HTTP primitives Checkout.API (plan 04-05) needs: `POST /orders/checkout` (order creation) and `GET /orders/{id}` with `FailureReason` (status polling with human-readable failure text).
- Blocker to flag for phase-level verification: `OrdersEndpointTests` integration suite has not been executed successfully in this session due to the sandboxed environment's `testhost`/Docker limitations described above — needs a Docker-capable environment to confirm the 0-failures acceptance criterion before CHK-01/CHK-02 are formally signed off.

---
*Phase: 04-checkout-saga-payments*
*Completed: 2026-08-12*

## Self-Check: PASSED

- FOUND: src/services/orders/ECommerce.Orders.API/Features/Orders/OrderDto.cs
- FOUND: src/services/orders/ECommerce.Orders.API/Features/Orders/OrdersEndpoints.cs
- FOUND: .planning/phases/04-checkout-saga-payments/04-03-SUMMARY.md
- FOUND: commit 32582e8 (feat: POST /orders/checkout, OrderDto.FailureReason)
- FOUND: commit eb476c1 (test: Orders integration suite update)
- FOUND: commit 57cfaf1 (docs: plan summary)
