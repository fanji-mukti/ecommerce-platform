---
phase: 02-identity-catalog-gateway
plan: "06a"
type: execute
wave: 3
depends_on:
  - 02-02
  - 02-03
  - 02-05
files_modified:
  - src/frontend/ecommerce-app/package.json
  - src/frontend/ecommerce-app/angular.json
  - src/frontend/ecommerce-app/vitest.config.ts
  - src/frontend/ecommerce-app/proxy.conf.json
  - src/frontend/ecommerce-app/src/app/app.config.ts
  - src/frontend/ecommerce-app/src/app/app.routes.ts
  - src/frontend/ecommerce-app/src/app/app.component.ts
  - src/frontend/ecommerce-app/src/app/app.component.html
  - src/frontend/ecommerce-app/src/app/app.component.scss
  - src/frontend/ecommerce-app/src/app/shared/models/product.model.ts
autonomous: true
requirements:
  - FE-01
  - FE-04

must_haves:
  truths:
    - "Angular 20 app scaffolds and builds without errors (ng build exits 0)"
    - "App uses provideZonelessChangeDetection() — no Zone.js in polyfills"
    - "OIDC is configured with provideAuth: authority points to Identity service at http://localhost:5005, clientId is ecommerce-spa, PKCE code flow"
    - "Auth interceptor attaches Bearer token to all /api/* requests via withInterceptors([authInterceptor()])"
    - "Angular dev proxy routes /api/* to YARP gateway at http://localhost:5000 (not OIDC endpoints)"
    - "Five routes are registered: /catalog, /product/:id, /login, /register, /callback"
    - "App shell (AppComponent) renders mat-toolbar with nav links and conditional Sign In / Sign Out based on auth state"
  artifacts:
    - path: "src/frontend/ecommerce-app/src/app/app.config.ts"
      provides: "Angular app configuration with OIDC, HTTP client, zoneless change detection"
      contains: "provideZonelessChangeDetection"
    - path: "src/frontend/ecommerce-app/proxy.conf.json"
      provides: "/api/* proxy to YARP gateway port 5000"
      contains: "localhost:5000"
    - path: "src/frontend/ecommerce-app/src/app/app.routes.ts"
      provides: "Five route definitions"
      contains: "catalog"
    - path: "src/frontend/ecommerce-app/src/app/shared/models/product.model.ts"
      provides: "Product and PagedResult TypeScript interfaces"
      contains: "interface Product"
    - path: "src/frontend/ecommerce-app/vitest.config.ts"
      provides: "Vitest runner configuration for Angular 20"
      contains: "vitest"
  key_links:
    - from: "app.config.ts provideAuth"
      to: "Identity service /connect/authorize"
      via: "authority: 'http://localhost:5005' (direct, not through YARP)"
      pattern: "localhost:5005"
    - from: "proxy.conf.json"
      to: "YARP gateway"
      via: "target: 'http://localhost:5000' — only /api prefix"
      pattern: "localhost:5000"
    - from: "authInterceptor"
      to: "HttpClient requests to /api/*"
      via: "withInterceptors([authInterceptor()]) in provideHttpClient"
      pattern: "authInterceptor"
---

## Phase Goal

**As a** shopper, **I want to** register, log in, and browse a product catalog through a web interface, **so that** I can discover and view products without needing direct access to backend APIs.

<objective>
Scaffold the Angular 20 frontend with Angular Material 20, configure OIDC authentication via angular-auth-oidc-client, wire the dev proxy to route /api/* through the YARP gateway, set up Vitest, implement the app shell with nav toolbar, define the five routes, and create shared TypeScript models. Feature components (CatalogList, ProductDetail, Login, Register, Callback) are implemented in plan 02-06b.

Purpose: This plan delivers the foundational Angular shell that plan 02-06b's feature components plug into. Without the OIDC config, routing, proxy, and app shell this plan creates, no feature component can function correctly.

Output: Angular 20 app scaffold at src/frontend/ecommerce-app/ with zoneless standalone configuration, Angular Material 20, provideAuth OIDC config, app shell component, five route stubs, proxy config, Vitest config, and Product model interface.
</objective>

<execution_context>
@C:\Users\User\.claude\get-shit-done\workflows\execute-plan.md
@C:\Users\User\.claude\get-shit-done\templates\summary.md
</execution_context>

<context>
@.planning/ROADMAP.md
@.planning/phases/02-identity-catalog-gateway/02-CONTEXT.md
@.planning/phases/02-identity-catalog-gateway/02-RESEARCH.md
@.planning/phases/02-identity-catalog-gateway/02-PATTERNS.md
@.planning/phases/02-identity-catalog-gateway/02-UI-SPEC.md
@.planning/phases/02-identity-catalog-gateway/02-02-SUMMARY.md
@.planning/phases/02-identity-catalog-gateway/02-03-SUMMARY.md
@.planning/phases/02-identity-catalog-gateway/02-05-SUMMARY.md

<interfaces>
<!-- Angular patterns — all from RESEARCH.md Pattern 8 and PATTERNS.md Angular section -->

app.config.ts providers:
  provideZonelessChangeDetection()
  provideRouter(routes)
  provideHttpClient(withFetch(), withInterceptors([authInterceptor()]))
  provideAuth({ config: { authority: 'http://localhost:5005', redirectUrl: 'http://localhost:4200/callback',
    postLogoutRedirectUri: 'http://localhost:4200', clientId: 'ecommerce-spa',
    scope: 'openid profile email', responseType: 'code',
    silentRenew: false, useRefreshToken: false,
    secureRoutes: ['http://localhost:4200/api'] }})

proxy.conf.json:
  { "/api": { "target": "http://localhost:5000", "secure": false, "changeOrigin": true } }

Angular 20 rules (CLAUDE.md + D-16 through D-21):
- standalone: true on ALL components — no NgModules, no BrowserModule, no CommonModule
- signals for component state (Signal<T>), not BehaviorSubject
- @if template control flow, not *ngIf
- Imports: only @angular/material/* secondary entry points the component directly uses
- provideZonelessChangeDetection() — no Zone.js

Product model interface (matching Catalog API response):
interface Product { id: string; name: string; sku: string; price: number; stockQuantity: number; category: string; imageUrl: string | null; }
Paginated response: { items: Product[]; totalCount: number; page: number; pageSize: number; }

Routes (D-20, UI-SPEC.md):
  /catalog → CatalogListComponent
  /product/:id → ProductDetailComponent
  /login → LoginComponent
  /register → RegisterComponent
  /callback → CallbackComponent
  / → redirect to /catalog
  ** → redirect to /catalog

Design system (UI-SPEC.md):
- Theme: indigo-pink (Angular Material default)
- Fonts: Roboto via Angular Material
</interfaces>
</context>

<tasks>

<task type="auto" tdd="true">
  <name>Task 1: Scaffold Angular app and configure OIDC, proxy, app shell, and Vitest</name>
  <files>
    src/frontend/ecommerce-app/package.json
    src/frontend/ecommerce-app/angular.json
    src/frontend/ecommerce-app/vitest.config.ts
    src/frontend/ecommerce-app/proxy.conf.json
    src/frontend/ecommerce-app/src/app/app.config.ts
    src/frontend/ecommerce-app/src/app/app.routes.ts
    src/frontend/ecommerce-app/src/app/app.component.ts
    src/frontend/ecommerce-app/src/app/app.component.html
    src/frontend/ecommerce-app/src/app/app.component.scss
    src/frontend/ecommerce-app/src/app/shared/models/product.model.ts
  </files>
  <read_first>
    - .planning/phases/02-identity-catalog-gateway/02-CONTEXT.md (D-16 through D-21 — scaffold decisions)
    - .planning/phases/02-identity-catalog-gateway/02-RESEARCH.md (Pattern 8 — angular-auth-oidc-client config; OQ-3 resolution — verify peer deps before install; assumption A2 about library version)
    - .planning/phases/02-identity-catalog-gateway/02-PATTERNS.md (app.config.ts pattern, proxy.conf.json pattern, Angular rules)
    - .planning/phases/02-identity-catalog-gateway/02-UI-SPEC.md (design system, nav toolbar spec, copywriting)
    - CLAUDE.md (Angular 20 rules: standalone components, signals, Vitest, provideZonelessChangeDetection)
  </read_first>
  <behavior>
    - Scaffold: run `ng new ecommerce-app --routing --style=scss --standalone --skip-tests` inside src/frontend/. If Angular CLI is not installed: `npm install -g @angular/cli@20` first.
    - Before installing angular-auth-oidc-client, run `npm info angular-auth-oidc-client peerDependencies` to verify Angular 20 compatibility. Install 20.x if Angular 20 peer deps require it; install 21.x if 21.x is declared compatible (per OQ-3 resolution).
    - Install additional packages: `npm install @angular/material @angular/cdk angular-auth-oidc-client` and `npm install --save-dev @analogjs/vitest-angular vitest`.
    - Angular Material setup: run `ng add @angular/material` selecting "Indigo/Pink" theme and "Yes" for Roboto font and browser animations.
    - app.config.ts: replace default with ApplicationConfig containing provideZonelessChangeDetection(), provideRouter(routes), provideHttpClient(withFetch(), withInterceptors([authInterceptor()])), and provideAuth config with authority=http://localhost:5005, redirectUrl=http://localhost:4200/callback, clientId=ecommerce-spa, scope="openid profile email", responseType=code, silentRenew=false, useRefreshToken=false, secureRoutes=['http://localhost:4200/api']. Remove Zone.js from polyfills if present.
    - app.routes.ts: define 5 routes per UI-SPEC.md. Use component stubs (empty standalone components) for routes that are not yet implemented — components will be replaced in plan 02-06b. Default '' redirects to /catalog. Wildcard '**' redirects to /catalog.
    - app.component.ts: standalone AppComponent with MatToolbarModule, MatButtonModule, RouterLink, RouterLinkActive, RouterOutlet imports. Uses OidcSecurityService to derive isAuthenticated signal and userData signal via toSignal(). Nav: left = "eCommerce" text link to /catalog; right = "Catalog" nav link + conditional "Sign In" (routerLink="/login") or username + "Sign Out" button (uses @if).
    - app.component.html: mat-toolbar color="primary" with left logo link and right auth slot. RouterOutlet below toolbar.
    - product.model.ts: TypeScript interface Product (id, name, sku, price, stockQuantity, category, imageUrl) and PagedResult<T> (items, totalCount, page, pageSize) matching the Catalog API response shape.
    - proxy.conf.json: proxy /api to http://localhost:5000 (YARP gateway port). DO NOT proxy /connect or /.well-known (those go directly to Identity at localhost:5005).
    - vitest.config.ts: configure @analogjs/vitest-angular for Angular 20 component testing. Add "test" script to package.json: "vitest --run" (or per @analogjs docs).
    - angular.json: ensure proxyConfig is set to proxy.conf.json for the serve configuration.
    - Note on route stubs: app.routes.ts must reference actual component classes. Create placeholder standalone components (empty templates, no logic) for CatalogListComponent, ProductDetailComponent, LoginComponent, RegisterComponent, CallbackComponent in their expected file paths under src/app/features/. Plan 02-06b replaces these with full implementations — the file paths must match exactly so 02-06b overwrites them.
  </behavior>
  <action>
    From C:\Repositories\ecommerce-platform\src\frontend\ (create the directory first if it doesn't exist):
    `ng new ecommerce-app --routing --style=scss --standalone --skip-tests`

    Then from src/frontend/ecommerce-app/:
    Check peer deps: `npm info angular-auth-oidc-client peerDependencies`
    `npm install @angular/material @angular/cdk angular-auth-oidc-client`
    `npm install --save-dev @analogjs/vitest-angular vitest`

    Run Angular Material schematics: `ng add @angular/material` — choose Indigo/Pink theme, include Roboto font.

    Replace src/app/app.config.ts with the OIDC-configured ApplicationConfig per interface context above. Remove Zone.js polyfill import if present.

    Replace src/app/app.routes.ts with 5 route stubs plus default and wildcard redirects. Create empty placeholder components at their canonical paths (src/app/features/catalog/catalog-list/catalog-list.component.ts, src/app/features/catalog/product-detail/product-detail.component.ts, src/app/features/auth/login/login.component.ts, src/app/features/auth/register/register.component.ts, src/app/features/auth/callback/callback.component.ts). Each stub: standalone: true, empty template, no imports beyond CommonModule removed.

    Create src/app/app.component.ts as a standalone component with mat-toolbar nav shell. Use toSignal() to convert OidcSecurityService observables to signals. Use @if for auth state conditional in template.

    Create proxy.conf.json at project root targeting only /api to http://localhost:5000.

    Update angular.json serve options to add "proxyConfig": "proxy.conf.json".

    Create src/app/shared/models/product.model.ts with Product and PagedResult<T> interfaces.

    Configure vitest.config.ts per @analogjs/vitest-angular documentation. Add test script to package.json.

    Verify build: `npm run build -- --configuration development`
  </action>
  <verify>
    <automated>cd src/frontend/ecommerce-app && npm run build -- --configuration development</automated>
  </verify>
  <done>
    - Angular app builds without errors (exit 0)
    - app.config.ts contains "provideZonelessChangeDetection" and "provideAuth" and "authInterceptor"
    - app.config.ts authority points to "http://localhost:5005" (Identity service directly, not gateway)
    - proxy.conf.json target is "http://localhost:5000" (YARP gateway)
    - app.routes.ts defines /catalog, /product/:id, /login, /register, /callback routes with stub components
    - No NgModules, no Zone.js import in polyfills (zoneless)
    - angular.json serve config references proxy.conf.json
    - AppComponent has mat-toolbar with primary color and @if for auth state
    - vitest.config.ts exists and is referenced in package.json test script
    - product.model.ts exports Product interface and PagedResult<T> interface
  </done>
</task>

</tasks>

<threat_model>
## Trust Boundaries

| Boundary | Description |
|----------|-------------|
| Angular SPA → YARP gateway | All /api/* calls cross here via proxy.conf.json |
| Angular SPA → Identity service | OIDC redirect flows go directly to localhost:5005 (not through YARP) |
| Session storage (browser) | JWT access token stored here — accessible by same-origin JS only |

## STRIDE Threat Register

| Threat ID | Category | Component | Disposition | Mitigation Plan |
|-----------|----------|-----------|-------------|-----------------|
| T-02-06a-01 | Information Disclosure | JWT in session storage (XSS risk) | accept | Accepted for Phase 2 — catalog browse has low sensitivity; OIDC + PKCE is correct; no HttpOnly cookie option without BFF pattern; Phase 6 can revisit |
| T-02-06a-02 | Spoofing | proxy.conf.json accidentally proxying /connect OIDC endpoints to gateway | mitigate | proxy.conf.json only proxies /api — OIDC flows (/connect, /.well-known) hit Identity directly; verified by acceptance criteria (authority=localhost:5005 not in proxy) |
| T-02-06a-03 | Tampering | Angular calling wrong OIDC authority (through YARP) | mitigate | app.config.ts authority set to "http://localhost:5005" (Identity service direct URL) — not the gateway URL (localhost:5000); acceptance criteria checks this value |
| T-02-06a-SC | Tampering | npm package installs | mitigate | angular-auth-oidc-client version pre-audited in RESEARCH.md Package Legitimacy Audit; peer dep verification step required before install (OQ-3 resolution) |
</threat_model>

<verification>
After task completes:

1. `cd src/frontend/ecommerce-app && npm run build -- --configuration development` exits 0
2. app.config.ts contains "provideZonelessChangeDetection" (no Zone.js)
3. app.config.ts authority is "http://localhost:5005" (NOT localhost:5000)
4. proxy.conf.json target is "http://localhost:5000" and only proxies /api
5. app.routes.ts defines all five routes with stub components
6. No *ngIf directives — AppComponent uses @if template control flow
7. vitest.config.ts exists and package.json has test script
8. product.model.ts exports Product and PagedResult interfaces
</verification>

<success_criteria>
- Angular app builds cleanly (development build, no TypeScript errors)
- OIDC configured against Identity service directly (not through gateway)
- Dev proxy routes only /api/* to YARP gateway at localhost:5000
- Five route stubs registered; plan 02-06b can overwrite with real implementations
- App shell (toolbar + router-outlet) renders with correct nav structure
- Vitest configured and runnable
</success_criteria>

<output>
Create .planning/phases/02-identity-catalog-gateway/02-06a-SUMMARY.md when done
</output>
