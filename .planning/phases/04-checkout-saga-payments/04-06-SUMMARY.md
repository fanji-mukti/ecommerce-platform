---
phase: 04-checkout-saga-payments
plan: 06
subsystem: ui
tags: [angular, angular-material, rxjs, polling, mat-stepper, checkout]

# Dependency graph
requires:
  - phase: 04-checkout-saga-payments
    provides: "GET /api/checkout/{id}, POST /api/checkout, GET /api/orders/{id} response shapes (plan 04-05 Checkout.API public facade)"
provides:
  - "/checkout Angular page: mat-stepper status visualization, 1.5s polling loop, .99 hint text, demo failure toggle, auto-redirect to /orders/:id"
  - "/orders/:id Angular page: order status chip + verbatim failure-reason display"
  - "cart-page 'Proceed to Checkout' CTA wired to /checkout (replaces disabled placeholder)"
affects: [phase-05-fulfillment-notifications, phase-06-hardening-azure-deployment]

# Tech tracking
tech-stack:
  added: []
  patterns:
    - "RxJS interval(1500)+switchMap+takeWhile(inclusive) as the standard polling-until-terminal-state idiom for Angular status pages"
    - "Signal-based loading/error/data state (cart/isLoading/hasError pattern from CartPageComponent) reused for CheckoutPageComponent and OrderDetailComponent"

key-files:
  created:
    - src/frontend/ecommerce-app/src/app/shared/models/checkout.model.ts
    - src/frontend/ecommerce-app/src/app/shared/models/order.model.ts
    - src/frontend/ecommerce-app/src/app/core/services/checkout.service.ts
    - src/frontend/ecommerce-app/src/app/core/services/orders.service.ts
    - src/frontend/ecommerce-app/src/app/features/checkout/checkout-page/checkout-page.component.ts
    - src/frontend/ecommerce-app/src/app/features/checkout/checkout-page/checkout-page.component.html
    - src/frontend/ecommerce-app/src/app/features/checkout/checkout-page/checkout-page.component.scss
    - src/frontend/ecommerce-app/src/app/features/checkout/checkout-page/checkout-page.component.spec.ts
    - src/frontend/ecommerce-app/src/app/features/orders/order-detail/order-detail.component.ts
    - src/frontend/ecommerce-app/src/app/features/orders/order-detail/order-detail.component.html
    - src/frontend/ecommerce-app/src/app/features/orders/order-detail/order-detail.component.scss
    - src/frontend/ecommerce-app/src/app/features/orders/order-detail/order-detail.component.spec.ts
  modified:
    - src/frontend/ecommerce-app/src/app/app.routes.ts
    - src/frontend/ecommerce-app/src/app/features/cart/cart-page/cart-page.component.html
    - src/frontend/ecommerce-app/src/app/features/cart/cart-page/cart-page.component.scss

key-decisions:
  - "Added minimal placeholder CheckoutPageComponent/OrderDetailComponent in Task 1 so app.routes.ts compiles ahead of their full implementation in Tasks 2-3 (routes reference components that don't fully exist until later tasks)"
  - "Used [checked]/(change) binding on mat-checkbox instead of ngModel/FormsModule to keep the demo toggle signal-compatible, per the plan's stated fallback"
  - "cart-page.component.ts required no changes — RouterLink was already imported for the existing empty-state 'Browse Catalog' link"

patterns-established:
  - "Pattern: fake-timer RxJS polling tests using vi.useFakeTimers() + vi.advanceTimersByTimeAsync(1500) to assert stop-on-terminal-state behavior without real waits"

requirements-completed: [FE-03, CHK-01, CHK-02]

# Metrics
duration: ~50min
completed: 2026-08-12
---

# Phase 04 Plan 06: Checkout & Order Detail Angular Pages Summary

**Angular `/checkout` (mat-stepper + 1.5s RxJS polling + demo failure toggle) and `/orders/:id` (verbatim failure-reason display) pages, wired end-to-end from the cart page's CTA.**

## Performance

- **Duration:** ~50 min
- **Completed:** 2026-08-12T14:24:05Z
- **Tasks:** 3 completed (Task 2 and 3 each ran as RED -> GREEN TDD pairs)
- **Files modified:** 16 (12 created, 3 modified — plus 2 new spec files as part of TDD)

## Accomplishments
- Built the only user-visible proof of the whole Phase 4 saga: a user can click through Cart -> Checkout -> Place Order -> watch a live-updating `mat-stepper` -> land on `/orders/:id` automatically, with zero manual refreshes (FE-03).
- `/checkout` polls `GET /api/checkout/{id}` on a fixed 1.5s interval via `interval(1500).pipe(switchMap(...), takeWhile(..., true))`, stopping the instant a terminal state is reached and redirecting to `/orders/:id` (D-07/D-08).
- `/orders/:id` renders the backend's exact `failureReason` string (e.g. "Payment declined") verbatim for `Cancelled`/`Failed` orders in a warn-colored `mat-chip`, and a neutral/primary chip with no failure text for non-terminal-failure statuses.
- Both the `.99` hint text and the "Simulate payment failure" demo toggle are visible on `/checkout` before Place Order is clicked (D-10/D-11).
- Replaced cart-page's disabled "Proceed to Checkout — Coming Soon" placeholder with a working `routerLink="/checkout"` link.

## Task Commits

Each task was committed atomically (Tasks 2 and 3 used the RED/GREEN TDD cycle per their `tdd="true"` flag):

1. **Task 1: Models, services, and routes** - `73d17b9` (feat)
2. **Task 2: CheckoutPageComponent — stepper, polling, hint text, demo toggle**
   - `da09b09` (test) — RED: 4 failing tests against the placeholder component
   - `e281760` (feat) — GREEN: full implementation, all 4 tests pass
3. **Task 3: OrderDetailComponent and cart-page CTA wiring**
   - `efda9c7` (test) — RED: 4 failing tests against the placeholder component
   - `2e2c8cc` (feat) — GREEN: full implementation, all 4 tests pass

**Plan metadata:** (this commit, made by the orchestrator after merge)

## Files Created/Modified
- `shared/models/checkout.model.ts` - `CheckoutStatusValue` union + `CheckoutStatus` interface matching the backend `GET /api/checkout/{id}` shape
- `shared/models/order.model.ts` - `OrderLineItem`/`OrderDetail` interfaces matching `GET /api/orders/{id}`
- `core/services/checkout.service.ts` - `startCheckout(simulatePaymentFailure)` (`POST /api/checkout`), `getStatus(id)` (`GET /api/checkout/{id}`)
- `core/services/orders.service.ts` - `getOrder(id)` (`GET /api/orders/{id}`)
- `features/checkout/checkout-page/checkout-page.component.ts/.html/.scss` - Redirects empty carts to `/cart`; `mat-stepper` driven by polled status; Place Order -> disabled+spinner -> 1.5s poll -> auto-redirect on terminal state; `.99` hint text + demo toggle
- `features/checkout/checkout-page/checkout-page.component.spec.ts` - 4 tests: empty-cart redirect, hint/toggle visibility, Place Order disables button + posts correct body, fake-timer-driven poll-to-terminal-state + navigation
- `features/orders/order-detail/order-detail.component.ts/.html/.scss` - Route-param -> `OrdersService.getOrder` -> signal, mirroring `ProductDetailComponent`'s not-found pattern; verbatim failure-reason paragraph + warn/primary status chip
- `features/orders/order-detail/order-detail.component.spec.ts` - 4 tests: not-found on missing param, not-found on fetch error, failure-reason + warn chip for cancelled order, no failure-reason + primary chip for paid order
- `app.routes.ts` - Added `/checkout` and `/orders/:id` routes before the wildcard route
- `features/cart/cart-page/cart-page.component.html/.scss` - Replaced disabled "Proceed to Checkout — Coming Soon" button with an enabled `<a routerLink="/checkout">` link; updated the `.order-summary` button-width selector to target the new anchor element

## Decisions Made
- Added minimal placeholder `CheckoutPageComponent`/`OrderDetailComponent` in Task 1 (not listed in Task 1's original `<files>`) so that wiring the routes in `app.routes.ts` compiles ahead of their full implementation in Tasks 2/3 — each was fully replaced (not incrementally patched) when its owning task ran, so the final diff still reads as if built in the task that owns it.
- Used `mat-checkbox`'s `[checked]`/`(change)` binding rather than `ngModel`/`FormsModule` for the demo toggle, keeping it signal-compatible per the plan's own stated fallback (`FormsModule` import avoided entirely).
- `cart-page.component.ts` needed no code changes — `RouterLink` was already in its `imports` array from the pre-existing empty-state "Browse Catalog" link.

## Deviations from Plan

### Auto-fixed Issues

**1. [Rule 3 - Blocking] Added placeholder CheckoutPageComponent/OrderDetailComponent in Task 1**
- **Found during:** Task 1 (Models, services, and routes)
- **Issue:** `app.routes.ts` imports `CheckoutPageComponent` and `OrderDetailComponent`, which are only fully built in Tasks 2 and 3. Task 1's own `<verify>` step requires `tsc --noEmit` to succeed, which is impossible with routes importing nonexistent components.
- **Fix:** Created minimal standalone stub components (empty template, no logic) in Task 1's commit so the app compiles; Tasks 2 and 3 then fully overwrote each stub with its real implementation.
- **Files modified:** `features/checkout/checkout-page/checkout-page.component.{ts,html,scss}`, `features/orders/order-detail/order-detail.component.{ts,html,scss}` (created in Task 1, replaced in Tasks 2/3)
- **Verification:** `npx tsc --noEmit -p tsconfig.app.json` passed after Task 1's commit and after every subsequent task's commit.
- **Committed in:** `73d17b9` (Task 1 commit; superseded by `e281760` and `2e2c8cc`)

**2. [Rule 1 - Bug] Fixed CSS selector after changing "Proceed to Checkout" from button to anchor**
- **Found during:** Task 3 (cart-page CTA wiring)
- **Issue:** The plan's action specified replacing `<button disabled>` with `<a routerLink="/checkout">`, but `cart-page.component.scss` had a `.order-summary button { width: 100%; }` rule that would silently stop applying to the new `<a>` element, leaving the CTA at its intrinsic (non-full-width) size.
- **Fix:** Updated the selector to `.order-summary a[mat-raised-button]` with `display: block; text-align: center; width: 100%;`.
- **Files modified:** `features/cart/cart-page/cart-page.component.scss`
- **Verification:** Visual/structural review of the compiled selector; no automated visual-regression check exists in this project.
- **Committed in:** `2e2c8cc` (Task 3 commit)

---

**Total deviations:** 2 auto-fixed (1 blocking, 1 bug)
**Impact on plan:** Both auto-fixes were necessary to keep the plan's own per-task verification gates green; no scope creep beyond what the plan's task boundaries already implied.

## Issues Encountered
- The worktree had no `node_modules` installed for the Angular project (fresh worktree checkout). Ran `npm ci` from `package-lock.json` (no new packages added, existing lockfile only) to enable `tsc --noEmit` and `vitest` verification for every task.
- Task 2's acceptance criteria required the literal substring `interval(1500)` in the component source; an initial implementation used a named `POLL_INTERVAL_MS` constant which failed that grep check. Inlined the literal `1500` into the `interval(...)` call to satisfy the criteria exactly.

## User Setup Required
None - no external service configuration required.

## Next Phase Readiness
- FE-03 is fully satisfied: a user can complete checkout entirely through the UI and watch order status update in real time via polling, with both the `.99` trigger and the explicit demo toggle discoverable on `/checkout`.
- Manual smoke check still recommended per the plan's `<verification>` block: run the full AppHost + `ng serve`, click through Cart -> Checkout -> Place Order, confirm the stepper advances and the page redirects to `/orders/:id` showing `Paid`; repeat with a `.99` total or the demo toggle to confirm a `Cancelled` order showing "Payment declined".
- No blockers for Phase 5 (Fulfillment & Notifications) — this plan's UI surface consumes the existing `/api/checkout` and `/api/orders` contracts from plan 04-05 without modification.

---
*Phase: 04-checkout-saga-payments*
*Completed: 2026-08-12*

## Self-Check: PASSED

All 16 claimed files verified present via `git ls-files --error-unmatch` (12 created + 3 modified + this summary). All 5 task commit hashes (`73d17b9`, `da09b09`, `e281760`, `efda9c7`, `2e2c8cc`) verified present in `git log --oneline --all`. Full frontend test suite (16 tests across 6 spec files) passes. `npx tsc --noEmit -p tsconfig.app.json` passes with zero errors.
