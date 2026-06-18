---
status: diagnosed
phase: 02-identity-catalog-gateway
source: [02-VERIFICATION.md]
started: 2026-06-17T00:00:00Z
updated: 2026-06-19T00:00:00Z
---

## Current Test

App shell rendering — blocked by AppHost crash

## Tests

### 1. App shell renders
expected: App shell renders with mat-toolbar, 'eCommerce' logo, 'Catalog' nav link, and 'Sign In' button; router-outlet is visible
result: failed — AppHost crashes before any service starts

### 2. Catalog browse without login
expected: Product grid loads with 'Browse Products' h1, category filter chips (Electronics, Clothing, Books, Home, Sports), and paginated product cards
result: [pending]

### 3. PKCE login flow (demo@example.com / demo123)
expected: Browser redirects to http://localhost:5005/Account/Login, then back to /callback, then to /catalog with 'Sign Out' button and username visible in toolbar
result: [pending]

### 4. User registration (/register)
expected: POST /api/identity/register returns 201, Angular navigates to /login
result: [pending]

### 5. Product detail view
expected: Navigates to /product/{id} showing product name, price, category, stock badge, disabled 'Add to Cart — Coming Soon' button, and 'Back to Catalog' link
result: [pending]

### 6. OIDC discovery endpoint (http://localhost:5005/.well-known/openid-configuration)
expected: JSON response with issuer, authorization_endpoint, token_endpoint, userinfo_endpoint fields
result: [pending]

## Summary

total: 6
passed: 0
issues: 2
pending: 5
skipped: 0
blocked: 5

## Gaps

### Gap 1: AppHost crashes on startup — cannot run any UAT
status: failed
debug_session:
  error: "System.InvalidOperationException: The endpoint 'http' for resource 'catalog' requested a proxy (IsProxied is true). Non-container resources cannot be proxied when both TargetPort and Port are specified with the same value."
  root_cause: "AppHost/Program.cs uses WithEndpoint(port: X, targetPort: X) on all project resources. In Aspire 10, setting port == targetPort on a non-container proxied resource is invalid — Aspire DCP can't bind both to the same port. Fix: replace all WithEndpoint patterns with WithHttpEndpoint(port: X) and remove the duplicate http endpoint on Payments."
  files_to_fix:
    - src/ecommerce.AppHost/Program.cs

### Gap 2: Catalog and Notifications crash standalone — null ASB connection string
status: failed
debug_session:
  error: "System.ArgumentNullException: Value cannot be null. (Parameter 'connectionString') at MassTransit.Configuration.ServiceBusHostConfigurator..ctor"
  root_cause: "Catalog.API and Notifications.API both call cfg.Host(builder.Configuration.GetConnectionString(\"messaging\")) which returns null when running outside Aspire AppHost. Aspire only injects ConnectionStrings:messaging when orchestrating via AppHost. Fix: add appsettings.Development.json to both services with a fallback emulator connection string so they can start standalone; OR document that they must be run via AppHost and ensure AppHost (Gap 1) is fixed first."
  files_to_fix:
    - src/services/catalog/ECommerce.Catalog.API/Program.cs
    - src/services/catalog/ECommerce.Catalog.API/appsettings.Development.json (new)
    - src/services/notifications/ECommerce.Notifications.API/Program.cs
    - src/services/notifications/ECommerce.Notifications.API/appsettings.Development.json (new)

### Gap 3: Catalog integration tests fail — DbInitializer runs in test host and conflicts with test-managed seeding
status: failed
debug_session:
  error: "ProductsEndpointTests fail despite Testcontainers Postgres running — suspected connection string or startup conflict"
  root_cause: "CatalogWebApplicationFactory removes MassTransit descriptors but does NOT remove DbInitializer (registered as IHostedService, not matched by the MassTransit filter). DbInitializer runs during WebApplicationFactory startup: it migrates the DB, seeds 30 products, then calls IPublishEndpoint.Publish() followed by SaveChangesAsync() through the EF outbox interceptor. The outbox interceptor writes to OutboxMessage table and the drainer may not have started yet, leaving the DB in a conflicted state. Tests then call Given_CatalogHasProducts which clears and re-seeds with a raw CatalogDbContext (no outbox), causing a mismatch. Fix: add services.RemoveAll<DbInitializer>() to CatalogWebApplicationFactory, and remove AddEntityFrameworkOutbox from the in-memory MassTransit test registration (outbox is only needed for the real ASB transport)."
  files_to_fix:
    - src/services/catalog/ECommerce.Catalog.Tests/Integration/ProductsEndpointSteps.cs
