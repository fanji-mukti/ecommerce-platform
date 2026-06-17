---
phase: 02-identity-catalog-gateway
reviewed: 2026-06-17T00:00:00Z
depth: standard
files_reviewed: 79
files_reviewed_list:
  - .github/workflows/ci.yml
  - src/building-blocks/Contracts/Catalog/Events/V1/CatalogSeeded.cs
  - src/building-blocks/Tests.Common/Builders/ProductBuilder.cs
  - src/building-blocks/Tests.Common/Builders/UserBuilder.cs
  - src/building-blocks/Tests.Common/ECommerce.Tests.Common.csproj
  - src/building-blocks/Tests.Common/PostgresFixture.cs
  - src/building-blocks/Tests.Common/ServiceWebApplicationFactory.cs
  - src/ecommerce.AppHost/Program.cs
  - src/ecommerce.AppHost/ecommerce.AppHost.csproj
  - src/frontend/ecommerce-app/angular.json
  - src/frontend/ecommerce-app/package.json
  - src/frontend/ecommerce-app/proxy.conf.json
  - src/frontend/ecommerce-app/src/app/app.config.ts
  - src/frontend/ecommerce-app/src/app/app.html
  - src/frontend/ecommerce-app/src/app/app.routes.ts
  - src/frontend/ecommerce-app/src/app/app.scss
  - src/frontend/ecommerce-app/src/app/app.ts
  - src/frontend/ecommerce-app/src/app/features/auth/callback/callback.component.html
  - src/frontend/ecommerce-app/src/app/features/auth/callback/callback.component.ts
  - src/frontend/ecommerce-app/src/app/features/auth/login/login.component.html
  - src/frontend/ecommerce-app/src/app/features/auth/login/login.component.ts
  - src/frontend/ecommerce-app/src/app/features/auth/register/register.component.html
  - src/frontend/ecommerce-app/src/app/features/auth/register/register.component.ts
  - src/frontend/ecommerce-app/src/app/features/catalog/catalog-list/catalog-list.component.html
  - src/frontend/ecommerce-app/src/app/features/catalog/catalog-list/catalog-list.component.scss
  - src/frontend/ecommerce-app/src/app/features/catalog/catalog-list/catalog-list.component.spec.ts
  - src/frontend/ecommerce-app/src/app/features/catalog/catalog-list/catalog-list.component.ts
  - src/frontend/ecommerce-app/src/app/features/catalog/product-card/product-card.component.html
  - src/frontend/ecommerce-app/src/app/features/catalog/product-card/product-card.component.ts
  - src/frontend/ecommerce-app/src/app/features/catalog/product-detail/product-detail.component.html
  - src/frontend/ecommerce-app/src/app/features/catalog/product-detail/product-detail.component.ts
  - src/frontend/ecommerce-app/src/app/shared/models/product.model.spec.ts
  - src/frontend/ecommerce-app/src/app/shared/models/product.model.ts
  - src/frontend/ecommerce-app/src/index.html
  - src/frontend/ecommerce-app/src/styles.scss
  - src/frontend/ecommerce-app/src/test-setup.ts
  - src/frontend/ecommerce-app/vitest.config.ts
  - src/services/catalog/ECommerce.Catalog.API/Data/CatalogDbContext.cs
  - src/services/catalog/ECommerce.Catalog.API/Data/DbInitializer.cs
  - src/services/catalog/ECommerce.Catalog.API/ECommerce.Catalog.API.csproj
  - src/services/catalog/ECommerce.Catalog.API/Features/Products/Product.cs
  - src/services/catalog/ECommerce.Catalog.API/Features/Products/ProductDto.cs
  - src/services/catalog/ECommerce.Catalog.API/Features/Products/ProductMapper.cs
  - src/services/catalog/ECommerce.Catalog.API/Features/Products/ProductsEndpoints.cs
  - src/services/catalog/ECommerce.Catalog.API/Program.cs
  - src/services/catalog/ECommerce.Catalog.Tests/ECommerce.Catalog.Tests.csproj
  - src/services/catalog/ECommerce.Catalog.Tests/Integration/ProductsEndpointSteps.cs
  - src/services/catalog/ECommerce.Catalog.Tests/Integration/ProductsEndpointTests.cs
  - src/services/catalog/ECommerce.Catalog.Tests/Unit/ProductValidationSteps.cs
  - src/services/catalog/ECommerce.Catalog.Tests/Unit/ProductValidationTests.cs
  - src/services/gateway/ECommerce.Gateway.API/ECommerce.Gateway.API.csproj
  - src/services/gateway/ECommerce.Gateway.API/Program.cs
  - src/services/gateway/ECommerce.Gateway.API/appsettings.json
  - src/services/gateway/ECommerce.Gateway.Tests/ECommerce.Gateway.Tests.csproj
  - src/services/identity/ECommerce.Identity.API/Data/DbInitializer.cs
  - src/services/identity/ECommerce.Identity.API/Data/IdentityDbContext.cs
  - src/services/identity/ECommerce.Identity.API/ECommerce.Identity.API.csproj
  - src/services/identity/ECommerce.Identity.API/Features/Authorization/AuthorizationEndpoint.cs
  - src/services/identity/ECommerce.Identity.API/Features/Profile/MeEndpoint.cs
  - src/services/identity/ECommerce.Identity.API/Features/Profile/UserProfileDto.cs
  - src/services/identity/ECommerce.Identity.API/Features/Registration/RegisterEndpoint.cs
  - src/services/identity/ECommerce.Identity.API/Features/Registration/RegisterRequest.cs
  - src/services/identity/ECommerce.Identity.API/Features/Registration/RegisterValidator.cs
  - src/services/identity/ECommerce.Identity.API/Pages/Account/Login.cshtml
  - src/services/identity/ECommerce.Identity.API/Pages/Account/Login.cshtml.cs
  - src/services/identity/ECommerce.Identity.API/Program.cs
  - src/services/identity/ECommerce.Identity.Tests/ECommerce.Identity.Tests.csproj
  - src/services/identity/ECommerce.Identity.Tests/Integration/RegisterEndpointSteps.cs
  - src/services/identity/ECommerce.Identity.Tests/Integration/RegisterEndpointTests.cs
  - src/services/identity/ECommerce.Identity.Tests/Unit/RegistrationValidatorSteps.cs
  - src/services/identity/ECommerce.Identity.Tests/Unit/RegistrationValidatorTests.cs
  - src/services/notifications/ECommerce.Notifications.API/Consumers/CatalogSeededConsumer.cs
  - src/services/notifications/ECommerce.Notifications.API/Data/DbInitializer.cs
  - src/services/notifications/ECommerce.Notifications.API/Data/NotificationsDbContext.cs
  - src/services/notifications/ECommerce.Notifications.API/ECommerce.Notifications.API.csproj
  - src/services/notifications/ECommerce.Notifications.API/Program.cs
  - src/services/notifications/ECommerce.Notifications.Tests/ECommerce.Notifications.Tests.csproj
  - src/services/notifications/ECommerce.Notifications.Tests/Integration/CatalogSeededConsumerSteps.cs
  - src/services/notifications/ECommerce.Notifications.Tests/Integration/CatalogSeededConsumerTests.cs
  - src/services/notifications/ECommerce.Notifications.Tests/Integration/CatalogSeededInboxDeduplicationSteps.cs
  - src/services/notifications/ECommerce.Notifications.Tests/Integration/CatalogSeededInboxDeduplicationTests.cs
findings:
  critical: 6
  warning: 8
  info: 4
  total: 18
status: issues_found
---

# Phase 02: Code Review Report

**Reviewed:** 2026-06-17T00:00:00Z
**Depth:** standard
**Files Reviewed:** 79
**Status:** issues_found

## Summary

Five services (Identity, Catalog, Notifications, Gateway, and the Angular SPA) plus shared infrastructure were reviewed. The implementation is architecturally sound and mostly idiomatic for the stated stack (.NET 10, MassTransit, OpenIddict, Angular 20 zoneless). However, six blockers were found that will cause incorrect runtime behaviour or expose security weaknesses before this code ships:

1. `ProductDto` omits the `Description` field, so the product detail page always renders nothing in the description section — this is a broken user-facing feature.
2. Demo seed passwords (`demo123`, `admin123`) are hardcoded in source-controlled production startup code; they will land in every deployed environment.
3. `MeEndpoint.GetMe` uses null-forgiving operators on values that can genuinely be null, silently returning `UserProfileDto("null!", "null!")`.
4. The Angular SPA hard-codes `secureRoutes` to `http://localhost:4200/api`, meaning the auth token is never attached in any non-localhost deployment (staging, production).
5. The `CatalogSeededConsumer` calls `db.SaveChangesAsync()` with no pending changes, which is non-functional — the MassTransit EF inbox requires the consumer to commit within its own outbox boundary, and this call is outside it.
6. The Payments service registers both `WithEndpoint(name: "http", ...)` and `.WithHttpEndpoint(port: 5006, name: "http")` in the AppHost — a duplicate endpoint registration that will throw an Aspire startup error.

---

## Critical Issues

### CR-01: `ProductDto` Missing `Description` — Product Detail Page Always Shows Empty Description

**File:** `src/services/catalog/ECommerce.Catalog.API/Features/Products/ProductDto.cs:3-10`

**Issue:** `ProductDto` declares seven fields (`Id`, `Name`, `Sku`, `Price`, `StockQuantity`, `Category`, `ImageUrl`) but omits `Description`. The Angular `Product` model at `product.model.ts:5` declares `description: string` as a required field. The product detail component template at `product-detail.component.html:51-56` renders `p.description.split('\n')` conditionally only when `p.description` is truthy — but because the API never sends the field, the JSON deserializer in Angular sets `description` to `undefined`, so the condition is always false and the description block is permanently invisible. Additionally, `ProductsEndpoints.cs:30` constructs `ProductDto` inline without a `Description` argument, confirming nothing is serialised.

**Fix:** Add `Description` to `ProductDto` and all its construction sites:

```csharp
// ProductDto.cs
public record ProductDto(
    Guid Id,
    string Name,
    string Sku,
    string Description,   // add this
    decimal Price,
    int StockQuantity,
    string Category,
    string? ImageUrl);

// ProductsEndpoints.cs line 30 — inline construction in Select()
.Select(p => new ProductDto(p.Id, p.Name, p.Sku, p.Description, p.Price,
    p.StockQuantity, p.Category, p.ImageUrl))

// ProductsEndpoints.cs line 41 — single product endpoint
: Results.Ok(new ProductDto(product.Id, product.Name, product.Sku, product.Description,
    product.Price, product.StockQuantity, product.Category, product.ImageUrl));
```

---

### CR-02: Hardcoded Weak Demo Passwords in Production Seed Code

**File:** `src/services/identity/ECommerce.Identity.API/Data/DbInitializer.cs:45-46`

**Issue:** Two demo accounts are seeded with plaintext passwords (`demo123`, `admin123`) that are committed to source control and will be created in every environment (local, staging, cloud) because the seeder only checks `FindByEmailAsync` — not an environment guard. `demo123` and `admin123` do not satisfy ASP.NET Core Identity's default complexity requirements (uppercase + digit + special char) so `um.CreateAsync` will actually fail silently (the result is not checked — see also CR-03 below), but the intent to create weak credentials in a deployed service is the security concern here. Even for a portfolio project this pattern trains bad habits and could be replicated into a production clone.

**Fix:** Move demo credentials to environment variables or `appsettings.Development.json` and add an environment guard:

```csharp
// Only seed demo users in Development
if (scope.ServiceProvider.GetRequiredService<IWebHostEnvironment>().IsDevelopment())
{
    var demoPassword = builder.Configuration["DemoUserPassword"]
        ?? throw new InvalidOperationException("DemoUserPassword config missing");
    await SeedUserIfNotExists(userManager, "demo@example.com", demoPassword, ct);
}
```

Also fix `SeedUserIfNotExists` to check and log the result (see WR-01).

---

### CR-03: `MeEndpoint.GetMe` Uses Null-Forgiving Operators on Values That Can Be Null

**File:** `src/services/identity/ECommerce.Identity.API/Features/Profile/MeEndpoint.cs:14`

**Issue:** The method retrieves `sub` and `email` from the claims principal and then calls `Results.Ok(new UserProfileDto(sub!, email!))`. Both `sub` and `email` can legitimately be null: the `email` claim is only present if the `email` scope was granted and the client requested it; the `sub` claim (`ClaimTypes.NameIdentifier`) may be absent if a token does not include it. The null-forgiving operator `!` suppresses the compiler warning but does not prevent a `NullReferenceException` at the `UserProfileDto` constructor when a null is passed to a `string` (non-nullable) parameter, or silently constructs a DTO with `null` values that serialize as JSON `null` — corrupting API consumers.

Additionally, the fallback `user.FindFirstValue("sub")` uses the string literal `"sub"` rather than `OpenIddictConstants.Claims.Subject` or `JwtRegisteredClaimNames.Sub`, making the fallback fragile.

**Fix:**

```csharp
public static IResult GetMe(ClaimsPrincipal user)
{
    var sub = user.FindFirstValue(ClaimTypes.NameIdentifier)
        ?? user.FindFirstValue("sub");
    var email = user.FindFirstValue(ClaimTypes.Email)
        ?? user.FindFirstValue("email");

    if (sub is null || email is null)
        return Results.Unauthorized();

    return Results.Ok(new UserProfileDto(sub, email));
}
```

---

### CR-04: `secureRoutes` Hard-Coded to `http://localhost:4200/api` — Auth Token Never Sent in Non-Local Deployments

**File:** `src/frontend/ecommerce-app/src/app/app.config.ts:23`

**Issue:** `angular-auth-oidc-client`'s `authInterceptor` only attaches the Bearer token to requests whose URL prefix matches an entry in `secureRoutes`. The value is set to `'http://localhost:4200/api'`. All API calls in the SPA use relative URLs (e.g., `/api/catalog/products`), which the browser resolves relative to the current origin. When deployed to any origin other than `http://localhost:4200` — including staging, production, or even `https://localhost:4200` — no request URL will match the prefix and the token will never be forwarded. The `/me` endpoint requires authorization; this means all authenticated API calls silently degrade to unauthenticated in any non-localhost environment.

**Fix:** Use the relative path prefix that matches how requests are actually sent:

```typescript
secureRoutes: ['/api'],
```

The library matches on prefix containment, so a relative path works and is origin-independent.

---

### CR-05: Duplicate Named Endpoint Registration for Payments in AppHost — Startup Crash

**File:** `src/ecommerce.AppHost/Program.cs:64-73`

**Issue:** The Payments project is given two endpoint registrations with the same name `"http"` on the same port `5006`:

```csharp
builder.AddProject<Projects.ECommerce_Payments_API>("payments")
    .WithEndpoint(name: "http", port: 5006, targetPort: 5006, scheme: "http", isExternal: true)
    .WithExternalHttpEndpoints()
    .WithHttpEndpoint(port: 5006, name: "http")   // duplicate
    .WithReference(serviceBus);
```

`WithEndpoint(name: "http", ...)` and `.WithHttpEndpoint(port: 5006, name: "http")` register the same named endpoint twice. Aspire throws `InvalidOperationException: An endpoint with the name 'http' already exists` at application startup. No other service in the file has this pattern; it appears to be an accidental duplication.

**Fix:** Remove the redundant `WithHttpEndpoint` call:

```csharp
builder.AddProject<Projects.ECommerce_Payments_API>("payments")
    .WithEndpoint(name: "http", port: 5006, targetPort: 5006, scheme: "http", isExternal: true)
    .WithExternalHttpEndpoints()
    .WithReference(serviceBus);
```

---

### CR-06: `CatalogSeededConsumer` Calls `db.SaveChangesAsync()` Outside the MassTransit Outbox Transaction — No-op or Data Inconsistency

**File:** `src/services/notifications/ECommerce.Notifications.API/Consumers/CatalogSeededConsumer.cs:22`

**Issue:** The consumer body calls `await db.SaveChangesAsync(context.CancellationToken)` but has made no changes to the `NotificationsDbContext` (no entities were added, modified, or removed). The call is therefore a no-op at the application level but incurs a database round-trip every message.

More importantly, the MassTransit EF outbox pattern (`UseEntityFrameworkOutbox`) wraps the consumer in its own transaction and calls `SaveChangesAsync` internally as part of committing the inbox state. Consumer code that calls `SaveChangesAsync` explicitly on the injected `DbContext` operates inside that same transaction scope in some configurations — meaning this extra call either double-commits or interferes with the outbox boundary depending on the MassTransit version's transaction handling. The intent is clearly for the outbox middleware to own the commit; removing the manual call makes the consumer correct and idiomatic.

**Fix:**

```csharp
public async Task Consume(ConsumeContext<CatalogSeeded> context)
{
    var msg = context.Message;
    logger.LogInformation(
        "CatalogSeeded received: SeedId={SeedId}, ItemCount={ItemCount}",
        msg.SeedId,
        msg.ItemCount);

    // MassTransit EF outbox middleware owns SaveChangesAsync — do not call it here.
    // Phase 2: log-only consumer; real notification logic goes here in Phase 3+.
    await Task.CompletedTask;
}
```

---

## Warnings

### WR-01: `SeedUserIfNotExists` Ignores the `IdentityResult` from `CreateAsync`

**File:** `src/services/identity/ECommerce.Identity.API/Data/DbInitializer.cs:63`

**Issue:** `await um.CreateAsync(user, password)` is called without checking the returned `IdentityResult`. ASP.NET Core Identity's default password policy requires uppercase letters, digits, and non-alphanumeric characters. The seed passwords `demo123` and `admin123` do not meet these requirements, so `CreateAsync` returns `Succeeded = false` with validation errors — but the code discards the result and silently continues. The seed appears to succeed (no exception is thrown) but the demo users are never actually created. This is a silent failure that can mislead developers.

**Fix:**

```csharp
var result = await um.CreateAsync(user, password);
if (!result.Succeeded)
{
    var errors = string.Join(", ", result.Errors.Select(e => e.Description));
    throw new InvalidOperationException(
        $"Failed to seed user '{email}': {errors}");
}
```

---

### WR-02: `RegisterEndpoint` Checks Only `DuplicateUserName` Error Code — Misses `DuplicateEmail`

**File:** `src/services/identity/ECommerce.Identity.API/Features/Registration/RegisterEndpoint.cs:30`

**Issue:** The duplicate-detection logic checks `e.Code == "DuplicateUserName"`. ASP.NET Core Identity can emit `DuplicateEmail` as a separate error code when `RequireUniqueEmail` is enabled (which is the default). If Identity emits `DuplicateEmail` instead of or in addition to `DuplicateUserName`, the 409 branch is skipped and a 400 Bad Request is returned instead, which breaks the Angular registration component's 409-specific handler that sets the `emailInUse` form error. The front-end user then sees a generic "Registration failed. Please try again." message instead of "An account with this email already exists."

**Fix:**

```csharp
var isDuplicate = result.Errors.Any(e =>
    e.Code is "DuplicateUserName" or "DuplicateEmail");
```

---

### WR-03: Integration Tests Share a Single `PostgresFixture` Instance — Tests Are Not Isolated

**File:** `src/services/catalog/ECommerce.Catalog.Tests/Integration/ProductsEndpointTests.cs:7`
**File:** `src/services/identity/ECommerce.Identity.Tests/Integration/RegisterEndpointTests.cs:6`

**Issue:** Both test classes use `IClassFixture<PostgresFixture>` which creates one Testcontainers Postgres instance shared across all tests in the class. The Catalog steps do clean up (`db.Products.RemoveRange(...)`) before seeding, but the Identity tests do not clean up between tests. The test `PostRegister_WithDuplicateEmail_Returns409` inserts `duplicate@example.com` and relies on the test running after the user was already created by `Given_UserAlreadyExists`. However, the `PostRegister_WithValidRequest_Returns201` test inserts `newuser@example.com` and never cleans it up. If any test runs more than once (e.g., via `dotnet test --repeat`) or test ordering differs, the `201` test will return `409` on repeat, causing a flaky failure.

**Fix:** Each integration test that inserts data should either use a unique email per test run (e.g., embed a `Guid` suffix) or wrap the test body in a transaction and roll back. For the Catalog tests, the existing per-step cleanup is adequate.

```csharp
// Example: generate unique email per test run
var email = $"newuser+{Guid.NewGuid():N}@example.com";
var response = await _steps.When_PostRegisterIsCalled(email, "StrongPass1!");
```

---

### WR-04: `CatalogSeededInboxDeduplicationSteps.When_SameMessagePublishedTwice` Does Not Set Transport-Level MessageId — Deduplication Test Does Not Actually Test Deduplication

**File:** `src/services/notifications/ECommerce.Notifications.Tests/Integration/CatalogSeededInboxDeduplicationSteps.cs:83-84`

**Issue:** The Postgres inbox deduplication test publishes the same message object twice without setting the transport-level `MessageId` header:

```csharp
await _harness!.Bus.Publish(message);
await _harness!.Bus.Publish(message);
```

MassTransit generates a new random `MessageId` for each `Publish` call when not overridden. The EF Core inbox deduplicates by transport `MessageId`. Because each publish gets a distinct `MessageId`, two separate `InboxState` rows are written, and the assertion `inboxCount.Should().Be(1)` will fail. This is in contrast to `CatalogSeededConsumerSteps.When_SameMessagePublishedTwice` (line 46-47) which correctly sets `ctx.MessageId = messageId` on both publishes.

**Fix:** Mirror the explicit MessageId override from the in-memory harness test:

```csharp
await _harness!.Bus.Publish(message, ctx => ctx.MessageId = messageId);
await _harness!.Bus.Publish(message, ctx => ctx.MessageId = messageId);
```

---

### WR-05: `ProductsEndpointSteps.Given_CatalogHasProductsInCategory` — `Substring(0, 3)` Throws on Short Category Names

**File:** `src/services/catalog/ECommerce.Catalog.Tests/Integration/ProductsEndpointSteps.cs:131` and `147`

**Issue:** The test helper builds SKU prefixes using `category.Substring(0, 3)`. If a caller passes a `category` or `otherCategory` string shorter than 3 characters (e.g., `"IT"`, `"Go"`), this throws `ArgumentOutOfRangeException` at runtime, crashing the test with a misleading error. The public API of this helper does not document this constraint.

**Fix:** Use `AsSpan` with a safe slice, or clamp the length:

```csharp
Sku = $"{category[..Math.Min(3, category.Length)].ToUpper()}-{i:D3}",
```

---

### WR-06: Angular `app.config.ts` Hard-Codes `authority` and `redirectUrl` to `localhost` — Breaks All Non-Local Deployments

**File:** `src/frontend/ecommerce-app/src/app/app.config.ts:15-17`

**Issue:** The OIDC `authority` (`http://localhost:5005`), `redirectUrl` (`http://localhost:4200/callback`), and `postLogoutRedirectUri` (`http://localhost:4200`) are all hardcoded. When the Angular app is built for any environment other than local dev (CI, staging, production container), the OIDC flow will attempt to contact the wrong authority and redirect to the wrong URI. This is a misconfiguration that will cause authentication to completely fail outside of a developer laptop.

**Fix:** Introduce Angular environment files (`environment.ts` / `environment.prod.ts`) and inject these values from the build configuration:

```typescript
// src/environments/environment.ts
export const environment = {
  oidcAuthority: 'http://localhost:5005',
  oidcRedirectUrl: 'http://localhost:4200/callback',
  oidcPostLogoutUri: 'http://localhost:4200',
};

// app.config.ts
import { environment } from '../environments/environment';
// ...
authority: environment.oidcAuthority,
redirectUrl: environment.oidcRedirectUrl,
```

---

### WR-07: `angular.json` Test Target Uses Karma + Zone.js While Production Uses Zoneless Vitest

**File:** `src/frontend/ecommerce-app/angular.json:97-115`

**Issue:** The `test` architect target at line 97 is configured to use `@angular/build:karma` with `zone.js` and `zone.js/testing` polyfills. The project explicitly uses `provideZonelessChangeDetection()` in `app.config.ts` and Vitest via `@analogjs/vitest-angular` (vitest.config.ts, test-setup.ts). Running `ng test` will spin up a Karma+Zone.js runner that is incompatible with the application's zoneless architecture, and the tests it runs are a different set from what `vitest` runs. This creates two divergent test surfaces, and the CI pipeline (ci.yml) runs `dotnet test` — the Angular tests are not run by CI at all (separate issue, WR-08).

**Fix:** Remove the `test` architect target from `angular.json` entirely, or replace it with a Vitest runner target. The `npm test` script already correctly delegates to Vitest:

```json
// Remove the Karma "test" architect block, or replace with:
"test": {
  "builder": "@analogjs/vitest-angular:test"
}
```

---

### WR-08: Angular Frontend Tests Not Run in CI

**File:** `.github/workflows/ci.yml:1-43`

**Issue:** The CI workflow matrix covers eleven .NET solution files. No step builds or tests the Angular frontend (`src/frontend/ecommerce-app`). The frontend has spec files (`catalog-list.component.spec.ts`, `product.model.spec.ts`) and a Vitest configuration. Because CI never runs `npm test`, failures in Angular code (including the CR-01 type mismatch that would surface if a TypeScript compile check were added) go undetected in the CI gate.

**Fix:** Add an Angular CI job:

```yaml
frontend:
  runs-on: ubuntu-latest
  defaults:
    run:
      working-directory: src/frontend/ecommerce-app
  steps:
    - uses: actions/checkout@v4
    - uses: actions/setup-node@v4
      with:
        node-version: '22'
        cache: 'npm'
        cache-dependency-path: src/frontend/ecommerce-app/package-lock.json
    - run: npm ci
    - run: npm test
```

---

## Info

### IN-01: `ProductMapper` (Mapperly) Is Declared But Never Used in Endpoints

**File:** `src/services/catalog/ECommerce.Catalog.API/Features/Products/ProductMapper.cs`

**Issue:** A source-generated `ProductMapper` class is defined with a `ToDto(Product) -> ProductDto` method. The actual endpoint code in `ProductsEndpoints.cs` constructs `ProductDto` inline using the constructor directly, bypassing the mapper entirely. The mapper is dead code that adds a build artefact without being exercised. If `ProductDto` grows additional fields, developers may update the constructor call but forget the mapper (or vice versa), creating drift.

**Fix:** Either use the mapper in the endpoints or remove it. Using it:

```csharp
// Inject ProductMapper and use it
var mapper = new ProductMapper();
// ...
.Select(p => mapper.ToDto(p))
```

Or remove `ProductMapper.cs` until it is actually needed.

---

### IN-02: `DbInitializer` (Catalog) Uses `Guid.NewGuid()` Seed Ids — Products Get New IDs on Every Restart

**File:** `src/services/catalog/ECommerce.Catalog.API/Data/DbInitializer.cs:42-79`

**Issue:** Every product is seeded with `Id = Guid.NewGuid()`. The seeder is idempotent only because it bails early if any products exist (`if (await db.Products.AnyAsync(ct)) return`). However, if the database is wiped and the service restarted (a common local dev operation), all products get new GUIDs. Any bookmarked product detail URL (`/product/{old-guid}`) will return 404. For a catalog this is a minor inconvenience, but it is worth noting for future phases where other services may store catalog IDs.

**Fix:** Use deterministic, fixed GUIDs for seed data so IDs are stable across restarts:

```csharp
new() { Id = new Guid("a1000000-0000-0000-0000-000000000001"), Name = "Wireless Noise-Cancelling Headphones", ... }
```

---

### IN-03: `zone.js` Listed as a Runtime Dependency in `package.json`

**File:** `src/frontend/ecommerce-app/package.json:37`

**Issue:** `"zone.js": "~0.15.0"` is listed under `dependencies` (production), but the application uses `provideZonelessChangeDetection()`. Zone.js is not needed at runtime for a zoneless Angular app. Keeping it as a production dependency increases the bundle size and contradicts the project's explicit decision to use zoneless change detection (CLAUDE.md).

**Fix:** Move `zone.js` to `devDependencies` if it is still needed for the Karma test runner (itself deprecated per project conventions), or remove it entirely once the Karma test target is removed.

---

### IN-04: `CatalogSeededConsumer` Injects `NotificationsDbContext` But Has No Entity Operations Planned

**File:** `src/services/notifications/ECommerce.Notifications.API/Consumers/CatalogSeededConsumer.cs:7`

**Issue:** The consumer declares `NotificationsDbContext db` as a constructor parameter, but the only usage is the erroneous `db.SaveChangesAsync()` call (covered in CR-06). Injecting the DbContext when no entities are read or written is misleading — it suggests persistent state operations that do not exist. When Phase 3 adds real notification logic, this will be needed; for now it is an unused dependency that adds a scoped EF Core context to every message dispatch.

**Fix (deferred until Phase 3):** Remove the `db` parameter from the constructor for now and re-add it when notification persistence is implemented.

```csharp
public class CatalogSeededConsumer(ILogger<CatalogSeededConsumer> logger)
    : IConsumer<CatalogSeeded>
```

---

_Reviewed: 2026-06-17T00:00:00Z_
_Reviewer: Claude (gsd-code-reviewer)_
_Depth: standard_
