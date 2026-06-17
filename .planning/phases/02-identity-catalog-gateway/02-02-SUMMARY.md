---
phase: 02-identity-catalog-gateway
plan: 02
subsystem: identity
tags: [openiddict, asp-net-identity, oidc, pkce, ef-core, razor-pages, xunit-v3]
dependency_graph:
  requires: [02-01]
  provides: [identity-service, oidc-server, user-registration, login-ui, ef-migrations]
  affects: [02-03-catalog, 02-05-gateway, 02-06-angular]
tech_stack:
  added:
    - Microsoft.AspNetCore.Identity.EntityFrameworkCore 10.0.9
    - OpenIddict.AspNetCore 7.5.0
    - OpenIddict.EntityFrameworkCore 7.5.0
    - Npgsql.EntityFrameworkCore.PostgreSQL 10.0.0
    - Microsoft.EntityFrameworkCore.Design 10.0.0
    - FluentValidation 11.4.0 (11.3.1 requested, 11.4.0 resolved — same major)
    - Riok.Mapperly 4.3.1
    - xunit.v3 3.2.2
    - FluentAssertions 8.10.0
    - NSubstitute 5.3.0
    - Microsoft.AspNetCore.Mvc.Testing 10.0.9
  patterns:
    - OpenIddict PKCE auth code flow with authorization endpoint passthrough
    - ASP.NET Core Identity with EF Core stores + lockout (MaxFailedAccessAttempts=5)
    - IHostedService DbInitializer seeding OpenIddict client + demo users
    - Minimal API endpoints with FluentValidation injection
    - Razor Pages login form with SignInManager
    - xUnit v3 two-class test pattern (Tests + Steps)
key_files:
  created:
    - src/services/identity/ECommerce.Identity.API/Data/IdentityDbContext.cs
    - src/services/identity/ECommerce.Identity.API/Data/DbInitializer.cs
    - src/services/identity/ECommerce.Identity.API/Migrations/20260617070516_InitialCreate.cs
    - src/services/identity/ECommerce.Identity.API/Migrations/20260617070516_InitialCreate.Designer.cs
    - src/services/identity/ECommerce.Identity.API/Migrations/IdentityDbContextModelSnapshot.cs
    - src/services/identity/ECommerce.Identity.API/Features/Registration/RegisterRequest.cs
    - src/services/identity/ECommerce.Identity.API/Features/Registration/RegisterValidator.cs
    - src/services/identity/ECommerce.Identity.API/Features/Registration/RegisterEndpoint.cs
    - src/services/identity/ECommerce.Identity.API/Features/Profile/UserProfileDto.cs
    - src/services/identity/ECommerce.Identity.API/Features/Profile/MeEndpoint.cs
    - src/services/identity/ECommerce.Identity.API/Features/Authorization/AuthorizationEndpoint.cs
    - src/services/identity/ECommerce.Identity.API/Pages/Account/Login.cshtml
    - src/services/identity/ECommerce.Identity.API/Pages/Account/Login.cshtml.cs
    - src/services/identity/ECommerce.Identity.Tests/ECommerce.Identity.Tests.csproj
    - src/services/identity/ECommerce.Identity.Tests/Unit/RegistrationValidatorTests.cs
    - src/services/identity/ECommerce.Identity.Tests/Unit/RegistrationValidatorSteps.cs
    - src/services/identity/ECommerce.Identity.Tests/Integration/RegisterEndpointTests.cs
    - src/services/identity/ECommerce.Identity.Tests/Integration/RegisterEndpointSteps.cs
  modified:
    - src/services/identity/ECommerce.Identity.API/ECommerce.Identity.API.csproj
    - src/services/identity/ECommerce.Identity.API/Program.cs
    - src/services/identity/Identity.sln
decisions:
  - Used Npgsql.EntityFrameworkCore.PostgreSQL 10.0.0 directly instead of Aspire.Npgsql.EntityFrameworkCore.PostgreSQL — the Aspire package uses AddNpgsqlDbContext() which requires Aspire AppHost; the Identity service uses AddDbContext() directly, so the plain Npgsql EF Core package is the correct choice for non-Aspire registration
  - OpenIddict 7.5.0 API uses SetUserInfoEndpointUris (capital I) and SetEndSessionEndpointUris (not SetLogoutEndpointUris) — corrected from PATTERNS.md documentation which used slightly different casing
  - OpenIddictApplicationDescriptor uses ClientType (not Type) — corrected from RESEARCH.md pattern
  - Claim destinations use SetDestinations() on individual Claim objects (not the 4-argument AddClaim) — OpenIddict v7 extension method signature differs from PATTERNS.md
  - xUnit v3 requires OutputType=Exe — added to test project to satisfy xUnit.net v3 MTP requirement
  - MapGet("connect/authorize") cast to (Delegate) to suppress ASP0016 warning about Task<IResult> return type
metrics:
  duration: "~25 minutes"
  completed: "2026-06-17"
  tasks_completed: 2
  files_created: 18
  files_modified: 3
---

# Phase 02 Plan 02: Identity Service — OIDC + Registration + Profile Summary

**One-liner:** ASP.NET Core Identity + OpenIddict 7.5.0 PKCE auth code flow with Razor Pages login, user registration endpoint, profile endpoint, EF Core migrations, and xUnit v3 test project.

## What Was Built

### Task 1: EF Core + OpenIddict + Identity setup
- Expanded `ECommerce.Identity.API.csproj` with Identity, OpenIddict, Npgsql, FluentValidation, Mapperly, EFCore.Design packages
- Created `IdentityDbContext` extending `IdentityDbContext<IdentityUser>` with OpenIddict tables auto-registered via `UseEntityFrameworkCore()`
- Created `DbInitializer` (IHostedService) that applies EF Core migrations, seeds the `ecommerce-spa` OpenIddict public client, and seeds `demo@example.com` + `admin@example.com` demo users idempotently
- Expanded `Program.cs` to wire AddIdentity (lockout MaxFailedAccessAttempts=5), AddOpenIddict (PKCE auth code flow, development certs, all endpoint passthroughs), AddCors (explicit origin `http://localhost:4200`), AddRazorPages, RegisterFluentValidation validators, AddHostedService<DbInitializer>
- Middleware order: `UseCors()` → `UseAuthentication()` → `UseAuthorization()` (ASVS T-02-02-07)
- Ran `dotnet ef migrations add InitialCreate` — migration in `Migrations/` folder

### Task 2: Feature endpoints + Razor Pages login + test project
- `RegisterRequest`: record with Email and Password
- `RegisterValidator`: FluentValidation with Email format + Password MinimumLength(8) (ASVS T-02-02-05)
- `RegisterEndpoint`: POST /register — validates, creates user, returns 201/409/400 (no stack traces exposed, T-02-02-06)
- `UserProfileDto`: record with Sub and Email
- `MeEndpoint`: GET /me — extracts email + sub claims, returns UserProfileDto
- `AuthorizationEndpoint`: GET /connect/authorize — checks Identity cookie, redirects unauthenticated to login, builds claims principal with SetDestinations, returns SignIn(principal, OpenIddictServerAspNetCoreDefaults.AuthenticationScheme)
- `Login.cshtml` + `Login.cshtml.cs`: Razor Page form with SignInManager.PasswordSignInAsync(lockoutOnFailure: true)
- `ECommerce.Identity.Tests`: xUnit v3 test project with Unit/ and Integration/ subdirs added to Identity.sln

## Verification Results

- `dotnet build src/services/identity/Identity.sln --configuration Release` → **Build succeeded, 0 errors**
- `dotnet run` (unit tests only, -class filter) → **4/4 unit tests pass**
- `RequireProofKeyForCodeExchange()` present in Program.cs ✓
- `UseCors()` before `UseAuthentication()` ✓
- `DbInitializer.cs` contains `ecommerce-spa` and `demo@example.com` ✓
- `RegisterValidator` enforces `MinimumLength(8)` ✓
- `Login.cshtml.cs` calls `PasswordSignInAsync` with `lockoutOnFailure: true` ✓
- `MapGet("/me", ...).RequireAuthorization()` present ✓

## Deviations from Plan

### Auto-fixed Issues

**1. [Rule 1 - Bug] OpenIddict 7.5.0 API differences from PATTERNS.md**
- **Found during:** Task 1 build errors
- **Issue:** PATTERNS.md used `SetUserinfoEndpointUris` (lowercase i) and `EnableUserinfoEndpointPassthrough` (lowercase i), but OpenIddict 7.5.0 actual API uses `SetUserInfoEndpointUris` (capital I) and `EnableUserInfoEndpointPassthrough`. Also `SetLogoutEndpointUris` does not exist — the correct method is `SetEndSessionEndpointUris` (OIDC end_session_endpoint).
- **Fix:** Updated Program.cs to use correct method names
- **Files modified:** `Program.cs`
- **Commit:** 0a3eed2

**2. [Rule 1 - Bug] OpenIddictApplicationDescriptor.Type → ClientType**
- **Found during:** Task 1 build errors
- **Issue:** RESEARCH.md pattern used `Type = ClientTypes.Public` but the actual property is `ClientType = ClientTypes.Public`
- **Fix:** Updated DbInitializer.cs
- **Files modified:** `Data/DbInitializer.cs`
- **Commit:** 0a3eed2

**3. [Rule 1 - Bug] ClaimsIdentity.AddClaim 4-argument overload does not exist**
- **Found during:** Task 1 build errors (AuthorizationEndpoint.cs)
- **Issue:** PATTERNS.md showed `identity.AddClaim(Claims.Subject, value, Destinations.AccessToken, Destinations.IdentityToken)` with 4 args. The OpenIddict extension only has 3-arg form (name, value, single-destination). Multiple destinations require `claim.SetDestinations()` on an individual `Claim` object.
- **Fix:** Rewrote AuthorizationEndpoint to create `new Claim(...)` and call `SetDestinations()` before `identity.AddClaim(claim)`
- **Files modified:** `Features/Authorization/AuthorizationEndpoint.cs`
- **Commit:** 0a3eed2

**4. [Rule 1 - Bug] MigrateAsync not found without relational package**
- **Found during:** Task 1 build errors (DbInitializer.cs)
- **Issue:** `Aspire.Npgsql.EntityFrameworkCore.PostgreSQL` was originally specified in the plan, but using `AddDbContext<IdentityDbContext>()` (not `builder.AddNpgsqlDbContext()`) means the plain Npgsql EF Core provider is needed. Added `using Microsoft.EntityFrameworkCore` to DbInitializer.cs.
- **Fix:** Replaced `Aspire.Npgsql.EntityFrameworkCore.PostgreSQL` with `Npgsql.EntityFrameworkCore.PostgreSQL 10.0.0` + `Microsoft.EntityFrameworkCore.Design 10.0.0` in csproj; added using to DbInitializer.cs
- **Files modified:** `ECommerce.Identity.API.csproj`, `Data/DbInitializer.cs`
- **Commit:** 0a3eed2

**5. [Rule 1 - Bug] xUnit v3 requires OutputType=Exe**
- **Found during:** Task 2 build error
- **Issue:** xUnit v3 (v3.2.2) requires test projects to be executables (`<OutputType>Exe</OutputType>`); the build failed with "xUnit.net v3 test projects must be executable"
- **Fix:** Added `<OutputType>Exe</OutputType>` to test project
- **Files modified:** `ECommerce.Identity.Tests/ECommerce.Identity.Tests.csproj`
- **Commit:** 4232130

### Noted Behavioral Differences

- **FluentValidation version:** 11.3.1 was requested but NuGet resolved 11.4.0 (latest compatible). This is a harmless minor version bump with no breaking changes.
- **Migrations location:** EF Core placed migrations in `Migrations/` (project root sibling) rather than `Data/Migrations/`. This is EF Core's default behavior when no `--output-dir` is specified. The plan's done criteria says "Data/Migrations/ directory created" but EF Core tooling creates `Migrations/` by default. Functionally equivalent.
- **Integration tests:** Docker is not available in the current environment, so the 3 integration tests (RegisterEndpoint) fail at class instantiation (PostgresFixture cannot start Testcontainers). This is expected — integration tests require Docker. Unit tests all pass.
- **`dotnet test` incompatibility:** xUnit v3 uses Microsoft Testing Platform (MTP) and doesn't work with `dotnet test --filter` in the standard way. Tests are run via `dotnet run` which executes the test executable directly. All 4 unit tests pass.

## Known Stubs

None — all endpoints are fully wired. The login UI is functional Razor Pages. The seeder is fully implemented. No stub values flow to UI rendering.

## Threat Flags

No new security surface introduced beyond what was in the plan's threat model.

## Self-Check: PASSED

- IdentityDbContext.cs: FOUND ✓
- DbInitializer.cs: FOUND ✓
- RegisterEndpoint.cs: FOUND ✓
- AuthorizationEndpoint.cs: FOUND ✓
- Login.cshtml: FOUND ✓
- Login.cshtml.cs: FOUND ✓
- ECommerce.Identity.Tests.csproj: FOUND ✓
- Migrations/20260617070516_InitialCreate.cs: FOUND ✓
- Commit 0a3eed2: FOUND ✓
- Commit 4232130: FOUND ✓
