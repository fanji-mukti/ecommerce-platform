---
phase: 04-checkout-saga-payments
plan: 01
subsystem: api
tags: [contracts, masstransit, aspire, yarp, dotnet10]

# Dependency graph
requires:
  - phase: 03-cart-orders-skeleton
    provides: OrderStateMachine, OrderCreated/OrderStatusChanged contracts, Orders.API, AppHost/Gateway topology
provides:
  - Checkout, Payments, and Fulfillment message contracts (StartCheckout, AuthorisePayment, RefundPayment, PaymentAuthorised, PaymentFailed, PaymentRefunded, FulfillmentFailed)
  - Extended OrderCreated (SimulatePaymentFailure) and OrderStatusChanged (FailureReason) contracts
  - AppHost wiring for checkout (post-orders, references orders+serviceBus) and payments (references postgres)
  - Gateway checkout-route/cluster for public /api/checkout/** ingress
affects: [04-02-orders-saga, 04-03-checkout-facade, 04-04-payments-service, 04-05-angular-checkout]

# Tech tracking
tech-stack:
  added: []
  patterns: [interface-first contract authoring, IMessageEnvelope record convention, trailing-optional-parameter contract extension for backward compatibility]

key-files:
  created:
    - src/building-blocks/Contracts/Checkout/Commands/V1/StartCheckout.cs
    - src/building-blocks/Contracts/Payments/Commands/V1/AuthorisePayment.cs
    - src/building-blocks/Contracts/Payments/Commands/V1/RefundPayment.cs
    - src/building-blocks/Contracts/Payments/Events/V1/PaymentAuthorised.cs
    - src/building-blocks/Contracts/Payments/Events/V1/PaymentFailed.cs
    - src/building-blocks/Contracts/Payments/Events/V1/PaymentRefunded.cs
    - src/building-blocks/Contracts/Fulfillment/Events/V1/FulfillmentFailed.cs
  modified:
    - src/building-blocks/Contracts/Orders/Events/V1/OrderCreated.cs
    - src/building-blocks/Contracts/Orders/Events/V1/OrderStatusChanged.cs
    - src/ecommerce.AppHost/Program.cs
    - src/services/gateway/ECommerce.Gateway.API/appsettings.json

key-decisions:
  - "Checkout.API declared after orders in AppHost (was before) so it can reference the orders variable for its internal HTTP client wiring"
  - "Checkout.API keeps no postgres reference — thin façade with no own database this phase"
  - "Payments.API gets its first postgres reference + WaitFor(postgres), mirroring the catalog project's pattern"
  - "Gateway routes /api/checkout/** publicly; payments and fulfillment remain internal-only (no gateway routes) per RESEARCH.md"

patterns-established:
  - "Contract extension via trailing optional parameters preserves existing named-argument call sites when adding fields to shipped records"

requirements-completed: [CHK-01, PAY-01]

duration: 25min
completed: 2026-08-08
---

# Phase 4 Plan 01: Contracts & Topology Wiring Summary

**Authored all Phase 4 cross-service message contracts (Checkout/Payments/Fulfillment) and wired AppHost/Gateway so Checkout.API and Payments.API are reachable and provisioned before any consumer/producer logic exists.**

## Performance

- **Duration:** 25 min
- **Tasks:** 2
- **Files modified:** 13

## Accomplishments
- Defined 7 new `IMessageEnvelope` contract records across Checkout/Payments/Fulfillment areas (StartCheckout, AuthorisePayment, RefundPayment, PaymentAuthorised, PaymentFailed, PaymentRefunded, FulfillmentFailed), deleting the 4 superseded placeholder files they replace
- Extended `OrderCreated` (+`SimulatePaymentFailure`) and `OrderStatusChanged` (+`FailureReason`) with trailing optional parameters, preserving every existing named-argument call site
- Reordered and rewired `ecommerce.AppHost/Program.cs`: `checkout` now declared after `orders` and references `orders` + `serviceBus` (no postgres); `payments` gains its first `postgres` reference; `gateway` references `checkout`
- Added `checkout-route`/`checkout` cluster to Gateway `appsettings.json`, exposing `/api/checkout/**` while leaving `payments`/`fulfillment` internal-only

## Task Commits

1. **Task 1: Author Phase 4 message contracts, extend OrderCreated/OrderStatusChanged** - `d9699d5` (feat)
2. **Task 2: Wire AppHost and Gateway for Checkout/Payments** - `93b0565` (feat)

**Plan metadata:** committed by orchestrator after wave merge (worktree mode — STATE.md/ROADMAP.md not touched by this agent)

## Files Created/Modified
- `src/building-blocks/Contracts/Checkout/Commands/V1/StartCheckout.cs` - JSON request body Checkout.API POSTs to Orders.API's `POST /orders/checkout`
- `src/building-blocks/Contracts/Payments/Commands/V1/AuthorisePayment.cs` - Published by Orders saga, consumed by Payments
- `src/building-blocks/Contracts/Payments/Commands/V1/RefundPayment.cs` - Published by Orders saga on CHK-04 fulfillment-failure compensation path
- `src/building-blocks/Contracts/Payments/Events/V1/PaymentAuthorised.cs` - Published by Payments on successful simulated authorisation
- `src/building-blocks/Contracts/Payments/Events/V1/PaymentFailed.cs` - Published by Payments on deterministic/forced failure (locked reason string `"Payment declined"`)
- `src/building-blocks/Contracts/Payments/Events/V1/PaymentRefunded.cs` - Published by Payments on refund completion
- `src/building-blocks/Contracts/Fulfillment/Events/V1/FulfillmentFailed.cs` - Published by Checkout.API's demo-only trigger endpoint (plan 04-05), consumed by Orders saga
- `src/building-blocks/Contracts/Orders/Events/V1/OrderCreated.cs` - Added `SimulatePaymentFailure` trailing optional field
- `src/building-blocks/Contracts/Orders/Events/V1/OrderStatusChanged.cs` - Added `FailureReason` trailing optional field
- `src/ecommerce.AppHost/Program.cs` - Reordered `checkout` after `orders`; rewired references for `checkout`, `payments`, `gateway`
- `src/services/gateway/ECommerce.Gateway.API/appsettings.json` - Added `checkout-route`/`checkout` cluster

## Decisions Made
- None beyond what the plan specified — plan's exact contract shapes, AppHost reordering, and Gateway route shape were followed verbatim.

## Deviations from Plan

None - plan executed exactly as written.

## Issues Encountered
None.

## User Setup Required

None - no external service configuration required.

## Next Phase Readiness
- `Contracts.csproj`, `Orders.sln`, and `ecommerce.AppHost.sln` all build with zero errors — every downstream Phase 4 plan (Orders saga extension in 04-02, Checkout façade in 04-03, Payments service in 04-04, Angular UI in 04-05) can now reference these contracts and the wired topology without further infrastructure edits.
- No blockers.

---
*Phase: 04-checkout-saga-payments*
*Completed: 2026-08-08*

## Self-Check: PASSED

All 7 created contract files verified present on disk; SUMMARY.md verified present; all 3 task/summary commits (`d9699d5`, `93b0565`, `f3ee157`) verified present in `git log`.
