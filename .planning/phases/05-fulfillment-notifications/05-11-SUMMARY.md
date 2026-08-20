---
phase: 05-fulfillment-notifications
plan: 11
subsystem: ui
tags: [angular, rxjs, polling, resilience, order-detail]

# Dependency graph
requires:
  - phase: 05-fulfillment-notifications
    provides: order-detail.component.ts polling loop (05-08/prior plans) that this plan hardens against transient errors
provides:
  - Transient-error-resilient polling in OrderDetailComponent.startPolling — a per-tick HTTP error no longer terminates the interval subscription
affects: []

# Tech tracking
tech-stack:
  added: []
  patterns:
    - "RxJS switchMap inner-observable error isolation via catchError(() => EMPTY) to keep an outer interval() subscription alive across per-tick failures"

key-files:
  created: []
  modified:
    - src/frontend/ecommerce-app/src/app/features/orders/order-detail/order-detail.component.ts
    - src/frontend/ecommerce-app/src/app/features/orders/order-detail/order-detail.component.spec.ts

key-decisions:
  - "Scoped the fix entirely to the inner switchMap callback (catchError(() => EMPTY)) rather than touching takeWhile/takeUntilDestroyed/the outer subscribe block, per the plan's explicit scope boundary"

patterns-established:
  - "Per-tick error isolation in polling loops: wrap the inner request in .pipe(catchError(() => EMPTY)) inside switchMap so failed ticks are silently skipped (no state mutation) while the outer interval keeps ticking"

requirements-completed: [FUL-02]

# Metrics
duration: 12min
completed: 2026-08-20
---

# Phase 05 Plan 11: Order-Detail Polling Error Recovery Summary

**Transient HTTP errors during /orders/:id's 1.5s polling loop now self-heal on the next tick via `catchError(() => EMPTY)` instead of silently and permanently killing live status updates.**

## Performance

- **Duration:** 12 min
- **Started:** 2026-08-20T15:41:00Z
- **Completed:** 2026-08-20T15:53:21Z
- **Tasks:** 1
- **Files modified:** 2

## Accomplishments
- Closed WR-03: a single transient HTTP error (network blip, transient 5xx) on any `/orders/:id` poll tick no longer terminates the `interval(1500)` subscription and stops live status polling for the rest of the component's lifetime
- Added a regression test proving the polling loop survives a transient error and still reaches/stops correctly at a terminal status on the following tick
- Verified the existing terminal-stop, destroy-cleanup, and non-navigation behaviors are unaffected (all 10 tests in the spec file pass)

## Task Commits

Each task was committed atomically:

1. **Task 1: Recover from transient polling errors + regression test** - `b28c38d` (fix)

**Plan metadata:** commit deferred to phase orchestrator (worktree mode — STATE.md/ROADMAP.md updates owned by orchestrator after wave merge)

## Files Created/Modified
- `src/frontend/ecommerce-app/src/app/features/orders/order-detail/order-detail.component.ts` - `startPolling`'s inner `switchMap` callback now wraps `this.ordersService.getOrder(id)` in `.pipe(catchError(() => EMPTY))`; imports `EMPTY` from `rxjs` and `catchError` from `rxjs/operators`
- `src/frontend/ecommerce-app/src/app/features/orders/order-detail/order-detail.component.spec.ts` - added "recovers from a transient poll error and still reaches a terminal status on the next tick" test, following the existing `vi.useFakeTimers()` + `httpMock.expectOne(...).flush(...)` + `vi.advanceTimersByTimeAsync(1500)` pattern

## Decisions Made
- Scoped the fix entirely to the inner `switchMap` callback (`catchError(() => EMPTY)`), leaving `takeWhile`, `takeUntilDestroyed`, and the outer `.subscribe({...})` block untouched — matches the plan's explicit scope boundary and keeps the change minimal and low-risk

## Deviations from Plan

None - plan executed exactly as written.

## Issues Encountered
- The worktree had no `node_modules` installed (git worktrees don't share `node_modules` with the main checkout and it's gitignored). Ran `npm ci` inside `src/frontend/ecommerce-app` to install dependencies before running tests — this is local, gitignored tooling setup, not a repo change, so no commit was needed for it.

## User Setup Required

None - no external service configuration required.

## Next Phase Readiness
- WR-03 (transient polling error resilience) is closed. No blockers for downstream work.
- This was a small, targeted, self-contained fix per the gap-closure brief; no new UI element or copy was introduced, so 05-UI-SPEC.md's pending checker sign-off is unaffected.

---
*Phase: 05-fulfillment-notifications*
*Completed: 2026-08-20*

## Self-Check: PASSED

- FOUND: src/frontend/ecommerce-app/src/app/features/orders/order-detail/order-detail.component.ts
- FOUND: src/frontend/ecommerce-app/src/app/features/orders/order-detail/order-detail.component.spec.ts
- FOUND: .planning/phases/05-fulfillment-notifications/05-11-SUMMARY.md
- FOUND commit: b28c38d
- FOUND commit: 58785c9
