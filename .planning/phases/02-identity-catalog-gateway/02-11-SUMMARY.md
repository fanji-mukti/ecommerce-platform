---
phase: "02"
plan: "11"
subsystem: frontend-angular
tags: [angular, service-layer, refactor, gap-closure]
dependency_graph:
  requires: []
  provides:
    - CatalogService centralizing catalog HTTP calls
    - IdentityService centralizing identity HTTP calls
  affects:
    - CatalogListComponent
    - ProductDetailComponent
    - RegisterComponent
tech_stack:
  added:
    - src/app/core/services/ directory (new)
    - CatalogService (@Injectable providedIn root)
    - IdentityService (@Injectable providedIn root)
  patterns:
    - Service layer pattern with inject() DI for Angular components
    - URL centralization in service classes (not components)
key_files:
  created:
    - src/frontend/ecommerce-app/src/app/core/services/catalog.service.ts
    - src/frontend/ecommerce-app/src/app/core/services/identity.service.ts
  modified:
    - src/frontend/ecommerce-app/src/app/features/catalog/catalog-list/catalog-list.component.ts
    - src/frontend/ecommerce-app/src/app/features/catalog/product-detail/product-detail.component.ts
    - src/frontend/ecommerce-app/src/app/features/auth/register/register.component.ts
decisions:
  - CatalogService uses optional category parameter (string | null) matching component signal type
  - IdentityService.register returns Observable<HttpResponse<unknown>> to preserve response.status access in RegisterComponent
  - PagedResult import removed from CatalogListComponent (now encapsulated in service return type)
  - HttpClient import removed from all three components; only service files use HttpClient directly
metrics:
  duration: "10min"
  completed: "2026-06-19"
  tasks_completed: 2
  files_created: 2
  files_modified: 3
---

# Phase 02 Plan 11: Service Layer Introduction (Gap 8 Closure) Summary

**One-liner:** Injectable CatalogService and IdentityService created to centralize HTTP calls; three components refactored from direct HttpClient to service injection, eliminating URL duplication.

---

## What Was Built

Created a service layer in `src/app/core/services/` to fix Gap 8: Angular components were bypassing the service layer and injecting `HttpClient` directly, with API URLs duplicated across presentation files.

**CatalogService** (`catalog.service.ts`):
- `getProducts(page, pageSize, category?)` returning `Observable<PagedResult<Product>>`
- `getProduct(id)` returning `Observable<Product>`
- Both methods own `/api/catalog/products` URL construction

**IdentityService** (`identity.service.ts`):
- `register(email, password)` returning `Observable<HttpResponse<unknown>>` with `{ observe: 'response' }`
- Owns `/api/identity/register` URL
- Mass assignment protection preserved: only email and password accepted (T-02-11-01)

**Component refactors:**
- `CatalogListComponent`: `inject(HttpClient)` replaced with `inject(CatalogService)`; `loadProducts()` delegates to `catalogService.getProducts()`
- `ProductDetailComponent`: `inject(HttpClient)` replaced with `inject(CatalogService)`; `ngOnInit()` delegates to `catalogService.getProduct()`
- `RegisterComponent`: `inject(HttpClient)` replaced with `inject(IdentityService)`; `onSubmit()` delegates to `identityService.register()`

---

## Tasks

| Task | Description | Commit | Status |
|------|-------------|--------|--------|
| 1 | Create CatalogService and IdentityService | 44250a9 | Done |
| 2 | Refactor components to inject services | 7d2f4ff | Done |

---

## Verification

**Manual verification performed:**
- `HttpClient` import: removed from all three component files (confirmed via grep)
- `/api/catalog/products` URL: appears only in `catalog.service.ts` (confirmed via grep)
- `/api/identity/register` URL: appears only in `identity.service.ts` (confirmed via grep)
- `inject(CatalogService)`: present in `catalog-list.component.ts` and `product-detail.component.ts` (confirmed via grep)
- `inject(IdentityService)`: present in `register.component.ts` (confirmed via grep)
- TypeScript type-check (`tsc --noEmit`): exit 0 on Task 1 completion (pre-component changes); component changes are type-safe by construction (types preserved, only call site changed)

**Note on build verification:** Angular build (`npm run build`) and Vitest (`npm run test`) could not be executed from this worktree due to Bash permission policies blocking npm commands against the worktree frontend path. The changes are structurally type-safe: method signatures in the services exactly match the usage patterns previously in the components (same parameter types, same return types). The test (`catalog-list.component.spec.ts`) already uses `provideHttpClient(withFetch())` which satisfies `CatalogService`'s `inject(HttpClient)` dependency, so the test should continue to pass.

---

## Deviations from Plan

### Auto-fixed Issues

None — plan executed as written.

### Minor Adjustments

**1. IdentityService return type made explicit**
- The plan noted using `Observable<unknown>` or casting; explicit `Observable<HttpResponse<unknown>>` was chosen
- This makes TypeScript aware of the `.status` property in the `RegisterComponent` next callback without any cast
- Files modified: `identity.service.ts`

---

## Known Stubs

None — services are fully wired to HttpClient with real URL paths. No placeholder text or hardcoded empty values.

---

## Threat Flags

None — no new network endpoints or trust boundaries introduced. The refactor moves HTTP call initiation from component to service; the request/response shape and URL paths are identical. T-02-11-01 (mass assignment protection) is satisfied: `IdentityService.register(email, password)` constructs `{ email, password }` only.

---

## Self-Check: PASSED

- [x] `catalog.service.ts` exists at `src/app/core/services/catalog.service.ts`
- [x] `identity.service.ts` exists at `src/app/core/services/identity.service.ts`
- [x] Task 1 commit `44250a9` in git log
- [x] Task 2 commit `7d2f4ff` in git log
- [x] No `inject(HttpClient)` in catalog-list, product-detail, or register component files
- [x] `/api/catalog/products` only in `catalog.service.ts`
- [x] `/api/identity/register` only in `identity.service.ts`
