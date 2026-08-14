# Phase 5: Fulfillment & Notifications - Discussion Log

> **Audit trail only.** Do not use as input to planning, research, or execution agents.
> Decisions are captured in CONTEXT.md — this log preserves the alternatives considered.

**Date:** 2026-08-14
**Phase:** 5-Fulfillment & Notifications
**Areas discussed:** Fulfillment timer & failure trigger, Notification content & user scoping, Notifications UI surface, Order detail visibility during fulfillment

---

## Fulfillment timer & failure trigger

| Option | Description | Selected |
|--------|-------------|----------|
| Configurable, default ~30s-1min | Mirrors Phase 4's `Checkout:TimeoutMinutes` pattern; reuses MassTransit.Quartz scheduling already wired in Orders | ✓ |
| Fixed short delay, hardcoded | Simpler, no config surface | |
| Near-instant (no real delay) | Loses the "timer-based simulation" demo value | |

**User's choice:** Configurable, default ~30s-1min

| Option | Description | Selected |
|--------|-------------|----------|
| Always succeeds; keep Phase 4's manual demo endpoint only | FUL-01/02 only describe the happy path; the manual demo endpoint stays the only compensation trigger | ✓ |
| Add a deterministic failure trigger | PAY-02-style condition; adds scope beyond FUL-01/02 as written | |

**User's choice:** Always succeeds; keep Phase 4's manual demo endpoint only

---

## Notification content & user scoping

| Option | Description | Selected |
|--------|-------------|----------|
| Add UserId to the relevant event contracts | Keeps Notifications fully event-driven, no synchronous coupling | ✓ |
| Notifications does a synchronous lookup call to Orders | Adds a runtime dependency, works against event-driven premise | |

**User's choice:** Add UserId to the relevant event contracts

| Option | Description | Selected |
|--------|-------------|----------|
| Short templated message + order link | e.g. "Your order has shipped" + OrderId, links to /orders/:id | ✓ |
| Raw event type + timestamp only | Reads as a debug log | |

**User's choice:** Short templated message + order link

---

## Notifications UI surface

| Option | Description | Selected |
|--------|-------------|----------|
| Add a minimal Angular /notifications page | Makes NOT-01 demoable end-to-end, not just via curl/Swagger | ✓ |
| Backend-API-only this phase | Matches the absence of a UI-hint flag in ROADMAP.md | |

**User's choice:** Add a minimal Angular /notifications page

| Option | Description | Selected |
|--------|-------------|----------|
| Add a nav bar link (no badge) | Consistent with existing nav; no unread-count state to track | ✓ |
| Direct URL only, no nav entry | Less discoverable during a live demo | |

**User's choice:** Add a nav bar link (no badge)

---

## Order detail visibility during fulfillment

| Option | Description | Selected |
|--------|-------------|----------|
| Show a "Shipping..." indicator during the wait | Client-side inference from status==Paid; matches SC1 visibility language | ✓ |
| No intermediate UI — just Paid then Fulfilled | Simpler, less visual payoff | |

**User's choice:** Show a "Shipping..." indicator during the wait

| Option | Description | Selected |
|--------|-------------|----------|
| Reuse Phase 4's polling, extend stop-condition to Fulfilled/Cancelled/Failed | Consistent mechanism, no new UX pattern | ✓ |
| Manual refresh only on /orders/:id | Breaks the "watch it happen live" demo value | |

**User's choice:** Reuse Phase 4's polling, extend stop-condition to Fulfilled/Cancelled/Failed

---

## Claude's Discretion

- Exact contract/event name and shape for the "order paid" signal Fulfillment consumes (new `OrderPaid` vs. filtering existing `OrderStatusChanged`) — contract-design/topic-wiring call for research and planning.
- How the Orders saga is wired to react to `OrderShipped` — mechanical, follows the existing `FulfillmentFailedEvent` binding pattern.
- Whether Fulfillment needs its own persisted state/DB for redelivery-safe timer idempotency — technical/research call.
- Exact `Fulfillment:ProcessingSeconds` config key name and default value.
- Notifications inbox message copy wording beyond the three working examples.
- Exact visual treatment of the "Shipping..." indicator — deferred to a `/gsd-ui-phase 5` pass.

## Deferred Ideas

- Deterministic natural failure trigger for Fulfillment — explicitly rejected for this phase.
- Mark notifications as read (NOT-V2-01) — V2.
- Real email/SMS delivery (NOT-V2-02) — V2.
- Unread-count badge on the notifications nav link — deferred, needs read/unread state this phase doesn't track.

---

*Phase: 5-Fulfillment & Notifications*
*Discussion log recorded: 2026-08-14*
