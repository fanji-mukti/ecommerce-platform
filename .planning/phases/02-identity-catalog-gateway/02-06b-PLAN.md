---
phase: 02-identity-catalog-gateway
plan: "06b"
type: execute
wave: 4
depends_on:
  - 02-06a
files_modified:
  - src/frontend/ecommerce-app/src/app/features/catalog/catalog-list/catalog-list.component.ts
  - src/frontend/ecommerce-app/src/app/features/catalog/catalog-list/catalog-list.component.html
  - src/frontend/ecommerce-app/src/app/features/catalog/catalog-list/catalog-list.component.scss
  - src/frontend/ecommerce-app/src/app/features/catalog/catalog-list/catalog-list.component.spec.ts
  - src/frontend/ecommerce-app/src/app/features/catalog/product-card/product-card.component.ts
  - src/frontend/ecommerce-app/src/app/features/catalog/product-card/product-card.component.html
  - src/frontend/ecommerce-app/src/app/features/catalog/product-detail/product-detail.component.ts
  - src/frontend/ecommerce-app/src/app/features/catalog/product-detail/product-detail.component.html
  - src/frontend/ecommerce-app/src/app/features/auth/login/login.component.ts
  - src/frontend/ecommerce-app/src/app/features/auth/login/login.component.html
  - src/frontend/ecommerce-app/src/app/features/auth/register/register.component.ts
  - src/frontend/ecommerce-app/src/app/features/auth/register/register.component.html
  - src/frontend/ecommerce-app/src/app/features/auth/callback/callback.component.ts
  - src/frontend/ecommerce-app/src/app/features/auth/callback/callback.component.html
autonomous: true
requirements:
  - FE-01
  - FE-04

must_haves:
  truths:
    - "User can navigate to /catalog and see a paginated grid of products fetched from /api/catalog/products"
    - "User can filter products by category using mat-chip-listbox"
    - "User can navigate to /product/:id and see full product detail"
    - "User can navigate to /login and be redirected to the OpenIddict PKCE authorization endpoint"
    - "User can navigate to /register and fill in email/password to create an account via POST /api/identity/register"
    - "OIDC callback at /callback completes token exchange and navigates to /catalog"
    - "CatalogListComponent Vitest test passes: component renders 'Browse Products' heading from signal state"
  artifacts:
    - path: "src/frontend/ecommerce-app/src/app/features/catalog/catalog-list/catalog-list.component.ts"
      provides: "Product grid with pagination and category filter signals"
      contains: "signal"
    - path: "src/frontend/ecommerce-app/src/app/features/catalog/catalog-list/catalog-list.component.spec.ts"
      provides: "Vitest component test for CatalogListComponent rendering"
      contains: "Browse Products"
    - path: "src/frontend/ecommerce-app/src/app/features/auth/login/login.component.ts"
      provides: "PKCE redirect trigger (no credentials form)"
      contains: "authorize"
    - path: "src/frontend/ecommerce-app/src/app/features/auth/register/register.component.ts"
      provides: "Registration form posting to /api/identity/register"
      contains: "identity/register"
    - path: "src/frontend/ecommerce-app/src/app/features/auth/callback/callback.component.ts"
      provides: "OIDC callback handler navigating to /catalog on success"
      contains: "checkAuth"
  key_links:
    - from: "CatalogListComponent"
      to: "YARP gateway /api/catalog/products"
      via: "HttpClient GET /api/catalog/products"
      pattern: "/api/catalog/products"
    - from: "LoginComponent"
      to: "OpenIddict /connect/authorize"
      via: "oidcSecurityService.authorize()"
      pattern: "authorize"
    - from: "CallbackComponent"
      to: "angular-auth-oidc-client"
      via: "checkAuth() + router.navigate"
      pattern: "checkAuth"
    - from: "RegisterComponent"
      to: "/api/identity/register"
      via: "HttpClient POST with {email, password}"
      pattern: "identity/register"
---

<objective>
Implement the six Angular feature components (CatalogListComponent, ProductCardComponent, ProductDetailComponent, LoginComponent, RegisterComponent, CallbackComponent) as full implementations — replacing the empty stubs created in plan 02-06a. Add a Vitest component test for CatalogListComponent per D-21.

Purpose: Delivers the complete user-facing vertical slice. After this plan, a user can authenticate, browse the product catalog with pagination and filtering, view product details, and register. This is the final plan in Phase 2 — all backend services from 02-02 through 02-05 must be reachable for end-to-end behavior.

Output: Six fully implemented standalone Angular components with signal-based state, Angular Material UI, and one Vitest test file proving CatalogListComponent renders the "Browse Products" heading.
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
@.planning/phases/02-identity-catalog-gateway/02-06a-SUMMARY.md

<interfaces>
<!-- From plan 02-06a — read these files after 02-06a completes -->

From src/frontend/ecommerce-app/src/app/shared/models/product.model.ts:
  interface Product { id: string; name: string; sku: string; price: number; stockQuantity: number; category: string; imageUrl: string | null; }
  interface PagedResult<T> { items: T[]; totalCount: number; page: number; pageSize: number; }

From src/frontend/ecommerce-app/src/app/app.routes.ts (stub paths set in 02-06a):
  /catalog → CatalogListComponent at features/catalog/catalog-list/catalog-list.component.ts
  /product/:id → ProductDetailComponent at features/catalog/product-detail/product-detail.component.ts
  /login → LoginComponent at features/auth/login/login.component.ts
  /register → RegisterComponent at features/auth/register/register.component.ts
  /callback → CallbackComponent at features/auth/callback/callback.component.ts

Copywriting (UI-SPEC.md):
- Catalog page h1: "Browse Products"
- Login page title: "Sign In" with subtitle "Welcome back. Sign in to continue."
- Login CTA: "Sign In" mat-raised-button color="accent" that calls oidcSecurityService.authorize()
- Register page title: "Create Account"
- /callback page: centered mat-spinner + "Completing sign in..."
- Empty state: "No products found" + "Try selecting a different category..."
- Error state: "Failed to load products" + "Retry" button

Stock badge thresholds (UI-SPEC.md):
> 10 = "In Stock" (primary color chip)
1-10 = "Low Stock" (no color attr)
0 = "Out of Stock" (disabled appearance)
</interfaces>
</context>

<tasks>

<task type="auto" tdd="true">
  <name>Task 1: Catalog feature components (CatalogList, ProductCard, ProductDetail) + Vitest test</name>
  <files>
    src/frontend/ecommerce-app/src/app/features/catalog/catalog-list/catalog-list.component.ts
    src/frontend/ecommerce-app/src/app/features/catalog/catalog-list/catalog-list.component.html
    src/frontend/ecommerce-app/src/app/features/catalog/catalog-list/catalog-list.component.scss
    src/frontend/ecommerce-app/src/app/features/catalog/catalog-list/catalog-list.component.spec.ts
    src/frontend/ecommerce-app/src/app/features/catalog/product-card/product-card.component.ts
    src/frontend/ecommerce-app/src/app/features/catalog/product-card/product-card.component.html
    src/frontend/ecommerce-app/src/app/features/catalog/product-detail/product-detail.component.ts
    src/frontend/ecommerce-app/src/app/features/catalog/product-detail/product-detail.component.html
  </files>
  <read_first>
    - src/frontend/ecommerce-app/src/app/shared/models/product.model.ts (Product and PagedResult interfaces from 02-06a)
    - src/frontend/ecommerce-app/src/app/app.routes.ts (route definitions — component paths must match exactly)
    - src/frontend/ecommerce-app/vitest.config.ts (Vitest config from 02-06a — verify test runner is configured)
    - .planning/phases/02-identity-catalog-gateway/02-UI-SPEC.md (component inventory, signal state contract, copywriting, stock badge thresholds, module import map)
    - .planning/phases/02-identity-catalog-gateway/02-PATTERNS.md (Angular component skeleton)
    - CLAUDE.md (Angular 20: standalone, signals, @if, no NgModules)
  </read_first>
  <behavior>
    CatalogListComponent (/catalog):
    - standalone: true. Imports: MatProgressBarModule, MatChipsModule, MatPaginatorModule, MatButtonModule, ProductCardComponent.
    - Signals: products = signal<Product[]>([]), isLoading = signal<boolean>(false), selectedCategory = signal<string|null>(null), currentPage = signal<number>(0), totalCount = signal<number>(0).
    - computed categories: derived from loaded products — distinct Category values plus "All" as the first option.
    - On init (ngOnInit or constructor effect): call loadProducts().
    - On category chip change: set selectedCategory signal, reset currentPage to 0, call loadProducts().
    - On page change (PageEvent): set currentPage to event.pageIndex, call loadProducts().
    - loadProducts(): set isLoading=true, build URL /api/catalog/products with page=currentPage()+1 and pageSize=12, append &category= if selectedCategory() is non-null. HttpClient.get<PagedResult<Product>>(url). On next: set products, totalCount, isLoading=false. On error: set isLoading=false.
    - Template: h1 "Browse Products" (required — Vitest test asserts this heading exists). mat-progress-bar (indeterminate) inside @if(isLoading()). mat-chip-listbox for categories. CSS grid of ProductCardComponents. mat-paginator with pageSize=12, pageSizeOptions=[12,24,48], length=totalCount(). Empty state: @if(products().length === 0 && !isLoading()) show "No products found" + "Try selecting a different category..." Empty/error states per UI-SPEC.md copywriting.

    ProductCardComponent:
    - standalone: true. Input: product = input.required<Product>() (signal input).
    - Imports: MatCardModule, MatChipModule, MatButtonModule, RouterLink, CurrencyPipe.
    - Template: mat-card appearance="outlined". Product image or grey placeholder if no imageUrl. h3 name, price formatted with CurrencyPipe USD. Stock mat-chip computed from stockQuantity (>10="In Stock", 1-10="Low Stock", 0="Out of Stock"). "View Details" mat-raised-button color="accent" [routerLink]="['/product', product().id]".

    ProductDetailComponent (/product/:id):
    - standalone: true. Imports: MatChipModule, MatButtonModule, MatProgressSpinnerModule, MatIconModule, RouterLink, CurrencyPipe, AsyncPipe.
    - Signals: product = signal<Product|null>(null), isLoading = signal<boolean>(false).
    - On init: read :id from ActivatedRoute params or inject route. HTTP GET /api/catalog/products/{id}. On next: set product. On error 404: set product to null, show 404 state.
    - Template: two-column layout. Left: image or placeholder. Right: h1 product name, category chip, price, stock badge, description, disabled "Add to Cart — Coming Soon" mat-raised-button color="primary". Back navigation: mat-button with mat-icon "arrow_back" + "Back to Catalog" routerLink="/catalog". 404 state: "Product not found" heading + back button.

    CatalogListComponent Vitest test (catalog-list.component.spec.ts):
    - Test: "renders Browse Products heading" — use TestBed.configureTestingModule from @angular/core/testing with provideHttpClient(withFetch()), provideRouter([]), provideZonelessChangeDetection(). Create component. fixture.detectChanges(). Assert compiled.querySelector('h1')?.textContent?.trim() equals "Browse Products".
    - All Angular testing must use the standalone component testing pattern (no NgModule declarations).
    - Use importProvidersFrom for any module-level requirements.

    All components: use @if template control flow (NOT *ngIf), use signals for state (NOT BehaviorSubject), standalone: true (NO NgModules).
  </behavior>
  <action>
    Create src/app/features/catalog/ directory tree.

    CatalogListComponent: inject HttpClient. Signal-based state per behavior above. loadProducts() builds URL with page+category params. Derive categories list via a computed signal from products(). Pass each product to ProductCardComponent via input() signal input. h1 must contain exactly "Browse Products" for the Vitest test to pass.

    ProductCardComponent: use input.required<Product>() for the product signal input. Compute stockLabel from product().stockQuantity using a computed signal.

    ProductDetailComponent: inject ActivatedRoute. Use route.snapshot.params['id'] or route.paramMap to read :id. Inject HttpClient. GET /api/catalog/products/{id}.

    catalog-list.component.spec.ts: write a Vitest test using Angular TestBed. Import the test from 'vitest'. Use describe/it/expect syntax compatible with @analogjs/vitest-angular. The test must import CatalogListComponent directly (standalone, no module needed). Provide HttpClient via provideHttpClient(withFetch()). Assert the h1 contains "Browse Products".

    After writing all files:
    - Verify build: `npm run build -- --configuration development`
    - Run Vitest: `npm run test -- --run`
  </action>
  <verify>
    <automated>cd src/frontend/ecommerce-app && npm run build -- --configuration development && npm run test -- --run</automated>
  </verify>
  <done>
    - CatalogListComponent.ts uses signal<Product[]>([]) for products state (not BehaviorSubject)
    - CatalogListComponent.html h1 contains exactly "Browse Products"
    - catalog-list.component.spec.ts exists and its Vitest test passes (npm run test -- --run exits 0)
    - ProductCardComponent uses input.required<Product>() signal input
    - ProductDetailComponent has disabled "Add to Cart — Coming Soon" button
    - All components use standalone: true and @if template syntax (no *ngIf)
    - Angular build exits 0 with no TypeScript compilation errors
  </done>
</task>

<task type="auto" tdd="true">
  <name>Task 2: Auth feature components (Login, Register, Callback)</name>
  <files>
    src/frontend/ecommerce-app/src/app/features/auth/login/login.component.ts
    src/frontend/ecommerce-app/src/app/features/auth/login/login.component.html
    src/frontend/ecommerce-app/src/app/features/auth/register/register.component.ts
    src/frontend/ecommerce-app/src/app/features/auth/register/register.component.html
    src/frontend/ecommerce-app/src/app/features/auth/callback/callback.component.ts
    src/frontend/ecommerce-app/src/app/features/auth/callback/callback.component.html
  </files>
  <read_first>
    - src/frontend/ecommerce-app/src/app/app.routes.ts (route definitions — component paths must match exactly)
    - src/frontend/ecommerce-app/src/app/app.config.ts (from 02-06a — OIDC authority and clientId for context)
    - .planning/phases/02-identity-catalog-gateway/02-UI-SPEC.md (Login, Register, Callback component specs and copywriting)
    - .planning/phases/02-identity-catalog-gateway/02-CONTEXT.md (D-03 angular-auth-oidc-client usage, D-18 authInterceptor, D-20 routes)
    - CLAUDE.md (Angular 20 standalone, signals, @if, ReactiveFormsModule for register form)
  </read_first>
  <behavior>
    LoginComponent (/login):
    - standalone: true. Imports: MatCardModule, MatButtonModule.
    - No form fields. Single "Sign In" mat-raised-button color="accent" full-width. On click: inject OidcSecurityService and call authorize(). Below card: "Don't have an account?" + "Register" anchor (routerLink="/register").
    - Template: centered mat-card max-width 400px. Title "Sign In", subtitle "Welcome back. Sign in to continue." Info hint: "You'll be redirected to complete sign in securely."

    RegisterComponent (/register):
    - standalone: true. Imports: MatCardModule, MatFormFieldModule, MatInputModule, MatButtonModule, MatIconModule, MatProgressSpinnerModule, ReactiveFormsModule.
    - Reactive form: email field (required, email validator), password field (required, minLength(8)), confirmPassword field (required, cross-field validator matching password). Inject FormBuilder.
    - On valid submit: HTTP POST to /api/identity/register with {email, password} (only these two fields — no other fields). On 201 success: navigate to /login. On 409 conflict: show "Email already in use" error inline. On error: show generic error.
    - Template: centered mat-card max-width 400px. Title "Create Account". Three mat-form-field fields. Submit "Create Account" mat-raised-button color="accent" full-width, disabled when form invalid or submitting. Loading: inline mat-spinner diameter=20 while submitting.
    - Security: POST body must contain ONLY email and password — no role, isAdmin, or other fields (T-02-06b-01 mitigates mass assignment).

    CallbackComponent (/callback):
    - standalone: true. Imports: MatProgressSpinnerModule.
    - On init: call OidcSecurityService.checkAuth(). On authentication success: router.navigate(['/catalog']). On error: show error state per UI-SPEC.md ("Sign-in failed" + "Try Again" button to /login).
    - Template: centered mat-spinner + "Completing sign in..." text. Error state: "Sign-in failed" heading + "Try Again" mat-button routerLink="/login".

    All components: standalone: true, @if control flow (no *ngIf), no NgModules.
  </behavior>
  <action>
    Create src/app/features/auth/ directory tree.

    LoginComponent: inject OidcSecurityService from angular-auth-oidc-client. Button calls .authorize(). No credentials form. Template per UI-SPEC.md copywriting.

    RegisterComponent: inject HttpClient and FormBuilder. Reactive form with cross-field password confirmation validator (custom validator function, not a library). POST body: { email: string, password: string } — strictly these two fields only. Handle HTTP status codes: 201 (navigate to /login), 409 (show "Email already in use" on email field), other (show generic error). isSubmitting signal controls spinner and button disabled state.

    CallbackComponent: inject OidcSecurityService and Router. checkAuth() returns Observable — subscribe in ngOnInit. Navigate to /catalog on isAuthenticated=true. Set error signal on failure.

    Wire all components in app.routes.ts — replace the empty stubs from 02-06a with these full implementations (same file paths, so the route wiring in app.routes.ts remains unchanged).

    Final build: `npm run build -- --configuration development`
  </action>
  <verify>
    <automated>cd src/frontend/ecommerce-app && npm run build -- --configuration development</automated>
  </verify>
  <done>
    - LoginComponent calls oidcSecurityService.authorize() on button click (no credentials form POST)
    - RegisterComponent POSTs to /api/identity/register with ONLY email and password in the request body
    - RegisterComponent reactive form enforces email format and password minLength(8)
    - CallbackComponent calls checkAuth() and navigates to /catalog on success
    - All three components use standalone: true and @if template syntax
    - Angular build exits 0 with no compilation errors
    - No NgModules, no Zone.js imports, no *ngIf directives in any component
  </done>
</task>

</tasks>

<threat_model>
## Trust Boundaries

| Boundary | Description |
|----------|-------------|
| Angular SPA → YARP gateway /api/* | Catalog, register API calls cross here |
| Angular SPA → Identity service /connect/* | OIDC flows go directly (bypassing YARP) |
| RegisterComponent form | User-supplied email/password — only these two fields must reach the API |

## STRIDE Threat Register

| Threat ID | Category | Component | Disposition | Mitigation Plan |
|-----------|----------|-----------|-------------|-----------------|
| T-02-06b-01 | Elevation of Privilege | RegisterComponent sending additional fields (mass assignment) | mitigate | RegisterComponent POSTs only email and password fields — no role, isAdmin, or other fields in the request body; enforced in action |
| T-02-06b-02 | Elevation of Privilege | Password field missing minLength on RegisterComponent | mitigate | RegisterComponent reactive form uses Validators.minLength(8) matching backend RegisterValidator; double-validation prevents short password submission |
| T-02-06b-03 | Spoofing | LoginComponent POSTing credentials directly instead of PKCE redirect | mitigate | LoginComponent has NO form — it calls oidcSecurityService.authorize() only; credentials never cross the SPA boundary |
| T-02-06b-SC | Tampering | npm package installs | mitigate | All packages installed in 02-06a and verified; no new packages in this plan |
</threat_model>

<verification>
After both tasks complete:

1. `cd src/frontend/ecommerce-app && npm run build -- --configuration development` exits 0
2. `npm run test -- --run` exits 0 — CatalogListComponent Vitest test passes
3. CatalogListComponent uses Signal<Product[]> (not BehaviorSubject)
4. CatalogListComponent.html h1 contains "Browse Products"
5. LoginComponent contains "authorize()" call (no credentials form POST)
6. RegisterComponent posts to "/api/identity/register" with only email + password
7. CallbackComponent contains "checkAuth()" and navigates to "/catalog"
8. ProductDetailComponent has disabled "Add to Cart — Coming Soon" button
9. No *ngIf directives — all use @if template control flow
10. All components: standalone: true, no NgModules
</verification>

<success_criteria>
- All six feature components implemented with full signal-based state and Angular Material UI
- CatalogListComponent Vitest test passes: renders "Browse Products" heading from signal state
- Angular build succeeds (no TypeScript or Angular compilation errors)
- /catalog component fetches from /api/catalog/products via HttpClient
- /login redirects to OpenIddict PKCE flow via authorize()
- /register submits only email + password to /api/identity/register
- /callback handles OIDC token exchange and navigates to /catalog
- No NgModules, no Zone.js, no *ngIf — full Angular 20 zoneless standalone signals
</success_criteria>

<output>
Create .planning/phases/02-identity-catalog-gateway/02-06b-SUMMARY.md when done
</output>
