---
phase: 05-fulfillment-notifications
plan: 02
subsystem: ui
tags: [angular, rxjs, signals, polling, angular-material]

# Dependency graph
requires:
  - phase: 04-checkout-saga-payments
    provides: checkout-page.component.ts's interval(1500)+switchMap+takeWhile+takeUntilDestroyed polling pattern, reused structurally here
provides:
  - "OrderDetailComponent isShipping computed signal + 'Preparing your shipment…' indicator while status === 'Paid'"
  - "OrderDetailComponent self-stopping interval(1500) polling loop against GET /api/orders/{id}, terminal at Fulfilled/Cancelled/Failed"
affects: [05-03, 05-04, 05-07, 05-08]

# Tech tracking
tech-stack:
  added: []
  patterns:
    - "Per-page terminal-status set: ORDER_DETAIL_TERMINAL_STATUSES (['Fulfilled','Cancelled','Failed']) is intentionally distinct from checkout-page's TERMINAL_STATUSES (which also includes 'Paid') — do not share/import across files, each page polls to its own terminal condition"
    - "In-place polling (no router.navigate on terminal) vs. checkout's redirect-on-terminal — /orders/:id is already the destination page"

key-files:
  created: []
  modified:
    - src/frontend/ecommerce-app/src/app/features/orders/order-detail/order-detail.component.ts
    - src/frontend/ecommerce-app/src/app/features/orders/order-detail/order-detail.component.html
    - src/frontend/ecommerce-app/src/app/features/orders/order-detail/order-detail.component.scss
    - src/frontend/ecommerce-app/src/app/features/orders/order-detail/order-detail.component.spec.ts

key-decisions:
  - "Polling-tick errors are swallowed (no hasError/notFound mutation) so a single transient failure doesn't blank a page the user is actively viewing — only the initial fetch error sets notFound, per plan's <behavior> spec"

patterns-established:
  - "Structural copy of an existing polling implementation (checkout-page) into a second component with a deliberately different terminal-status set and no-navigate-on-terminal behavior"

requirements-completed: [FUL-02]

# Metrics
duration: ~15min
completed: 2026-08-14
---

# Phase 5 Plan 2: Shipping Indicator + Order-Detail Polling Summary

**Client-side-only "Preparing your shipment…" indicator and self-stopping 1.5s polling loop added to OrderDetailComponent, structurally copied from checkout-page's polling pattern but with a distinct terminal-status set (excludes 'Paid') and no redirect-on-terminal.**

## Performance

- **Duration:** ~15 min
- **Completed:** 2026-08-14T14:53:10Z
- **Tasks:** 1 (TDD: RED + GREEN)
- **Files modified:** 4 (3 plan-scoped + 1 test file)

## Accomplishments
- `isShipping` computed signal (`order()?.status === 'Paid'`) drives a `mat-spinner diameter="18"` + "Preparing your shipment…" indicator on `/orders/:id`
- Net-new `startPolling(id)` method: `interval(1500)` → `switchMap` to `ordersService.getOrder(id)` → updates the `order` signal every tick, `takeWhile` stops the loop (inclusive) once status reaches `Fulfilled`/`Cancelled`/`Failed`
- Polling torn down on component destroy via `takeUntilDestroyed(this.destroyRef)`, preventing duplicate loops on navigate-away-and-back
- Polling-tick errors are silently swallowed — only the initial fetch error sets `notFound`, so a page the user is actively viewing never blanks on a transient failure
- Added 5 new spec cases (indicator visibility for Paid vs. Cancelled, polling-driven update to Fulfilled with indicator clearing, teardown-on-destroy, no-navigate-on-terminal) — all 9 tests in the file pass, all 23 tests in the frontend suite pass

## Task Commits

TDD task executed as RED → GREEN (no REFACTOR needed — implementation is a direct structural copy of an already-shipped, reviewed pattern):

1. **Task 1 RED: failing tests for shipping indicator + polling** - `ed1e623` (test)
2. **Task 1 GREEN: shipping indicator + self-stopping polling implementation** - `f5a1ad5` (feat)

**Plan metadata:** (this commit, added after SUMMARY.md is written)

## Files Created/Modified
- `order-detail.component.ts` - Added `DestroyRef` inject, `isShipping` computed signal, `ORDER_DETAIL_TERMINAL_STATUSES` module constant, `startPolling()` private method called after the initial fetch succeeds
- `order-detail.component.html` - Added `@if (isShipping())` block with `mat-spinner` + `.shipping-indicator-text` below the status chip / failure-reason blocks
- `order-detail.component.scss` - Added `.shipping-indicator { display: flex; align-items: center; gap: var(--space-xs); margin-bottom: var(--space-lg); }` after `.status-badge`
- `order-detail.component.spec.ts` - Added 5 tests covering indicator visibility, polling-driven updates, terminal stop condition, destroy teardown, and no-navigate behavior

## Decisions Made
- Polling-tick errors do not clear the displayed order or set `notFound`/`hasError` — only the initial `ngOnInit` fetch error does. This matches the plan's explicit `<behavior>` spec and avoids blanking a page the user is actively viewing on a single transient network blip.
- `ORDER_DETAIL_TERMINAL_STATUSES` is a separate module-scope constant from `checkout-page.component.ts`'s `TERMINAL_STATUSES` (not shared/imported) since the two pages have genuinely different terminal sets (`/orders/:id` treats `'Paid'` as non-terminal — it's still expecting Fulfillment to move it forward).

## Deviations from Plan

None — plan executed exactly as written. One minor note: the plan's acceptance criterion `grep -c "takeUntilDestroyed" ... returns 1` actually resolves to `2` (one import line, one usage line) — this matches the identical count in the existing, already-shipped `checkout-page.component.ts` reference implementation the plan explicitly asked to structurally copy, so it is not treated as a discrepancy requiring a fix.

## Issues Encountered
- The worktree had no `node_modules` installed (fresh git worktree checkout); ran `npm install --prefer-offline --no-audit --no-fund` in `src/frontend/ecommerce-app` to restore the dependency tree before tests/tsc could run. This is a worktree-environment setup step, not a plan deviation — `node_modules` is gitignored and expected to be installed per-checkout.
- Initial RED-phase test ("shows the indicator while Paid") included an erroneous extra `httpMock.expectOne(...).flush(...)` call assuming a polling tick had already fired; since that test uses real timers (not `vi.useFakeTimers()`), no second request exists yet at assertion time. Fixed during GREEN by removing the incorrect drain call — this is a test-authoring correction, not a production-code deviation.

## User Setup Required

None - no external service configuration required.

## Next Phase Readiness
- D-07 and D-08 are fully satisfied on the frontend, independent of and in parallel with the Phase 5 backend work (05-01, 05-03 through 05-07)
- Once 05-03 (Orders saga) and 05-04 (Fulfillment service) land server-side, `/orders/:id` will automatically reflect `Paid → Fulfilled` transitions live via this polling loop with zero additional frontend work
- No blockers

---
*Phase: 05-fulfillment-notifications*
*Completed: 2026-08-14*
