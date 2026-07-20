# Phase 3: Cart & Orders Skeleton - Discussion Log

> **Audit trail only.** Do not use as input to planning, research, or execution agents.
> Decisions are captured in CONTEXT.md — this log preserves the alternatives considered.

**Date:** 2026-07-21
**Phase:** 3-Cart & Orders Skeleton
**Areas discussed:** Cart↔Order boundary, Redis cart data model, Orders CQRS/state machine implementation, Angular /cart page UX

---

## Cart↔Order Boundary

| Option | Description | Selected |
|--------|-------------|----------|
| Test-trigger endpoint | Non-production endpoint snapshots cart into a Pending Order, clears cart; Phase 4 replaces it with real saga checkout | ✓ |
| Seeded orders only | No cart→order transition; seed fake orders directly for display | |
| Direct synchronous creation | Cart calls Orders synchronously as a stepping stone toward the async saga | |

**User's choice:** Test-trigger endpoint (recommended option).
**Notes:** None.

---

## Redis Cart Data Model

| Option | Description | Selected |
|--------|-------------|----------|
| `cart:{userId}`, no TTL | Simple key, persists until explicitly cleared | ✓ |
| `cart:{userId}` with sliding TTL | 7-day sliding expiry refreshed on writes | |

**User's choice:** No TTL (recommended option).

| Option | Description | Selected |
|--------|-------------|----------|
| Synchronous HTTP call to Catalog | Cart.API calls Catalog internally to fetch and snapshot price on add | ✓ |
| Angular sends price with request | Client supplies price; less secure/realistic | |

**User's choice:** Synchronous HTTP call to Catalog (recommended option).

---

## Orders CQRS/State Machine Implementation

| Option | Description | Selected |
|--------|-------------|----------|
| Same DB, separate tables | One OrdersDB, write table + read-model table | ✓ |
| Fully separate read DB | Second DB/Redis purely for projections | |

**User's choice:** Same DB, separate tables (recommended option).

| Option | Description | Selected |
|--------|-------------|----------|
| Async via MassTransit domain events | Outbox/inbox pattern consistent with Phase 2 | ✓ |
| Inline synchronous update | Same-transaction read-model update | |

**User's choice:** Async via MassTransit domain events (recommended option).

---

## Angular /cart Page UX

| Option | Description | Selected |
|--------|-------------|----------|
| Debounced PATCH | Immediate local UI update, debounced (~500ms) network call | ✓ |
| Immediate PATCH per click | Network call on every click | |

**User's choice:** Debounced PATCH (recommended option).

| Option | Description | Selected |
|--------|-------------|----------|
| Claude's discretion | mat-card line items, mat-icon-button steppers, summary panel, empty state | ✓ |
| User has specifics in mind | — | |

**User's choice:** Claude's discretion (recommended option).

---

## Claude's Discretion

- Catalog product schema already fixed from Phase 2; Order entity fields beyond core set (OrderId, UserId, LineItems, Status, Timestamps).
- Read-model projection consumer placement (in-process vs. separate consumer class).
- Cart clear semantics (full Redis key delete vs. empty line-item list).
- `/cart` page visual details beyond noted Material components.

## Deferred Ideas

- Real checkout-triggered cart clear via `OrderPaid` event — Phase 4.
- Order history list page (FE-V2-01) — already flagged V2 in STATE.md; Phase 3 planning to confirm minimal orders view need.
- Sliding TTL / cart abandonment cleanup — out of scope for portfolio demo.
- Separate read-model database for Orders CQRS — not needed at this scale.
