---
phase: 05-fulfillment-notifications
plan: 08
subsystem: ui
tags: [angular, angular-material, mat-list, notifications, signals, vitest]

# Dependency graph
requires:
  - phase: 05-fulfillment-notifications (plan 05-05)
    provides: "GET /notifications JWT-scoped endpoint, NotificationEntry response shape (id, orderId, message, eventType, occurredAt)"
provides:
  - "NotificationEntry Angular model + NotificationsService thin HTTP wrapper"
  - "NotificationsPageComponent — the sole UI surface for NOT-01 (loading/empty/error/populated mat-list states)"
  - "/notifications route and toolbar nav link (D-06)"
affects: []

# Tech tracking
tech-stack:
  added: []
  patterns:
    - "NotificationsPageComponent mirrors catalog-list.component.ts's isLoading/hasError signal pair + ngOnInit/loadX()/retry() lifecycle shape"
    - "mat-list / mat-list-item introduced to this repo for the first time — full-row routerLink target (a[mat-list-item]), not a button"

key-files:
  created:
    - src/frontend/ecommerce-app/src/app/shared/models/notification.model.ts
    - src/frontend/ecommerce-app/src/app/core/services/notifications.service.ts
    - src/frontend/ecommerce-app/src/app/features/notifications/notifications-page/notifications-page.component.ts
    - src/frontend/ecommerce-app/src/app/features/notifications/notifications-page/notifications-page.component.html
    - src/frontend/ecommerce-app/src/app/features/notifications/notifications-page/notifications-page.component.scss
    - src/frontend/ecommerce-app/src/app/features/notifications/notifications-page/notifications-page.component.spec.ts
  modified:
    - src/frontend/ecommerce-app/src/app/app.routes.ts
    - src/frontend/ecommerce-app/src/app/app.html

key-decisions:
  - "Followed the plan's locked eventType->icon mapping exactly (payment/local_shipping/error), only PaymentFailed gets destructive-icon red styling"
  - "NotificationsService takes no parameters — backend scopes by JWT server-side, avoiding an IDOR-prone client-supplied userId (T-05-14)"

patterns-established:
  - "First mat-list/mat-list-item usage in the repo — full-row anchor pattern (a[mat-list-item] + [routerLink]) for list rows that are themselves navigation targets"

requirements-completed: [NOT-01]

# Metrics
duration: 11min
completed: 2026-08-19
---

# Phase 05 Plan 08: Notifications UI — Model, Service, Page, Nav Summary

**Angular `/notifications` inbox page (mat-list, TDD-covered) with loading/empty/error/populated states, a locked eventType-to-icon mapping, and a toolbar nav link — the only UI surface for NOT-01.**

## Performance

- **Duration:** ~11 min
- **Started:** 2026-08-19T14:25:00Z (approx.)
- **Completed:** 2026-08-19T14:36:24Z
- **Tasks:** 2 completed
- **Files modified:** 8 (6 created, 2 modified)

## Accomplishments
- `NotificationEntry` model + `NotificationsService` thin HTTP wrapper (`GET /api/notifications`), matching `orders.service.ts`'s exact convention, no client-supplied `userId` (IDOR-safe by construction)
- `NotificationsPageComponent` built via genuine RED/GREEN TDD: a failing spec (5 tests covering loading, empty, error+retry, populated rows, and pure icon-mapping methods) written first, confirmed failing (component didn't exist), then implemented to pass
- Locked icon mapping enforced: `payment` (OrderPaid), `local_shipping` (OrderShipped), `error` (PaymentFailed) — only the `PaymentFailed` icon gets `destructive-icon` red styling, per UI-SPEC's color-discipline note
- Each notification row is a full-row `<a mat-list-item [routerLink]="['/orders', n.orderId]">` — first `mat-list`/`mat-list-item` usage in this repo
- `/notifications` route added to `app.routes.ts`; toolbar nav link added to `app.html` inside the existing `@if (isAuthenticated()...)` block, immediately after Cart (D-06)
- Full verification: `tsc --noEmit` zero errors, full Vitest suite (28 tests across 7 files) passes with zero regressions

## Task Commits

Each task was committed atomically:

1. **Task 1: Model and service** - `8b41a83` (feat)
2. **Task 2: NotificationsPageComponent, route, nav link** - `d511867` (test, RED) → `e9b6240` (feat, GREEN)

_Note: Worktree mode — this plan's SUMMARY.md is committed separately by this agent; STATE.md/ROADMAP.md are updated centrally by the orchestrator after merge._

## Files Created/Modified
- `src/frontend/ecommerce-app/src/app/shared/models/notification.model.ts` - `NotificationEntry` interface + locked `NotificationEventType` union
- `src/frontend/ecommerce-app/src/app/core/services/notifications.service.ts` - `getNotifications()` thin HttpClient wrapper, no params
- `src/frontend/ecommerce-app/src/app/features/notifications/notifications-page/notifications-page.component.ts` - Loading/empty/error/populated states, `iconFor`/`isDestructive` helpers
- `src/frontend/ecommerce-app/src/app/features/notifications/notifications-page/notifications-page.component.html` - `mat-list` template per UI-SPEC
- `src/frontend/ecommerce-app/src/app/features/notifications/notifications-page/notifications-page.component.scss` - `var(--space-*)` convention, matches `order-detail.component.scss`
- `src/frontend/ecommerce-app/src/app/features/notifications/notifications-page/notifications-page.component.spec.ts` - 5 Vitest/TestBed tests (loading, empty, error+retry, populated icons/links, pure icon-mapping methods)
- `src/frontend/ecommerce-app/src/app/app.routes.ts` - Added `/notifications` route
- `src/frontend/ecommerce-app/src/app/app.html` - Added "Notifications" nav link after "Cart"

## Decisions Made
- Ran `npm install` in the worktree to restore `node_modules` (not present in the fresh worktree checkout) before any `tsc`/`vitest` verification could run — a standard dependency-restore operation (existing `package.json`/lockfile), not a new package addition, so it does not fall under the Rule 3 package-install exclusion
- Test file targets pure methods (`iconFor`, `isDestructive`) via `TestBed.createComponent` without `detectChanges()`, avoiding an unintended `ngOnInit`-triggered HTTP call in that specific assertion

## Deviations from Plan

None - plan executed exactly as written.

## Issues Encountered
None.

## User Setup Required

None - no external service configuration required.

## Next Phase Readiness
- NOT-01 is now demoable end-to-end through the UI: a logged-in user reaches `/notifications` via the nav bar and sees their inbox, each row linking to `/orders/:id`
- This was the final plan (wave 3) of Phase 05 — Fulfillment & Notifications; no further phase-05 UI work is queued

---
*Phase: 05-fulfillment-notifications*
*Completed: 2026-08-19*

## Self-Check: PASSED

All 7 claimed files verified present on disk; all 4 commits (`8b41a83`, `d511867`, `e9b6240`, `a8dcbf2`) verified present in `git log`.
