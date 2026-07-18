---
phase: "02"
plan: "06a"
subsystem: frontend
tags:
  - angular
  - oidc
  - angular-material
  - vitest
  - spa
dependency_graph:
  requires:
    - 02-02  # Identity service (OIDC authority at localhost:5005)
    - 02-03  # Catalog service (API at /api/catalog via gateway)
    - 02-05  # YARP gateway (proxy target at localhost:5000)
  provides:
    - Angular 20 app scaffold with OIDC, routing, proxy, and app shell
    - src/frontend/ecommerce-app/ project directory
  affects:
    - 02-06b  # Feature components plug into the shell this plan creates
tech_stack:
  added:
    - Angular 20.3 (ng new ecommerce-app, standalone, zoneless)
    - Angular Material 20.2.14 (indigo-pink prebuilt theme)
    - angular-auth-oidc-client 21.0.2 (PKCE OIDC client)
    - Vitest 3.2.6 via @analogjs/vite-plugin-angular (test runner)
    - jsdom 29.1.1 (browser environment for vitest)
  patterns:
    - provideZonelessChangeDetection() — no Zone.js
    - provideAuth() standalone functional API (not NgModule AuthModule.forRoot)
    - withInterceptors([authInterceptor()]) functional HTTP interceptor
    - OidcSecurityService.authenticated signal (built-in, not toSignal conversion)
    - @if template control flow (not *ngIf)
    - Angular Material prebuilt theme (indigo-pink.css) — M3 compatible
key_files:
  created:
    - src/frontend/ecommerce-app/src/app/app.config.ts
    - src/frontend/ecommerce-app/src/app/app.routes.ts
    - src/frontend/ecommerce-app/src/app/app.ts
    - src/frontend/ecommerce-app/src/app/app.html
    - src/frontend/ecommerce-app/src/app/app.scss
    - src/frontend/ecommerce-app/proxy.conf.json
    - src/frontend/ecommerce-app/vitest.config.ts
    - src/frontend/ecommerce-app/src/app/shared/models/product.model.ts
    - src/frontend/ecommerce-app/src/app/shared/models/product.model.spec.ts
    - src/frontend/ecommerce-app/src/app/features/auth/callback/callback.component.ts
    - src/frontend/ecommerce-app/src/app/features/auth/login/login.component.ts
    - src/frontend/ecommerce-app/src/app/features/auth/register/register.component.ts
    - src/frontend/ecommerce-app/src/app/features/catalog/catalog-list/catalog-list.component.ts
    - src/frontend/ecommerce-app/src/app/features/catalog/product-detail/product-detail.component.ts
  modified:
    - src/frontend/ecommerce-app/angular.json (polyfills=[], proxyConfig added)
    - src/frontend/ecommerce-app/package.json (test script: vitest --run)
    - src/frontend/ecommerce-app/src/styles.scss (Angular Material theme + spacing)
    - src/frontend/ecommerce-app/src/index.html (mat-typography class)
decisions:
  - "Used Angular Material 20.x (v20-lts tag) matching Angular 20 — Angular Material 22 was the npm latest but peer-deps required Angular 22+"
  - "Used prebuilt indigo-pink.css theme instead of SCSS theming API — M3 broke M2 palette APIs ($indigo-palette undefined); prebuilt theme is M3-compatible and correct for the indigo-pink UI-SPEC requirement"
  - "Used @analogjs/vite-plugin-angular (not @analogjs/vitest-angular/plugin) — vitest-angular 2.x does not export a /plugin subpath; vite-plugin-angular is the correct Vite plugin"
  - "Used OidcSecurityService.authenticated and .userData signals directly — library 21.x exposes Angular signals natively, no toSignal() conversion required"
  - "Staged package-lock.json — not in .gitignore; ensures reproducible installs for CI"
  - "Angular 20 naming convention: app.ts/app.html/app.scss (not app.component.*) — plan referenced .component suffix but Angular 20 CLI drops it for the root component"
metrics:
  duration: "12 minutes"
  completed: "2026-06-17"
  tasks_completed: 1
  tasks_total: 1
  files_created: 27
---

# Phase 02 Plan 06a: Angular Shell Scaffold Summary

Angular 20 app scaffolded at `src/frontend/ecommerce-app/` with zoneless standalone configuration, Angular Material 20 (indigo-pink theme), OIDC via `angular-auth-oidc-client` 21.x, dev proxy routing `/api/*` to YARP gateway, five route stubs ready for plan 02-06b feature components, and Vitest configured and running.

## Tasks Completed

| Task | Name | Commit | Files |
|------|------|--------|-------|
| 1 | Scaffold Angular app and configure OIDC, proxy, app shell, and Vitest | c9deae5 | 27 files |

## Verification Evidence

1. `npm run build -- --configuration development` exits 0 (1.8s build time)
2. `app.config.ts` contains `provideZonelessChangeDetection` — verified
3. `app.config.ts` authority is `http://localhost:5005` (NOT localhost:5000) — verified
4. `proxy.conf.json` target is `http://localhost:5000`, only proxies `/api` — verified
5. `app.routes.ts` defines all five routes with stub components — verified
6. `app.html` uses `@if` template control flow (not `*ngIf`) — verified
7. `vitest.config.ts` exists; `package.json` test script is `vitest --run` — verified
8. `product.model.ts` exports `Product` and `PagedResult<T>` — verified
9. `npm run test` passes 2 tests in product.model.spec.ts — verified
10. Build polyfills are empty (`[]`) — no Zone.js in production bundle

## Deviations from Plan

### Auto-fixed / Adapted Issues

**1. [Rule 1 - Bug] Angular Material 22 peer dep conflict**
- **Found during:** Task 1 (npm install)
- **Issue:** `npm install @angular/material` resolved Angular Material 22.0.1, which requires Angular CDK 22 and Angular 22+. Project is on Angular 20.
- **Fix:** Pinned Angular Material to `@20` tag (resolves to 20.2.14 v20-lts), same for `@angular/cdk@20`.
- **Files modified:** package.json (versions), package-lock.json

**2. [Rule 1 - Bug] @analogjs/vitest-angular/plugin export missing**
- **Found during:** Task 1 (vitest run)
- **Issue:** `vitest.config.ts` imported `@analogjs/vitest-angular/plugin` per an older API — vitest-angular 2.x does not export that subpath.
- **Fix:** Replaced with `@analogjs/vite-plugin-angular` (the correct Vite plugin that ships alongside vitest-angular).
- **Files modified:** vitest.config.ts

**3. [Rule 1 - Bug] jsdom missing**
- **Found during:** Task 1 (vitest run after plugin fix)
- **Issue:** vitest `environment: 'jsdom'` requires `jsdom` as a separate peer dependency not bundled with vitest.
- **Fix:** `npm install --save-dev jsdom`. Installed 29.1.1.
- **Files modified:** package.json, package-lock.json

**4. [Rule 3 - Adaptation] Angular Material SCSS theming API changed in v20**
- **Found during:** Task 1 (first build attempt)
- **Issue:** Angular Material 20 uses M3 design tokens. The M2 Sass API (`mat.$indigo-palette`, `mat.define-theme()` with M2 palettes) is gone — `mat.$indigo-palette` is undefined. Build failed.
- **Fix:** Switched to prebuilt CSS theme (`@import '@angular/material/prebuilt-themes/indigo-pink.css'`). The UI-SPEC calls for "indigo-pink (Angular Material built-in)" which maps exactly to this prebuilt theme. No functional or visual change from plan intent.
- **Files modified:** styles.scss

**5. [Rule 3 - Adaptation] Angular 20 component naming convention**
- **Found during:** Task 1 (ng new scaffold)
- **Issue:** Angular 20 CLI scaffolds the root component as `app.ts`, `app.html`, `app.scss` (dropping `.component` from the suffix). Plan referenced `app.component.ts`, `app.component.html`, `app.component.scss`.
- **Fix:** Used Angular 20 convention (`app.ts` etc.) — this is the correct generated output; the plan's filename references were based on pre-20 conventions. Functionality is identical.
- **Files:** app.ts, app.html, app.scss (not app.component.*)

**6. [Rule 3 - Adaptation] OidcSecurityService signals API used directly**
- **Found during:** Task 1 (implementation)
- **Issue:** Plan suggested `toSignal(oidcSecurityService.isAuthenticated$)` pattern. angular-auth-oidc-client 21.x already exposes `authenticated` and `userData` as native Angular signals on `OidcSecurityService`.
- **Fix:** Used `this.oidcSecurityService.authenticated` signal directly — no `toSignal()` needed. Cleaner, more idiomatic Angular 20 pattern.
- **Files modified:** app.ts

## Known Stubs

The following components are intentional stubs — plan 02-06b replaces them with full implementations:

| File | Stub Nature | Resolved In |
|------|-------------|-------------|
| `src/app/features/catalog/catalog-list/catalog-list.component.ts` | Empty template, no logic | 02-06b |
| `src/app/features/catalog/product-detail/product-detail.component.ts` | Empty template, no logic | 02-06b |
| `src/app/features/auth/login/login.component.ts` | Empty template, no logic | 02-06b |
| `src/app/features/auth/register/register.component.ts` | Empty template, no logic | 02-06b |
| `src/app/features/auth/callback/callback.component.ts` | Empty template, no logic | 02-06b |

These stubs are intentional by plan design (plan 02-06b's purpose is to replace them). The routing shell, OIDC config, proxy, and models are fully implemented.

## Threat Flags

No new threat surface found beyond what the plan's threat model already covers:
- T-02-06a-02: proxy.conf.json proxies only `/api` — OIDC at localhost:5005 goes direct (verified in app.config.ts)
- T-02-06a-03: authority set to `http://localhost:5005` (not through YARP) — verified
- T-02-06a-SC: angular-auth-oidc-client 21.x is the pre-audited package (RESEARCH.md Package Legitimacy Audit)

## Self-Check: PASSED

Files verified:
- `src/frontend/ecommerce-app/src/app/app.config.ts` — exists, contains provideZonelessChangeDetection and provideAuth
- `src/frontend/ecommerce-app/proxy.conf.json` — exists, target=localhost:5000
- `src/frontend/ecommerce-app/src/app/app.routes.ts` — exists, 5 routes defined
- `src/frontend/ecommerce-app/src/app/shared/models/product.model.ts` — exists, exports Product and PagedResult
- `src/frontend/ecommerce-app/vitest.config.ts` — exists
- Commit c9deae5 — verified in git log
