---
status: diagnosed
phase: 02-identity-catalog-gateway
source: [02-VERIFICATION.md]
started: 2026-06-17T00:00:00Z
updated: 2026-06-19T12:00:00Z
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
issues: 5
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

### Gap 4: CatalogSeededInboxDeduplicationSteps — transport MessageId not set on both publishes, EF inbox deduplication test broken
status: failed
debug_session:
  error: "CatalogSeededConsumer_WhenSameMessageIdDeliveredTwice_InboxStateContainsExactlyOneRow — assertion 'inboxCount should be 1' fails with actual count 2"
  root_cause: "CatalogSeededInboxDeduplicationSteps.When_SameMessagePublishedTwice publishes the same message object twice without setting ctx.MessageId on either call (unlike CatalogSeededConsumerSteps which correctly passes ctx => ctx.MessageId = messageId). MassTransit auto-generates a fresh Guid for the transport-level MessageId header on each Publish call. The EF Core inbox deduplicates by transport MessageId header — not by the CatalogSeeded.MessageId record field. With two different auto-generated transport MessageIds, the inbox stores two InboxState rows and the consumer body executes twice. The assertion Then_InboxStateHasExactlyOneRow (inboxCount.Should().Be(1)) fails with count 2. Fix: pass ctx => ctx.MessageId = messageId on both Bus.Publish calls in CatalogSeededInboxDeduplicationSteps, matching the pattern in CatalogSeededConsumerSteps."
  files_to_fix:
    - src/services/notifications/ECommerce.Notifications.Tests/Integration/CatalogSeededInboxDeduplicationSteps.cs

### Gap 5: Demo user passwords don't meet ASP.NET Core Identity default complexity requirements — seed silently fails
status: failed
debug_session:
  error: "UAT test #3 (PKCE login with demo@example.com / demo123) — login page shows 'Invalid credentials' because the account was never created"
  root_cause: "Identity.API Program.cs configures AddIdentity without overriding PasswordOptions, leaving all ASP.NET Core Identity defaults active: RequireUppercase=true, RequireNonAlphanumeric=true, RequireDigit=true. Both seed passwords ('demo123' and 'admin123') fail on RequireUppercase and RequireNonAlphanumeric. DbInitializer.SeedUserIfNotExists calls um.CreateAsync(user, password) but does not check IdentityResult.Succeeded — the IdentityResult.Failed return value is silently discarded, the user is never inserted, and no exception is thrown. The Identity service starts without error but demo@example.com and admin@example.com do not exist in the database. Fix: either (a) relax PasswordOptions in Program.cs for non-production (options.Password.RequireUppercase = false; options.Password.RequireNonAlphanumeric = false) and use matching passwords in seed, or (b) change seed passwords to meet the defaults (e.g. 'Demo123!' / 'Admin123!'). Also add a check on IdentityResult in SeedUserIfNotExists and throw if !result.Succeeded."
  files_to_fix:
    - src/services/identity/ECommerce.Identity.API/Data/DbInitializer.cs
    - src/services/identity/ECommerce.Identity.API/Program.cs

### Gap 6: CatalogSeededConsumerSteps — NotificationsDbContext not registered in test DI, consumer cannot be resolved
status: failed
debug_session:
  error: "CatalogSeededConsumer_WhenSameMessageIdDeliveredTwice_ProcessesExactlyOnce — InvalidOperationException: No service for type 'ECommerce.Notifications.API.Data.NotificationsDbContext' has been registered"
  root_cause: "CatalogSeededConsumerSteps.Given_HarnessWithInMemoryTransport builds a ServiceCollection with only AddLogging() and AddMassTransitTestHarness(). CatalogSeededConsumer has a primary constructor requiring NotificationsDbContext (used for db.SaveChangesAsync). The ServiceCollection has no AddDbContext<NotificationsDbContext> registration. When MassTransit's test harness creates a scope to resolve CatalogSeededConsumer for each incoming message, DI throws InvalidOperationException. The consumer body never executes, the harness Consumed list remains empty, and Then_ConsumerBodyInvokedExactlyOnce (consumed.Should().HaveCount(1)) fails with count 0. Fix: register an in-memory NotificationsDbContext in Given_HarnessWithInMemoryTransport (services.AddDbContext<NotificationsDbContext>(o => o.UseInMemoryDatabase('test'))) so the consumer can be resolved without requiring a real Postgres container."
  files_to_fix:
    - src/services/notifications/ECommerce.Notifications.Tests/Integration/CatalogSeededConsumerSteps.cs
