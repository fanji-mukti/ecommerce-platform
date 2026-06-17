---
phase: "02"
plan: "06b"
subsystem: frontend
tags:
  - angular
  - angular-material
  - signals
  - oidc
  - vitest
  - catalog
  - auth
dependency_graph:
  requires:
    - 02-06a  # Angular shell scaffold (routes, OIDC config, models, test setup)
    - 02-02   # Identity service (OpenIddict PKCE at localhost:5005)
    - 02-03   # Catalog service (API at /api/catalog via YARP)
    - 02-05   # YARP gateway (proxy at localhost:5000)
  provides:
    - CatalogListComponent: signal-based product grid with pagination and category filter
    - ProductCardComponent: product card with stock badge, View Details routerLink
    - ProductDetailComponent: product detail with disabled Add to Cart placeholder
    - LoginComponent: PKCE redirect trigger via oidcSecurityService.authorize()
    - RegisterComponent: reactive form POSTing only {email, password} to /api/identity/register
    - CallbackComponent: OIDC token exchange via checkAuth(), navigates to /catalog
    - Vitest test: CatalogListComponent renders 'Browse Products' heading
  affects:
    - Phase 3 auth guard (Cart/Orders routes protected after this OIDC flow is proven)
tech_stack:
  added: []
  patterns:
    - signal<T>() for all component state (no BehaviorSubject)
    - input.required<T>() for component inputs (signal input API)
    - computed() for derived values (stock labels, category list)
    - @if template control flow (not *ngIf)
    - ReactiveFormsModule with cross-field validator (no NgModule)
    - setupTestBed({ zoneless: true }) from @analogjs/vitest-angular/setup-testbed
    - TDD RED/GREEN cycle for CatalogListComponent (vitest.config.ts updated)
key_files:
  created:
    - src/frontend/ecommerce-app/src/app/features/catalog/catalog-list/catalog-list.component.html
    - src/frontend/ecommerce-app/src/app/features/catalog/catalog-list/catalog-list.component.scss
    - src/frontend/ecommerce-app/src/app/features/catalog/catalog-list/catalog-list.component.spec.ts
    - src/frontend/ecommerce-app/src/app/features/catalog/product-card/product-card.component.ts
    - src/frontend/ecommerce-app/src/app/features/catalog/product-card/product-card.component.html
    - src/frontend/ecommerce-app/src/app/features/catalog/product-detail/product-detail.component.html
    - src/frontend/ecommerce-app/src/app/features/auth/login/login.component.html
    - src/frontend/ecommerce-app/src/app/features/auth/register/register.component.html
    - src/frontend/ecommerce-app/src/app/features/auth/callback/callback.component.html
    - src/frontend/ecommerce-app/src/test-setup.ts
  modified:
    - src/frontend/ecommerce-app/src/app/features/catalog/catalog-list/catalog-list.component.ts (stub replaced with full implementation)
    - src/frontend/ecommerce-app/src/app/features/catalog/product-detail/product-detail.component.ts (stub replaced)
    - src/frontend/ecommerce-app/src/app/features/auth/login/login.component.ts (stub replaced)
    - src/frontend/ecommerce-app/src/app/features/auth/register/register.component.ts (stub replaced)
    - src/frontend/ecommerce-app/src/app/features/auth/callback/callback.component.ts (stub replaced)
    - src/frontend/ecommerce-app/vitest.config.ts (setupFiles updated to use setup-testbed)
decisions:
  - "Used @analogjs/vitest-angular/setup-testbed (not setup-zone) — setup-zone is Zone.js-only and does not call TestBed.initTestEnvironment(); setup-testbed initializes BrowserTestingModule with zoneless:true required for Angular 20 component tests"
  - "Derived categories list computed from loaded products (not a separate API call) — avoids extra network round trip for Phase 2 scope"
  - "RegisterComponent posts { email, password } strictly — no role, isAdmin, or other fields reach /api/identity/register (T-02-06b-01 mitigation)"
  - "Cross-field password confirmation validator implemented as a plain function (not a class) — idiomatic Angular reactive forms pattern, no external library"
metrics:
  duration: "6 minutes"
  completed: "2026-06-17"
  tasks_completed: 2
  tasks_total: 2
  files_created: 10
  files_modified: 6
---

# Phase 02 Plan 06b: Angular Feature Components Summary

Six Angular 20 feature components implemented with full signal-based state, Angular Material UI, and a passing Vitest test — CatalogList with pagination/category filter, ProductCard with stock badges, ProductDetail with disabled cart placeholder, LoginComponent as PKCE redirect trigger, RegisterComponent with reactive form and mass-assignment protection, and CallbackComponent handling OIDC token exchange.

## Tasks Completed

| Task | Name | Commit | Files |
|------|------|--------|-------|
| 1 (RED) | Catalog Vitest failing test + setup-testbed fix | f16f5f0 | 3 files |
| 1 (GREEN) | CatalogList, ProductCard, ProductDetail components | 8f6bd76 | 7 files |
| 2 | Login, Register, Callback auth components | db38f5e | 6 files |

## Verification Evidence

1. `npm run build -- --configuration development` exits 0 (4s build time) — verified
2. `npm test` exits 0 — 3 tests pass: 2 model tests + CatalogListComponent heading test
3. CatalogListComponent uses `signal<Product[]>([])` — verified
4. CatalogListComponent.html h1 contains "Browse Products" — verified (Vitest asserts this)
5. LoginComponent calls `oidcSecurityService.authorize()` — no credentials form (T-02-06b-03 mitigated)
6. RegisterComponent POSTs to `/api/identity/register` with `{ email, password }` only (T-02-06b-01 mitigated)
7. RegisterComponent uses `Validators.minLength(8)` (T-02-06b-02 mitigated)
8. CallbackComponent calls `checkAuth()` and navigates to `/catalog` on success — verified
9. ProductDetailComponent has disabled "Add to Cart — Coming Soon" button — verified
10. No `*ngIf` directives — all components use `@if` template control flow — verified
11. All components: `standalone: true`, no NgModules — verified

## Deviations from Plan

### Auto-fixed Issues

**1. [Rule 3 - Blocking] Vitest setup-zone does not initialize Angular TestBed for component testing**
- **Found during:** Task 1 (RED phase — test run after writing spec)
- **Issue:** The existing `vitest.config.ts` used `@analogjs/vitest-angular/setup-zone` as the setupFile. This file imports Zone.js plugins but never calls `getTestBed().initTestEnvironment()`. When `TestBed.configureTestingModule()` is called in a component test, Angular throws "Need to call TestBed.initTestEnvironment() first."
- **Fix:** Created `src/test-setup.ts` that imports `setupTestBed` from `@analogjs/vitest-angular/setup-testbed` and calls `setupTestBed({ zoneless: true })`. Updated `vitest.config.ts` to use this file as the setup file. This correctly initializes `BrowserTestingModule` + `provideZonelessChangeDetection()` before tests run.
- **Files modified:** `vitest.config.ts`, `src/test-setup.ts` (created)
- **Commit:** f16f5f0

**2. [Rule 1 - Bug] Import path `../../shared/models/product.model` resolved to wrong directory**
- **Found during:** Task 1 GREEN (first build attempt)
- **Issue:** Catalog components at `features/catalog/catalog-list/` used `../../shared/` which resolves to `features/shared/` — a directory that does not exist. The model is at `src/app/shared/models/`.
- **Fix:** Updated all three catalog component imports to `../../../shared/models/product.model` (3 levels up from the component file to `app/`, then down to `shared/models/`).
- **Files modified:** catalog-list.component.ts, product-card.component.ts, product-detail.component.ts
- **Commit:** 8f6bd76 (fixed inline before commit)

## Known Stubs

None. All stubs from plan 02-06a have been replaced with full implementations. The only intentional placeholder is the "Add to Cart — Coming Soon" disabled button on ProductDetailComponent — this is per-spec, not a stub.

## TDD Gate Compliance

- RED commit exists: f16f5f0 — `test(02-06b): add failing CatalogListComponent Vitest test (RED)`
- GREEN commit exists: 8f6bd76 — `feat(02-06b): implement catalog feature components (GREEN)`
- GREEN gate: CatalogListComponent Vitest test passes

## Threat Flags

No new threat surface introduced beyond the plan's threat model:
- T-02-06b-01: RegisterComponent strictly sends `{ email, password }` — verified in register.component.ts line 79
- T-02-06b-02: `Validators.minLength(8)` applied to password field — verified in register.component.ts line 45
- T-02-06b-03: LoginComponent has no form fields; calls `authorize()` only — verified in login.component.ts line 16-18

## Self-Check: PASSED

Files verified:
- `src/frontend/ecommerce-app/src/app/features/catalog/catalog-list/catalog-list.component.ts` — exists, contains signal<Product[]>([])
- `src/frontend/ecommerce-app/src/app/features/catalog/catalog-list/catalog-list.component.html` — exists, h1 "Browse Products"
- `src/frontend/ecommerce-app/src/app/features/catalog/catalog-list/catalog-list.component.spec.ts` — exists, test passes
- `src/frontend/ecommerce-app/src/app/features/catalog/product-card/product-card.component.ts` — exists, input.required<Product>()
- `src/frontend/ecommerce-app/src/app/features/catalog/product-detail/product-detail.component.html` — exists, "Add to Cart — Coming Soon" disabled button
- `src/frontend/ecommerce-app/src/app/features/auth/login/login.component.ts` — exists, contains authorize()
- `src/frontend/ecommerce-app/src/app/features/auth/register/register.component.ts` — exists, contains identity/register
- `src/frontend/ecommerce-app/src/app/features/auth/callback/callback.component.ts` — exists, contains checkAuth()
- Commit f16f5f0 (RED test) — verified
- Commit 8f6bd76 (catalog GREEN) — verified
- Commit db38f5e (auth components) — verified
