---
phase: 03-cart-orders-skeleton
plan: 04
subsystem: ui
tags: [angular, angular-material, signals, rxjs, cart]

requires:
  - phase: 03-cart-orders-skeleton (Plan 03-01)
    provides: Cart API (GET/POST/PATCH/DELETE /api/cart, /api/cart/items/{productId}) behind the gateway, JWT-protected
provides:
  - Angular Cart/CartLineItem models and CartService HTTP client
  - CartPageComponent: loading/error/empty/populated states, debounced quantity updates, immediate remove
  - CartLineItemComponent: quantity stepper, remove button, line total
  - /cart route registration
  - Functional "Add to Cart" entry point on /product/:id
  - Authenticated-only "Cart" nav link in app.html
affects: [phase-04-checkout-saga-payments]

tech-stack:
  added: []
  patterns:
    - "RxJS Subject + debounceTime(500) for per-line quantity-update debouncing, subscribed once in the component constructor"
    - "Optimistic local update of only the affected line's quantity/lineTotal on stepper click; itemCount/grandTotal stay pinned to the last server-confirmed Cart response until the debounced PATCH resolves"
    - "HttpErrorResponse status-based branching in service subscribers: 401 -> isUnauthorized signal + router.navigate(['/login']); other -> hasError signal"

key-files:
  created:
    - src/frontend/ecommerce-app/src/app/shared/models/cart.model.ts
    - src/frontend/ecommerce-app/src/app/core/services/cart.service.ts
    - src/frontend/ecommerce-app/src/app/features/cart/cart-page/cart-page.component.ts
    - src/frontend/ecommerce-app/src/app/features/cart/cart-page/cart-page.component.html
    - src/frontend/ecommerce-app/src/app/features/cart/cart-page/cart-page.component.scss
    - src/frontend/ecommerce-app/src/app/features/cart/cart-page/cart-page.component.spec.ts
    - src/frontend/ecommerce-app/src/app/features/cart/cart-line-item/cart-line-item.component.ts
    - src/frontend/ecommerce-app/src/app/features/cart/cart-line-item/cart-line-item.component.html
    - src/frontend/ecommerce-app/src/app/features/catalog/product-detail/product-detail.component.spec.ts
  modified:
    - src/frontend/ecommerce-app/src/app/app.routes.ts
    - src/frontend/ecommerce-app/src/app/app.html
    - src/frontend/ecommerce-app/src/app/features/catalog/product-detail/product-detail.component.ts
    - src/frontend/ecommerce-app/src/app/features/catalog/product-detail/product-detail.component.html

key-decisions:
  - "Grand total and item count are never locally recomputed on quantity change — only the touched line's quantity/lineTotal update optimistically; the summary panel always reflects the last GET/PATCH/DELETE /cart server response (T-03-14 compliance)"
  - "Added product-detail.component.spec.ts (not in plan's files list) since no prior test coverage existed for that component and the plan's own verify step (`npx vitest run product-detail`) requires it to exist"

patterns-established:
  - "Cart line-item touch targets use inline min-width/min-height: 44px on mat-icon-button (no shared SCSS file exists for cart-line-item, per plan's files list) to satisfy the 44x44px accessibility exception in 03-UI-SPEC.md"

requirements-completed: [FE-02]

coverage:
  - id: D1
    description: "CartPageComponent renders loading/error/empty/populated states sourced from GET /api/cart, with 401 redirecting to /login instead of the generic error state"
    requirement: FE-02
    verification:
      - kind: unit
        ref: "src/app/features/cart/cart-page/cart-page.component.spec.ts#renders 'Your Cart' as the h1 heading"
        status: pass
      - kind: unit
        ref: "src/app/features/cart/cart-page/cart-page.component.spec.ts#renders empty state with a Browse Catalog link when the cart has no items"
        status: pass
    human_judgment: true
    rationale: "401-redirect-to-login and error-state Retry flow are not covered by an automated test (only 200/empty-cart paths are unit-tested); needs a manual pass with the backend running to confirm the network behavior end-to-end."
  - id: D2
    description: "Cart item-count subtext pluralizes '1 item' vs 'N items'"
    requirement: FE-02
    verification:
      - kind: unit
        ref: "src/app/features/cart/cart-page/cart-page.component.spec.ts#pluralizes the item-count subtext: 1 item vs N items"
        status: pass
    human_judgment: false
  - id: D3
    description: "Quantity stepper clicks debounce (~500ms) into a single PATCH /api/cart/items/{productId}; remove is immediate with no confirmation dialog"
    requirement: FE-02
    verification: []
    human_judgment: true
    rationale: "Debounce timing and rapid-click coalescing require a live network trace (or fake-timer test not written in this plan) to verify; no automated test exercises CartLineItemComponent's stepper or CartPageComponent's debounce Subject directly."
  - id: D4
    description: "Add to Cart on /product/:id is enabled, calls CartService.addItem, and navigates to /cart on success"
    requirement: FE-02
    verification:
      - kind: unit
        ref: "src/app/features/catalog/product-detail/product-detail.component.spec.ts#renders an enabled \"Add to Cart\" button (no \"Coming Soon\" placeholder)"
        status: pass
      - kind: unit
        ref: "src/app/features/catalog/product-detail/product-detail.component.spec.ts#calls CartService.addItem and navigates to /cart on click"
        status: pass
    human_judgment: false
  - id: D5
    description: "Cart nav link appears in app.html only when authenticated"
    requirement: FE-02
    verification: []
    human_judgment: true
    rationale: "No spec exists for the top-level AppComponent's authenticated/unauthenticated nav branches; verified only by grep for the routerLink and manual visual check is recommended."

duration: 21min
completed: 2026-08-04
status: complete
---

# Phase 3 Plan 04: Cart Angular UI Summary

**Angular cart feature (models, HttpClient service, page + line-item components) wired to Plan 03-01's Cart API, with a debounced quantity stepper and a functional product-detail "Add to Cart" entry point.**

## Performance

- **Duration:** 21 min
- **Started:** 2026-08-04T14:01:03Z
- **Completed:** 2026-08-04T14:22:00Z
- **Tasks:** 2
- **Files modified:** 13 (9 created, 4 modified)

## Accomplishments
- `/cart` route renders a working Angular Material cart page covering loading, error, empty, and populated (1-10 item) states per 03-UI-SPEC.md
- Quantity stepper updates the local line item immediately, then sends a single debounced (500ms) PATCH per settled value — rapid clicks do not spam the network
- Remove is immediate (no confirmation dialog), matching the D-09 "instant, no network-blocking modal" philosophy
- `/product/:id`'s previously-disabled "Add to Cart — Coming Soon" button now calls `CartService.addItem` and navigates to `/cart` on success, keeping its `color="primary"` styling
- Authenticated users see a "Cart" nav link in the top toolbar

## Task Commits

Each task was committed atomically:

1. **Task 1: Cart feature — model, service, page component, line-item component** - `451dad1` (feat)
2. **Task 2: Wire Add to Cart entry point and nav link** - `8a6fff3` (feat)

Follow-up style fix (Rule 1, backstop truth compliance): `3321f04` (style)

**Plan metadata:** committed separately after this summary

## Files Created/Modified
- `src/frontend/ecommerce-app/src/app/shared/models/cart.model.ts` - `CartLineItem`/`Cart` plain interfaces
- `src/frontend/ecommerce-app/src/app/core/services/cart.service.ts` - `CartService` (getCart/addItem/updateQuantity/removeItem)
- `src/frontend/ecommerce-app/src/app/features/cart/cart-page/cart-page.component.ts` - Cart page container: state signals, debounced quantity Subject, 401 handling
- `src/frontend/ecommerce-app/src/app/features/cart/cart-page/cart-page.component.html` - loading/error/empty/populated markup
- `src/frontend/ecommerce-app/src/app/features/cart/cart-page/cart-page.component.scss` - reuses `.error-state`/`.empty-state` pattern from catalog-list
- `src/frontend/ecommerce-app/src/app/features/cart/cart-page/cart-page.component.spec.ts` - 3 tests (heading, empty state, pluralization)
- `src/frontend/ecommerce-app/src/app/features/cart/cart-line-item/cart-line-item.component.ts` - single line-item row logic (stepper increment/decrement, remove emit)
- `src/frontend/ecommerce-app/src/app/features/cart/cart-line-item/cart-line-item.component.html` - mat-card row markup, 44x44px stepper buttons, 2-line clamp on product name
- `src/frontend/ecommerce-app/src/app/app.routes.ts` - registered `/cart` route
- `src/frontend/ecommerce-app/src/app/app.html` - added authenticated-only "Cart" nav link
- `src/frontend/ecommerce-app/src/app/features/catalog/product-detail/product-detail.component.ts` - `onAddToCart()`, `isAdding`/`addError` signals
- `src/frontend/ecommerce-app/src/app/features/catalog/product-detail/product-detail.component.html` - enabled Add to Cart button, inline add-error message
- `src/frontend/ecommerce-app/src/app/features/catalog/product-detail/product-detail.component.spec.ts` - 2 tests (button renders enabled, click wires addItem + navigate)

## Decisions Made
- Grand total/item count are never recomputed from local-only state on a quantity change — only the touched line's `quantity`/`lineTotal` update optimistically; `itemCount`/`grandTotal` stay pinned to the last server response until the debounced PATCH resolves. This satisfies the plan's must-have truth and threat register entry T-03-14 more literally than a fully-recomputed local total would.
- No separate `.scss` file was created for `cart-line-item` (matches the plan's `files_modified` list, which omits one) — touch-target and line-clamp styles are inline in the template.

## Deviations from Plan

### Auto-fixed Issues

**1. [Rule 2 - Missing Critical] Added `product-detail.component.spec.ts`**
- **Found during:** Task 2 (Wire Add to Cart entry point)
- **Issue:** No test file existed for `ProductDetailComponent` prior to this plan, yet the plan's own `<verify><automated>` step for Task 2 is `npx vitest run product-detail`, which fails with "No test files found" if none exists.
- **Fix:** Added a spec covering (a) the enabled Add to Cart button rendering without the "Coming Soon" placeholder, and (b) the click handler calling `CartService.addItem` and navigating to `/cart`.
- **Files modified:** `src/frontend/ecommerce-app/src/app/features/catalog/product-detail/product-detail.component.spec.ts`
- **Verification:** `npx vitest run product-detail` — 2/2 pass
- **Committed in:** `8a6fff3` (Task 2 commit)

**2. [Rule 1 - Bug] Added `text-overflow: ellipsis` to the line-clamp style**
- **Found during:** post-Task-1 review against the truths_backstop item ("Product name snapshot on cart line items truncates to 2 lines via `-webkit-line-clamp: 2` with ellipsis for long names")
- **Issue:** The initial line-clamp style (`overflow: hidden; display: -webkit-box; -webkit-line-clamp: 2; -webkit-box-orient: vertical;`) omitted `text-overflow: ellipsis`, which is the conventional pairing to guarantee the visible ellipsis glyph on truncation.
- **Fix:** Added `text-overflow: ellipsis;` to the inline style.
- **Files modified:** `src/frontend/ecommerce-app/src/app/features/cart/cart-line-item/cart-line-item.component.html`
- **Verification:** Full vitest suite re-run (8/8 pass)
- **Committed in:** `3321f04`

---

**Total deviations:** 2 auto-fixed (1 missing critical test coverage, 1 cosmetic bug)
**Impact on plan:** Both additions are test/CSS-correctness only — no scope creep, no new files beyond the plan's implied verification surface.

## Issues Encountered
None beyond the deviations documented above.

## Known Stubs
- The "Proceed to Checkout — Coming Soon" button in the Order Summary panel is an intentional disabled placeholder per 03-UI-SPEC.md's Copywriting Contract — checkout wiring is explicitly deferred to Phase 4.

## User Setup Required
None - no external service configuration required.

## Next Phase Readiness
- FE-02 vertical slice complete: a logged-in user can add an item from `/product/:id`, land on `/cart`, adjust quantities, remove items, and see a correctly-totaled Order Summary sourced from Plan 03-01's Cart API.
- Manual UAT still recommended (see coverage `human_judgment: true` entries) for: 401-redirect-to-login, error-state Retry, debounce timing under rapid clicks, and the authenticated-only nav link — these require a running backend/browser session that this executor did not have available.
- Phase 4 (Checkout Saga & Payments) can build on the disabled "Proceed to Checkout" button as its entry point.

---
*Phase: 03-cart-orders-skeleton*
*Completed: 2026-08-04*

## Self-Check: PASSED

All 13 created/modified files confirmed present on disk; all 3 commit hashes (`451dad1`, `8a6fff3`, `3321f04`) confirmed in git log.
