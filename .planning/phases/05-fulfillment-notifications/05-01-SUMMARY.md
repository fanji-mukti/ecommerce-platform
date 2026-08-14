---
phase: 05-fulfillment-notifications
plan: 01
subsystem: api
tags: [contracts, masstransit, dotnet, event-driven, message-envelope]

# Dependency graph
requires:
  - phase: 04-checkout-saga-payments
    provides: IMessageEnvelope pattern, existing Orders/Payments/Fulfillment contract shapes
provides:
  - "New Fulfillment-context event OrderShipped (FUL-02 signal)"
  - "UserId field (D-03) added to OrderStatusChanged, PaymentFailed, AuthorisePayment"
affects: [05-02, 05-03, 05-04, 05-05, 05-06, 05-07, 05-08]

# Tech tracking
tech-stack:
  added: []
  patterns:
    - "IMessageEnvelope-implementing positional C# records, envelope fields first (MessageId, CorrelationId, CausationId, OccurredAt) then domain fields"
    - "UserId (D-03) carried directly on cross-service messages to avoid synchronous lookups for notification scoping"

key-files:
  created:
    - src/building-blocks/Contracts/Fulfillment/Events/V1/OrderShipped.cs
  modified:
    - src/building-blocks/Contracts/Orders/Events/V1/OrderStatusChanged.cs
    - src/building-blocks/Contracts/Payments/Events/V1/PaymentFailed.cs
    - src/building-blocks/Contracts/Payments/Commands/V1/AuthorisePayment.cs

key-decisions:
  - "OrderShipped uses CheckoutId (not OrderId) per ADR-0007 Fulfillment-context vocabulary, matching FulfillmentFailed's existing naming"
  - "UserId inserted immediately after each record's correlating id field (OrderId/CheckoutId), preserving trailing default parameters at the end"
  - "PaymentAuthorised intentionally left unchanged — Notifications only consumes OrderStatusChanged, OrderShipped, PaymentFailed per D-03"

patterns-established:
  - "Pattern 1: New Fulfillment events follow FulfillmentFailed.cs's exact envelope/record shape as the copy-source analog"
  - "Pattern 2: D-03 UserId propagation — populated server-side from JWT claim at message-construction time, never from client input"

requirements-completed: [FUL-01, FUL-02, NOT-02]

# Metrics
duration: 8min
completed: 2026-08-14
---

# Phase 05 Plan 01: Message Contracts Foundation Summary

**Added OrderShipped (FUL-02) Fulfillment event and propagated UserId (D-03) into OrderStatusChanged, PaymentFailed, and AuthorisePayment — the interface-first foundation every downstream Phase 5 plan builds against.**

## Performance

- **Duration:** 8 min
- **Started:** 2026-08-14T13:20:00Z
- **Completed:** 2026-08-14T13:28:00Z
- **Tasks:** 2 completed
- **Files modified:** 4 (1 created, 3 modified)

## Accomplishments
- Created `OrderShipped` event record implementing `IMessageEnvelope`, following the `FulfillmentFailed.cs` analog exactly, carrying `CheckoutId` (Fulfillment vocabulary) and `UserId` (D-03)
- Added `UserId` to `OrderStatusChanged` (after `OrderId`), `PaymentFailed` (after `CheckoutId`), and `AuthorisePayment` (after `CheckoutId`) — positional records, so every downstream call site must now pass `UserId:` explicitly (compile-time enforced, by design)
- Left `PaymentAuthorised` unchanged since Notifications never consumes it (per D-03 and RESEARCH.md Anti-Patterns)
- `Contracts.csproj` builds standalone with zero errors after both tasks

## Task Commits

Each task was committed atomically:

1. **Task 1: Create the OrderShipped contract** - `159f603` (feat)
2. **Task 2: Add UserId (D-03) to OrderStatusChanged, PaymentFailed, AuthorisePayment** - `137a4be` (feat)

_Note: Worktree mode — final plan-metadata commit (SUMMARY.md) is committed separately by this agent; STATE.md/ROADMAP.md are updated centrally by the orchestrator after merge._

## Files Created/Modified
- `src/building-blocks/Contracts/Fulfillment/Events/V1/OrderShipped.cs` - New FUL-02 event, implements IMessageEnvelope, carries CheckoutId + UserId + ShippedAt
- `src/building-blocks/Contracts/Orders/Events/V1/OrderStatusChanged.cs` - Added `string UserId` after `Guid OrderId`
- `src/building-blocks/Contracts/Payments/Events/V1/PaymentFailed.cs` - Added `string UserId` after `Guid CheckoutId`
- `src/building-blocks/Contracts/Payments/Commands/V1/AuthorisePayment.cs` - Added `string UserId` after `Guid CheckoutId`

## Decisions Made
- Followed the plan's exact field-insertion positions and naming (CheckoutId not OrderId in Fulfillment context) — no deviation from documented shapes.

## Deviations from Plan

None - plan executed exactly as written.

## Issues Encountered

None. As documented in the plan, `Orders.sln` and `Payments.sln` are now expected to fail to build (existing named-argument call sites don't yet pass `UserId:`) — this is intentional per the plan and is NOT fixed in this plan; plan 05-03 (Wave 2) addresses every call site. Only `Contracts.csproj` was built/verified per this plan's `<verify>` scope.

## User Setup Required

None - no external service configuration required.

## Next Phase Readiness
- All Phase 5 message contracts now exist, compile in isolation (`Contracts.csproj`), and follow the established envelope/namespace convention.
- Downstream plans (05-02 through 05-08) — Orders saga wiring, Payments consumer, Fulfillment service, Notifications consumers — can implement against these shapes without renegotiating a contract.
- Known/expected blocker for Wave 2: `Orders.sln` and `Payments.sln` will not compile until 05-03 updates call sites in `OrderStateMachine.cs`, `AuthorisePaymentConsumer.cs`, and their test files to pass `UserId:`.

---
*Phase: 05-fulfillment-notifications*
*Completed: 2026-08-14*
