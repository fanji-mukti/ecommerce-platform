# Phase 4: Checkout Saga & Payments - Discussion Log

> **Audit trail only.** Do not use as input to planning, research, or execution agents.
> Decisions are captured in CONTEXT.md — this log preserves the alternatives considered.

**Date:** 2026-08-08
**Phase:** 4-Checkout Saga & Payments
**Areas discussed:** Fulfillment-failure demo trigger, Checkout/Orders UX during the saga, Saga timeout duration for the demo, Payment-failure demo affordance

---

## Fulfillment-failure demo trigger

| Option | Description | Selected |
|--------|-------------|----------|
| Demo-only test endpoint | Same pattern as Phase 3's test-create-from-cart: a clearly-marked non-production endpoint that publishes the failure event directly into the saga | ✓ |
| Deterministic magic value | A second magic condition (product/quantity) that deterministically fails "fulfillment" | |
| Claude decides | Pick whichever fits best once research/planning look at the saga shape | |

**User's choice:** Demo-only test endpoint

| Option | Description | Selected |
|--------|-------------|----------|
| API/test-only | No UI affordance — trigger exists for integration tests and manual API calls during demo narration | ✓ |
| Exposed in UI | A "simulate fulfillment failure" button on /checkout | |

**User's choice:** API/test-only

| Option | Description | Selected |
|--------|-------------|----------|
| Any time in Paid state | Callable any time the order is Paid — simplest for demo narration | ✓ |
| Claude decides | Let planning pick based on saga states | |

**User's choice:** Any time in Paid state

---

## Checkout/Orders UX during the saga

| Option | Description | Selected |
|--------|-------------|----------|
| Step indicator | Visual progress stepper lighting up each saga stage | ✓ |
| Status text + spinner | Simpler spinner + plain status label | |
| Claude decides | | |

**User's choice:** Step indicator

| Option | Description | Selected |
|--------|-------------|----------|
| Fixed short interval (~1-2s) | Simple poll every 1-2s until terminal state | ✓ |
| Backoff polling | Start fast, back off over time | |
| Claude decides | | |

**User's choice:** Fixed short interval (~1-2s)

| Option | Description | Selected |
|--------|-------------|----------|
| Auto-redirect to /orders/:id | Navigate automatically on terminal state | ✓ |
| Stay on /checkout, show result inline | Show result without navigating away | |

**User's choice:** Auto-redirect to /orders/:id

| Option | Description | Selected |
|--------|-------------|----------|
| Terminal status only | Just the status field | |
| Status + reason | Status plus human-readable failure reason | ✓ |

**User's choice:** Status + reason

---

## Saga timeout duration for the demo

| Option | Description | Selected |
|--------|-------------|----------|
| Configurable via appsettings/env var | Checkout:TimeoutMinutes, default 15, overridable for demos | ✓ |
| Hardcoded 15 minutes, no override | Matches roadmap literally | |

**User's choice:** Configurable via appsettings/env var

| Option | Description | Selected |
|--------|-------------|----------|
| Integration test with a short test-only timeout | xUnit test injects a short timeout, asserts compensation fires | ✓ |
| Claude decides | | |

**User's choice:** Integration test with a short test-only timeout

---

## Payment-failure demo affordance

| Option | Description | Selected |
|--------|-------------|----------|
| Hint text near cart total / checkout button | Small helper text revealing the .99 trick | ✓ |
| Hidden trick, no UI hint | No in-app hint | |

**User's choice:** Hint text near cart total / checkout button

| Option | Description | Selected |
|--------|-------------|----------|
| No explicit toggle | Rely solely on natural cart total ending in .99 | |
| Add a demo toggle | "Simulate payment failure" checkbox, force-triggers regardless of total | ✓ |

**User's choice:** Add a demo toggle

---

## Claude's Discretion

- Reconciling roadmap's `Started`/`AwaitingPayment`/`Paid` state names with the existing `OrderStateMachine`'s `Pending`/`Paid`/`Fulfilled`/`Cancelled`/`Failed` states (built Phase 3) — extend in place vs. new states, left to research/planning.
- Whether `Checkout.API` holds any state of its own vs. pure façade over the Orders saga.
- Exact `AuthorisePayment`/`RefundPayment` command/event shapes and Payments idempotency-key storage.
- Visual styling of step indicator, hint text placement, demo toggle placement — deferred to `/gsd-ui-phase 4`.

## Deferred Ideas

- Real Fulfillment service logic (timer-based shipping simulation) — Phase 5.
- Real Notifications inbox — Phase 5.
- Real payment provider integration (Stripe) — PAY-V2-01, out of scope for v1.
