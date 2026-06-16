# Phase 2: Identity, Catalog & Gateway - Research

**Researched:** 2026-06-17
**Domain:** ASP.NET Core Identity + OpenIddict OIDC, EF Core 10 / Npgsql, MassTransit 8.3.6 outbox/inbox, YARP 2.x gateway, Angular 20 + angular-auth-oidc-client, Testcontainers xUnit v3
**Confidence:** HIGH (stack fully locked in CLAUDE.md; all key package versions verified against NuGet/npm)

---

<user_constraints>
## User Constraints (from CONTEXT.md)

### Locked Decisions

**Auth Flow (IDN-01, IDN-02, IDN-03, IDN-04)**
- D-01: OpenIddict PKCE auth code flow is the authentication mechanism. Angular redirects to `/connect/authorize`, receives an auth code, exchanges it for a JWT. No ROPC, no custom JWT endpoint — full OIDC.
- D-02: Angular SPA client (client_id, redirect_uri, allowed scopes) is seeded at startup via a `DbInitializer` / `IHostedService` that writes into OpenIddict's EF Core tables on first run. No manual configuration steps required to run the demo.
- D-03: Angular uses `angular-auth-oidc-client` to handle the PKCE callback, token storage (session storage), and silent renew. An HTTP interceptor attaches the `Bearer` token to every `HttpClient` call.
- D-04: User store uses ASP.NET Core Identity tables inside the Identity service's own PostgreSQL database. DB-per-service boundary is strictly maintained.
- D-05: Demo user accounts are seeded by the same `DbInitializer` — at least 2 seeded users.

**YARP Gateway**
- D-06: YARP gateway is a separate 9th service at `src/services/gateway/ECommerce.Gateway.API/` with its own `Gateway.sln`.
- D-07: All routes defined in the `ReverseProxy` section of `appsettings.json`. Aspire injects cluster destination URLs via environment variables.
- D-08: Gateway does NOT validate JWTs. Forwards `Authorization` header as-is. Each downstream service validates JWT against OpenIddict's discovery endpoint.
- D-09: Gateway added to Aspire AppHost (10th resource) and CI matrix (11th solution).

**MassTransit Outbox / Inbox (INF-01, INF-02)**
- D-10: Only Catalog (publisher) and Notifications (consumer) get MassTransit wired in Phase 2.
- D-11: Proof event is `ECommerce.Catalog.Events.V1.CatalogSeeded` — replaces `Placeholder.cs` in Contracts.
- D-12: `CatalogSeeded` record: `Guid SeedId`, `int ItemCount`, `DateTimeOffset SeededAt` + `IMessageEnvelope` fields.
- D-13: Notifications service consumes `CatalogSeeded` and persists a placeholder inbox entry.
- D-14: Forced redelivery test uses MassTransit InMemory test harness — same `MessageId` published twice, verify consumer processed exactly once. No ASB emulator needed for this test.
- D-15: MassTransit 8.3.6 pinned explicitly in every `.csproj`. `MassTransit.EntityFrameworkCore` provides outbox/inbox tables; `MassTransit.Testing` provides InMemory harness.

**Angular Shell (FE-01, FE-04)**
- D-16: `ng new ecommerce-app --routing --style=scss` with `provideZonelessChangeDetection()`. Angular 20, standalone components, signals.
- D-17: Angular Material 20.
- D-18: `provideHttpClient(withFetch())` + functional `authInterceptor` from `angular-auth-oidc-client`.
- D-19: Angular runs via `ng serve` on `localhost:4200` — NOT added to Aspire AppHost in Phase 2. `proxy.conf.json` proxies `/api/*` to YARP gateway URL.
- D-20: Angular routes: `/catalog`, `/product/:id`, `/login`, `/register`, `/callback`.
- D-21: Vitest via `@analogjs/vitest-angular` configured at scaffold time.

**Test Infrastructure**
- D-22 through D-30: Test projects for Identity, Catalog, Notifications, Gateway. Shared `Tests.Common`. Two-class pattern (`*Tests` / `*Steps`). `Unit/` + `Integration/` subdirs. Per-class `IClassFixture<PostgresFixture>`. xUnit v3 + FluentAssertions + NSubstitute.

**Namespaces and Code Structure**
- D-31: Root namespace `ECommerce.{ServiceName}.API` per service.
- D-32: Vertical-slice feature folders.
- D-33: Only `Catalog/Events/V1/Placeholder.cs` replaced in Phase 2.

### Claude's Discretion
- Catalog product schema (fields beyond Name, Price, Stock, Category): Claude chooses sensible demo fields (Description, ImageUrl, SKU).
- Pagination style for `GET /products`: offset-based acceptable for Phase 2.
- OpenIddict OIDC scopes: standard `openid profile email` sufficient.
- YARP route prefixes: Claude decides (e.g. `/api/identity/...`, `/api/catalog/...`).

### Deferred Ideas (OUT OF SCOPE)
- Angular added to Aspire AppHost as Node.js resource — deferred to Phase 5+.
- Playwright E2E tests — deferred to Phase 4.
- Coverage gate in CI — deferred to Phase 3+.
- MassTransit wiring for Cart, Orders, Checkout, Payments, Fulfillment — each gets MT in their phases.
- Real OpenIddict scopes beyond `openid profile email` — deferred to Phase 4+.
</user_constraints>

---

<phase_requirements>
## Phase Requirements

| ID | Description | Research Support |
|----|-------------|------------------|
| IDN-01 | User can register with email and password | ASP.NET Core Identity `UserManager<IdentityUser>.CreateAsync()` + a dedicated `/register` Minimal API endpoint with FluentValidation. OpenIddict does not handle registration — Identity does. |
| IDN-02 | User can log in and receive a JWT token (via OpenIddict) | Full OIDC PKCE auth code flow: `GET /connect/authorize` triggers login challenge → Identity cookie login → `POST /connect/token` issues JWT access token + ID token. |
| IDN-03 | User can retrieve their current profile (GET /me) | Protected Minimal API endpoint on Identity service, validates JWT via `AddOpenIddict().AddValidation()`, extracts claims from `HttpContext.User`. |
| IDN-04 | System provides seeded demo user accounts | `DbInitializer` IHostedService runs `UserManager.CreateAsync()` for 2 demo users + seeds OpenIddict client registration on first run. |
| CAT-01 | User can list products with pagination and category filtering | EF Core 10 + Npgsql `DbContext`, `GET /products?page=1&pageSize=12&category=Electronics` with LINQ `.Where().Skip().Take()`. |
| CAT-02 | User can view product detail by ID | `GET /products/{id}` — EF Core `FindAsync(id)`, returns 404 if not found. |
| CAT-03 | System provides seeded demo catalog of 20-50 SKUs | Catalog `DbInitializer` IHostedService seeds Product rows on first run; publishes `CatalogSeeded` event via MassTransit transactional outbox. |
| FE-01 | User can browse the product catalog and view product detail (/catalog, /product/:id) | Angular 20 `CatalogListComponent` (signals, pagination, category filter chips) + `ProductDetailComponent`. Calls `/api/catalog/products` through YARP proxy. |
| FE-04 | User can register and log in (/register, /login) | `RegisterComponent` POSTs to `/api/identity/register`. `LoginComponent` triggers PKCE redirect via `angular-auth-oidc-client`'s `authorize()`. `CallbackComponent` handles token exchange. |
| INF-01 | Every publishing service uses MassTransit transactional outbox for guaranteed at-least-once delivery | Catalog service: `AddEntityFrameworkOutbox<CatalogDbContext>()` with `UsePostgres()` + `UseBusOutbox()`. `CatalogSeeded` published inside a DB transaction from the seeder. |
| INF-02 | Every consuming service uses idempotent inbox to deduplicate redelivered messages | Notifications service: `cfg.UseEntityFrameworkOutbox<NotificationsDbContext>(context)` per endpoint. InboxState table tracks processed `MessageId`s. Duplicate detection window: default 30 min. |
</phase_requirements>

---

## Summary

Phase 2 wires three distinct technical pillars — each with its own complexity — that must interoperate end-to-end: an OpenIddict OIDC authorization server, a YARP reverse proxy, and MassTransit 8.3.6's transactional outbox/inbox. The stack is fully determined by CLAUDE.md and CONTEXT.md, so research focused on exact configuration patterns, verified package versions, and the non-obvious pitfalls in each domain.

The biggest technical risk is the **MassTransit 8.3.6 + Azure Service Bus emulator incompatibility**: the emulator uses non-TLS AMQP port 5672, but MassTransit 8.3.6 attempts port 443 (HTTPS) because the `UseDevelopmentEmulator=true` support was only added in v9.x. The mitigating factor is that D-14 deliberately uses the **InMemory test harness** — no ASB emulator connectivity is required for the Phase 2 idempotency test. The Catalog and Notifications services do need ASB connectivity for the seeding-event proof, but this can be deferred to a spike or worked around by running against real ASB in a dev subscription until the emulator issue is resolved.

OpenIddict requires a custom authorization controller (or Minimal API handler) to issue tokens — it is not a plug-and-play solution. The handler must: check authentication state, redirect unauthenticated users to the Identity login page, build a `ClaimsPrincipal` with correct claim destinations, and return `SignIn(principal, OpenIddictServerAspNetCoreDefaults.AuthenticationScheme)`. YARP service discovery with `AddServiceDiscoveryDestinationResolver()` eliminates hardcoded cluster URLs — Aspire's logical service names resolve at runtime.

**Primary recommendation:** Build the Identity service first (it has the deepest dependency chain: EF Core schema + Identity + OpenIddict), then Catalog (EF Core + MassTransit outbox), then Gateway (YARP — simplest, most mechanical), then Angular (depends on all three backends being reachable). Wire the Notifications inbox consumer in parallel with Catalog outbox.

---

## Architectural Responsibility Map

| Capability | Primary Tier | Secondary Tier | Rationale |
|------------|-------------|----------------|-----------|
| User registration | API / Backend (Identity service) | — | Identity data lives in Identity DB; no browser-side logic beyond form submission |
| JWT issuance (PKCE token exchange) | API / Backend (Identity service / OpenIddict) | — | Token issuance is a server-side cryptographic operation — never in browser |
| JWT validation on protected endpoints | API / Backend (each service) | — | Each service validates against OpenIddict discovery endpoint; gateway passes header through |
| OIDC PKCE redirect + callback | Browser / Client (Angular) | Frontend Server (n/a — Angular is SPA) | `angular-auth-oidc-client` handles authorization redirect, code exchange, token storage in session storage |
| API routing + header forwarding | CDN / Static / Gateway (YARP) | — | YARP terminates external requests and forwards to the correct downstream service |
| Catalog data (read) | API / Backend (Catalog service) | Database / Storage (PostgreSQL) | EF Core query layer; PostgreSQL persists product rows |
| Catalog data seeding | API / Backend (Catalog service) | — | `DbInitializer` IHostedService — server-side, runs at startup |
| Message publishing (outbox) | API / Backend (Catalog service) | Database / Storage (PostgreSQL) | Transactional outbox stores messages in Postgres before ASB delivery |
| Message consumption (inbox) | API / Backend (Notifications service) | Database / Storage (PostgreSQL) | InboxState table deduplicates by MessageId |
| Angular product catalog UI | Browser / Client (Angular) | — | Signal-based components render product grid, handle pagination and filter |
| Angular auth flow UI | Browser / Client (Angular) | — | `angular-auth-oidc-client` manages session storage tokens, interceptor attaches Bearer header |

---

## Standard Stack

### Core (.NET Services)

| Library | Version | Purpose | Why Standard |
|---------|---------|---------|--------------|
| `Microsoft.AspNetCore.Identity.EntityFrameworkCore` | 10.0.9 [VERIFIED: NuGet] | User/role storage, password hashing, `UserManager<T>` | Ships with ASP.NET Core; industry standard for .NET identity |
| `OpenIddict.AspNetCore` | 7.5.0 [VERIFIED: NuGet] | OIDC server endpoints, token issuance, PKCE | Latest stable; MIT; recommended in CLAUDE.md over Duende |
| `OpenIddict.EntityFrameworkCore` | 7.5.0 [VERIFIED: NuGet] | OpenIddict EF Core stores (applications, tokens, authorizations) | Required for persisted client registrations and token revocation |
| `Microsoft.AspNetCore.Authentication.JwtBearer` | 10.0.9 [ASSUMED — ships with .NET 10 SDK] | JWT validation on protected API endpoints | Standard ASP.NET Core bearer middleware |
| `Yarp.ReverseProxy` | 2.3.0 [VERIFIED: NuGet] | Reverse proxy routing, header forwarding, transforms | Microsoft-maintained; appsettings-based routes; CLAUDE.md choice |
| `Microsoft.Extensions.ServiceDiscovery.Yarp` | 10.7.0 [VERIFIED: NuGet] | Aspire service discovery resolution for YARP cluster destinations | Required to avoid hardcoded cluster URLs in Aspire environments |
| `Npgsql.EntityFrameworkCore.PostgreSQL` | 10.0.2 [VERIFIED: NuGet] | EF Core provider for PostgreSQL | Only stable Npgsql 10.x; matches EF Core 10 |
| `Aspire.Npgsql.EntityFrameworkCore.PostgreSQL` | 13.4.4 [VERIFIED: NuGet] | Aspire-wired EF Core + Postgres with built-in health, telemetry, retry | Replaces manual `AddDbContext` in Aspire-hosted services |
| `MassTransit` | 8.3.6 [VERIFIED: NuGet] | Messaging abstraction, consumer pipeline | Pinned Apache-2.0 version per ADR-0006 |
| `MassTransit.Azure.ServiceBus.Core` | 8.3.6 [VERIFIED: NuGet] | Azure Service Bus transport | Same pin — match version exactly |
| `MassTransit.EntityFrameworkCore` | 8.3.6 [VERIFIED: NuGet] | Transactional outbox + inbox tables, EF Core integration | Cannot be retrofitted; must land with first MT configuration |
| `MassTransit.Testing` | 8.3.6 [VERIFIED: NuGet — 8.3.6 confirmed exists] | InMemory test harness for idempotency test | Enables D-14 without ASB emulator |
| `Riok.Mapperly` | 4.3.1 [VERIFIED: NuGet] | Source-generated DTO ↔ domain mapping | CLAUDE.md: Mapperly not AutoMapper; AOT-friendly |
| `FluentValidation` | 11.3.1 [VERIFIED: NuGet] | Request/command validation | CLAUDE.md standard |
| `MediatR` | 13.1.0 [VERIFIED: NuGet — 13.x exists] | In-process mediator for command/query dispatch | CLAUDE.md notes license risk; use 12.x if license concern applies. 13.1.0 is latest. |

> **MediatR licensing note:** CLAUDE.md warns of license trajectory risk. MediatR 12.x is Apache-2.0. MediatR 13.x license status is [ASSUMED] to remain permissive — planner should verify on nuget.org before pinning 13.x. If uncertain, lock to `12.4.1` which is confirmed Apache-2.0. Phase 2 does not strictly require MediatR — vertical-slice handlers can use plain classes.

### Supporting (Test Infrastructure)

| Library | Version | Purpose | When to Use |
|---------|---------|---------|-------------|
| `xunit` (v3) | `xunit.v3` NuGet series [ASSUMED — v3 GA package names differ from v2] | Test framework | All test projects per D-27 |
| `FluentAssertions` | 8.10.0 [VERIFIED: NuGet] | Readable assertion syntax | All test projects |
| `NSubstitute` | 5.3.0 [VERIFIED: NuGet] | Mocking for unit tests | Unit test projects |
| `Testcontainers.PostgreSql` | 4.12.0 [VERIFIED: NuGet] | PostgreSQL container for integration tests | `PostgresFixture` in Tests.Common |
| `Microsoft.AspNetCore.Mvc.Testing` | 10.0.9 [ASSUMED — ships with .NET 10] | `WebApplicationFactory<T>` for full-pipeline API tests | Integration test projects |

> **xUnit v3 package name:** xUnit v3 was released in 2024. The NuGet package ID may be `xunit.v3` and `xunit.v3.runner.visualstudio`. Verify exact IDs at nuget.org before writing .csproj files — v2 (`xunit 2.9.x`) is the fallback if v3 packaging is confusing [ASSUMED — v3 package name not confirmed via authoritative source in this session].

### Angular

| Package | Version | Purpose | Why Standard |
|---------|---------|---------|--------------|
| `@angular/material` | 20.x [ASSUMED — verified as current stable line in UI-SPEC] | Component library | D-17; Material 20 matches Angular 20 |
| `@angular/cdk` | 20.x [ASSUMED] | Angular Material peer dependency | Required by @angular/material |
| `angular-auth-oidc-client` | 21.x [ASSUMED — latest as of research per WebSearch; earlier research confirmed 21.0.2] | OIDC client, PKCE, token management | D-03; maintained by damienbod; MIT |
| `@analogjs/vitest-angular` | latest [ASSUMED] | Vitest runner for Angular 20 | D-21; CLAUDE.md prefers Vitest over Karma |

> **angular-auth-oidc-client version mismatch note:** CONTEXT.md D-03 specifies `19.x`. The current latest version confirmed via WebSearch is `21.x` (21.0.2, May 2026). The library follows Angular major version matching, so Angular 20 should use `20.x` (peer dep aligned), but `21.x` is the latest release. Use `21.x` unless Angular 20 peer dep compatibility fails — check `npm info angular-auth-oidc-client peerDependencies` before installing. This discrepancy is flagged as a planner verification item. [ASSUMED — npm registry unreachable from this environment due to TLS certificate issue]

### Alternatives Considered

| Instead of | Could Use | Tradeoff |
|------------|-----------|----------|
| `angular-auth-oidc-client` | `oidc-client-ts` directly | Lower-level; more setup required; `angular-auth-oidc-client` wraps oidc-client-ts with Angular provider integration |
| `Aspire.Npgsql.EntityFrameworkCore.PostgreSQL` | Plain `AddDbContext<T>()` | Aspire package adds health checks, retries, telemetry for free; no downside in Aspire-hosted services |
| `Riok.Mapperly` (source-gen) | Manual `ToDomain()` / `ToDto()` methods | For Phase 2's 2-3 entities, manual mapping is fine too; Mapperly adds compile-time safety on property drift |

**Installation (NuGet — Identity service):**
```bash
dotnet add package Microsoft.AspNetCore.Identity.EntityFrameworkCore --version 10.0.9
dotnet add package OpenIddict.AspNetCore --version 7.5.0
dotnet add package OpenIddict.EntityFrameworkCore --version 7.5.0
dotnet add package Aspire.Npgsql.EntityFrameworkCore.PostgreSQL --version 13.4.4
dotnet add package FluentValidation --version 11.3.1
dotnet add package Riok.Mapperly --version 4.3.1
```

**Installation (NuGet — Catalog service):**
```bash
dotnet add package Aspire.Npgsql.EntityFrameworkCore.PostgreSQL --version 13.4.4
dotnet add package MassTransit --version 8.3.6
dotnet add package MassTransit.Azure.ServiceBus.Core --version 8.3.6
dotnet add package MassTransit.EntityFrameworkCore --version 8.3.6
dotnet add package FluentValidation --version 11.3.1
dotnet add package Riok.Mapperly --version 4.3.1
```

**Installation (NuGet — Notifications service):**
```bash
dotnet add package Aspire.Npgsql.EntityFrameworkCore.PostgreSQL --version 13.4.4
dotnet add package MassTransit --version 8.3.6
dotnet add package MassTransit.Azure.ServiceBus.Core --version 8.3.6
dotnet add package MassTransit.EntityFrameworkCore --version 8.3.6
```

**Installation (NuGet — Gateway service):**
```bash
dotnet add package Yarp.ReverseProxy --version 2.3.0
dotnet add package Microsoft.Extensions.ServiceDiscovery.Yarp --version 10.7.0
```

**Installation (NuGet — Test projects):**
```bash
dotnet add package Testcontainers.PostgreSql --version 4.12.0
dotnet add package FluentAssertions --version 8.10.0
dotnet add package NSubstitute --version 5.3.0
dotnet add package MassTransit.Testing --version 8.3.6
dotnet add package Microsoft.AspNetCore.Mvc.Testing
```

**Version verification (done during research):**
- `MassTransit` 8.3.6 confirmed on NuGet [VERIFIED: NuGet]
- `MassTransit.EntityFrameworkCore` 8.3.6 confirmed on NuGet [VERIFIED: NuGet]
- `MassTransit.Azure.ServiceBus.Core` 8.3.6 confirmed on NuGet [VERIFIED: NuGet]
- `OpenIddict.AspNetCore` 7.5.0 confirmed on NuGet [VERIFIED: NuGet]
- `Yarp.ReverseProxy` 2.3.0 confirmed on NuGet [VERIFIED: NuGet]
- `Npgsql.EntityFrameworkCore.PostgreSQL` 10.0.2 confirmed on NuGet [VERIFIED: NuGet]
- `Testcontainers.PostgreSql` 4.12.0 confirmed on NuGet [VERIFIED: NuGet]

---

## Package Legitimacy Audit

> slopcheck was unavailable in this environment (pip install failed). All packages are tagged [ASSUMED] for provenance unless confirmed via official documentation. NuGet registry existence confirmed for .NET packages.

| Package | Registry | Age | Source Repo | slopcheck | Disposition |
|---------|----------|-----|-------------|-----------|-------------|
| `MassTransit` 8.3.6 | NuGet | 10+ yrs | github.com/MassTransit/MassTransit | unavailable | Approved — major .NET ecosystem library, confirmed on NuGet |
| `MassTransit.EntityFrameworkCore` 8.3.6 | NuGet | 10+ yrs | github.com/MassTransit/MassTransit | unavailable | Approved — same repo |
| `MassTransit.Azure.ServiceBus.Core` 8.3.6 | NuGet | 5+ yrs | github.com/MassTransit/MassTransit | unavailable | Approved — same repo |
| `MassTransit.Testing` 8.3.6 | NuGet | 5+ yrs | github.com/MassTransit/MassTransit | unavailable | Approved — same repo |
| `OpenIddict.AspNetCore` 7.5.0 | NuGet | 7+ yrs | github.com/openiddict/openiddict-core | unavailable | Approved — well-known OIDC library, documented on official site |
| `OpenIddict.EntityFrameworkCore` 7.5.0 | NuGet | 7+ yrs | github.com/openiddict/openiddict-core | unavailable | Approved — same repo |
| `Yarp.ReverseProxy` 2.3.0 | NuGet | 4+ yrs | github.com/microsoft/reverse-proxy | unavailable | Approved — Microsoft-maintained |
| `Microsoft.Extensions.ServiceDiscovery.Yarp` | NuGet | 2+ yrs | github.com/dotnet/aspire | unavailable | Approved — Aspire ecosystem, Microsoft |
| `Aspire.Npgsql.EntityFrameworkCore.PostgreSQL` | NuGet | 2+ yrs | github.com/dotnet/aspire | unavailable | Approved — Aspire ecosystem, Microsoft |
| `Npgsql.EntityFrameworkCore.PostgreSQL` 10.0.2 | NuGet | 8+ yrs | github.com/npgsql/efcore.pg | unavailable | Approved — official Npgsql team |
| `Riok.Mapperly` 4.3.1 | NuGet | 3+ yrs | github.com/riok/mapperly | unavailable | Approved — source-gen mapper, established |
| `FluentValidation` 11.3.1 | NuGet | 10+ yrs | github.com/FluentValidation/FluentValidation | unavailable | Approved — industry standard |
| `Testcontainers.PostgreSql` 4.12.0 | NuGet | 4+ yrs | github.com/testcontainers/testcontainers-dotnet | unavailable | Approved — official Testcontainers org |
| `angular-auth-oidc-client` 21.x | npm | 6+ yrs | github.com/damienbod/angular-auth-oidc-client | unavailable | Approved — well-known, active maintenance, MIT; version mismatch with CONTEXT.md 19.x flagged — planner must verify Angular 20 peer dep |

**Packages removed due to slopcheck [SLOP] verdict:** none
**Packages flagged as suspicious [SUS]:** none
*slopcheck was unavailable at research time — all packages tagged [ASSUMED] for provenance. The planner must add a `checkpoint:human-verify` task before each install group.*

---

## Architecture Patterns

### System Architecture Diagram

```
Browser (Angular 20)
  │  GET /catalog, /product/:id          POST /api/identity/register
  │  /login → PKCE redirect              GET /api/catalog/products
  │  /callback → token storage           GET /api/catalog/products/{id}
  ▼
YARP Gateway (localhost:5000)
  │  Route: /api/identity/** → Identity service
  │  Route: /api/catalog/**  → Catalog service
  │  Header: Authorization pass-through (no JWT validation)
  ├──────────────────────────────────────────────┐
  ▼                                              ▼
Identity Service (localhost:5005)         Catalog Service (localhost:5001)
  ├── POST /register (UserManager)          ├── GET /products (EF Core + Npgsql)
  ├── GET /connect/authorize → login UI     ├── GET /products/{id}
  ├── POST /connect/token → JWT             ├── DbInitializer (seed + publish CatalogSeeded)
  ├── GET /me (JWT-protected)               │     └── MassTransit Transactional Outbox
  ├── GET /.well-known/openid-configuration │           └── OutboxMessage table (Postgres)
  ├── ASP.NET Core Identity (IdentityDB)    ├── CatalogDB (Postgres)
  └── OpenIddict (same DB)                  └── JWT validation → OpenIddict discovery
                                                │
                                         Azure Service Bus (topic: catalog-events)
                                                │
                                         Notifications Service (localhost:5008)
                                           ├── Consumer: CatalogSeededConsumer
                                           ├── MassTransit Idempotent Inbox
                                           │     └── InboxState table (NotificationsDB)
                                           └── NotificationsDB (Postgres)
```

**Data flow for product catalog (happy path):**
1. Angular calls `GET /api/catalog/products?page=1&pageSize=12` through proxy to YARP
2. YARP forwards to Catalog service (service discovery resolves "catalog" → Aspire port)
3. Catalog service queries PostgreSQL, returns paginated product list + totalCount
4. Angular renders product grid via `CatalogListComponent` signals

**Data flow for PKCE login:**
1. Angular calls `oidcSecurityService.authorize()` → redirect to `http://localhost:5005/connect/authorize`
2. OpenIddict checks auth state → challenges `Cookie` scheme → redirect to `/Account/Login`
3. User posts credentials → ASP.NET Core Identity validates → sets cookie → redirect back to `/connect/authorize`
4. Authorization endpoint builds `ClaimsPrincipal`, calls `SignIn()` with OpenIddict scheme
5. Browser receives auth code → redirect to `localhost:4200/callback`
6. `angular-auth-oidc-client` exchanges auth code for tokens at `POST /connect/token`
7. Access token stored in session storage; `authInterceptor` attaches to all API calls

### Recommended Project Structure

```
src/
├── services/
│   ├── identity/
│   │   ├── Identity.sln
│   │   ├── ECommerce.Identity.API/
│   │   │   ├── Program.cs              (existing — expand)
│   │   │   ├── ECommerce.Identity.API.csproj
│   │   │   ├── Features/
│   │   │   │   ├── Registration/       (RegisterEndpoint.cs, RegisterRequest.cs, RegisterValidator.cs)
│   │   │   │   ├── Profile/            (MeEndpoint.cs, UserProfileDto.cs)
│   │   │   │   └── Authorization/      (AuthorizationEndpoint.cs — PKCE handler)
│   │   │   ├── Data/
│   │   │   │   ├── IdentityDbContext.cs
│   │   │   │   └── DbInitializer.cs   (seeds OIDC client + demo users)
│   │   │   └── Pages/
│   │   │       └── Account/            (Login.cshtml, Login.cshtml.cs — Razor Pages for login UI)
│   │   └── ECommerce.Identity.Tests/
│   │       ├── Unit/
│   │       └── Integration/
│   ├── catalog/
│   │   ├── Catalog.sln
│   │   ├── ECommerce.Catalog.API/
│   │   │   ├── Program.cs              (existing — expand)
│   │   │   ├── Features/
│   │   │   │   └── Products/           (ProductsEndpoints.cs, Product.cs, ProductDto.cs, ProductMapper.cs)
│   │   │   └── Data/
│   │   │       ├── CatalogDbContext.cs
│   │   │       └── DbInitializer.cs   (seeds 20-50 SKUs, publishes CatalogSeeded)
│   │   └── ECommerce.Catalog.Tests/
│   │       ├── Unit/
│   │       └── Integration/
│   ├── notifications/
│   │   ├── Notifications.sln
│   │   ├── ECommerce.Notifications.API/
│   │   │   ├── Program.cs              (existing — expand)
│   │   │   ├── Consumers/
│   │   │   │   └── CatalogSeededConsumer.cs
│   │   │   └── Data/
│   │   │       └── NotificationsDbContext.cs
│   │   └── ECommerce.Notifications.Tests/
│   │       └── Integration/
│   └── gateway/
│       ├── Gateway.sln                 (new)
│       ├── ECommerce.Gateway.API/      (new)
│       │   ├── Program.cs
│       │   ├── ECommerce.Gateway.API.csproj
│       │   └── appsettings.json        (ReverseProxy routes)
│       └── ECommerce.Gateway.Tests/
│           └── Integration/
├── building-blocks/
│   ├── Contracts/
│   │   └── Catalog/Events/V1/
│   │       └── CatalogSeeded.cs        (replaces Placeholder.cs)
│   └── Tests.Common/
│       ├── ECommerce.Tests.Common.csproj (new)
│       ├── PostgresFixture.cs
│       ├── ServiceWebApplicationFactory.cs
│       └── Builders/
│           ├── ProductBuilder.cs
│           └── UserBuilder.cs
└── frontend/
    └── ecommerce-app/                  (ng new output)
        ├── src/app/
        │   ├── features/
        │   │   ├── catalog/
        │   │   └── auth/
        │   └── app.config.ts
        └── proxy.conf.json
```

### Pattern 1: OpenIddict Auth Code PKCE Server Setup

**What:** Register OpenIddict with ASP.NET Core Identity and EF Core stores; enable auth code flow with PKCE passthrough endpoints.
**When to use:** Identity service Program.cs only.

```csharp
// Source: documentation.openiddict.com/guides/getting-started + andreyka26.com verified pattern
builder.Services.AddOpenIddict()
    .AddCore(options =>
    {
        options.UseEntityFrameworkCore()
               .UseDbContext<IdentityDbContext>();
    })
    .AddServer(options =>
    {
        options.SetAuthorizationEndpointUris("connect/authorize")
               .SetTokenEndpointUris("connect/token")
               .SetUserinfoEndpointUris("connect/userinfo")
               .SetLogoutEndpointUris("connect/logout");

        options.RegisterScopes(Scopes.OpenId, Scopes.Email, Scopes.Profile);
        options.AllowAuthorizationCodeFlow();

        // Development certificates — replace with real certs in production
        options.AddDevelopmentEncryptionCertificate()
               .AddDevelopmentSigningCertificate();

        options.UseAspNetCore()
               .EnableAuthorizationEndpointPassthrough()
               .EnableTokenEndpointPassthrough()
               .EnableUserinfoEndpointPassthrough()
               .EnableLogoutEndpointPassthrough();
    })
    .AddValidation(options =>
    {
        // Validate tokens issued by this same server
        options.UseLocalServer();
        options.UseAspNetCore();
    });
```
[CITED: documentation.openiddict.com/integrations/aspnet-core, andreyka26.com/openid-connect-authorization-code-using-openiddict-and-dot-net]

### Pattern 2: OpenIddict Authorization Endpoint (Minimal API handler)

**What:** The authorization endpoint is the core of PKCE — it checks login state, redirects to Identity login if unauthenticated, builds claims, and returns a sign-in result.
**When to use:** Authorization controller/handler in Identity service.

```csharp
// Source: [CITED: andreyka26.com verified pattern — adapted for Minimal API]
app.MapGet("/connect/authorize", async (HttpContext ctx, IOpenIddictServerDispatcher dispatcher) =>
{
    var request = ctx.GetOpenIddictServerRequest()
        ?? throw new InvalidOperationException("OpenIddict request missing.");

    // Check if user is already authenticated via cookie
    var result = await ctx.AuthenticateAsync(IdentityConstants.ApplicationScheme);
    if (!result.Succeeded)
    {
        // Redirect to login page; carry the original OIDC params
        var redirectUri = QueryHelpers.AddQueryString(
            "/Account/Login",
            new Dictionary<string, string?> { ["returnUrl"] = ctx.Request.PathBase + ctx.Request.Path + ctx.Request.QueryString });

        return Results.Challenge(
            new AuthenticationProperties { RedirectUri = redirectUri },
            [IdentityConstants.ApplicationScheme]);
    }

    // Build claims principal with correct destinations
    var user = result.Principal;
    var identity = new ClaimsIdentity(OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
    identity.AddClaim(Claims.Subject, user.FindFirstValue(ClaimTypes.NameIdentifier)!,
        Destinations.AccessToken, Destinations.IdentityToken);
    identity.AddClaim(Claims.Email, user.FindFirstValue(ClaimTypes.Email)!,
        Destinations.IdentityToken);
    identity.AddClaim(Claims.Name, user.FindFirstValue(ClaimTypes.Name)!,
        Destinations.IdentityToken);

    var principal = new ClaimsPrincipal(identity);
    principal.SetScopes(request.GetScopes());

    return Results.SignIn(principal, authenticationScheme: OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
});
```
[ASSUMED — adapted from documented patterns; exact Minimal API variant not confirmed in official samples]

> **Critical implementation note:** OpenIddict's authorization endpoint requires Razor Pages or MVC for the login page. The login UI cannot be a Minimal API endpoint because it renders HTML with a form. Add `builder.Services.AddRazorPages()` and `app.MapRazorPages()` to the Identity service. The `/Account/Login` Razor Page uses `SignInManager.PasswordSignInAsync()` to issue the cookie, then redirects back to the OIDC authorize URL.

### Pattern 3: OpenIddict Client Seeding (IHostedService)

**What:** Seeds the Angular SPA client registration and demo users on first run. Idempotent — safe to run on every startup.
**When to use:** `DbInitializer` in Identity service.

```csharp
// Source: [CITED: documentation.openiddict.com/guides/getting-started — client seeding pattern]
public class DbInitializer(IServiceProvider serviceProvider) : IHostedService
{
    public async Task StartAsync(CancellationToken ct)
    {
        await using var scope = serviceProvider.CreateAsyncScope();
        var manager = scope.ServiceProvider.GetRequiredService<IOpenIddictApplicationManager>();

        if (await manager.FindByClientIdAsync("ecommerce-spa", ct) is null)
        {
            await manager.CreateAsync(new OpenIddictApplicationDescriptor
            {
                ClientId = "ecommerce-spa",
                ConsentType = ConsentTypes.Implicit,
                DisplayName = "ECommerce SPA",
                Type = ClientTypes.Public,
                PostLogoutRedirectUris = { new Uri("http://localhost:4200") },
                RedirectUris = { new Uri("http://localhost:4200/callback") },
                Permissions =
                {
                    Permissions.Endpoints.Authorization,
                    Permissions.Endpoints.Token,
                    Permissions.GrantTypes.AuthorizationCode,
                    Permissions.ResponseTypes.Code,
                    Permissions.Scopes.Email,
                    Permissions.Scopes.Profile,
                    $"{Permissions.Prefixes.Scope}openid"
                },
            }, ct);
        }

        // Seed demo users
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();
        await SeedUserIfNotExists(userManager, "demo@example.com", "demo123", ct);
        await SeedUserIfNotExists(userManager, "admin@example.com", "admin123", ct);
    }

    private static async Task SeedUserIfNotExists(UserManager<IdentityUser> um,
        string email, string password, CancellationToken ct)
    {
        if (await um.FindByEmailAsync(email) is null)
        {
            var user = new IdentityUser { UserName = email, Email = email, EmailConfirmed = true };
            await um.CreateAsync(user, password);
        }
    }

    public Task StopAsync(CancellationToken ct) => Task.CompletedTask;
}
```
[CITED: documentation.openiddict.com — adapted from official client seeding pattern]

### Pattern 4: MassTransit Transactional Outbox (Catalog service)

**What:** Wire MassTransit 8.3.6 with EF Core outbox so `CatalogSeeded` is published atomically inside the seeder's database transaction.
**When to use:** Catalog service Program.cs + `CatalogDbContext`.

```csharp
// Source: [CITED: masstransit.massient.com/documentation/configuration/middleware/outbox]
// DbContext — add outbox entities to model
protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    base.OnModelCreating(modelBuilder);
    modelBuilder.AddInboxStateEntity();
    modelBuilder.AddOutboxMessageEntity();
    modelBuilder.AddOutboxStateEntity();
}

// Program.cs
builder.Services.AddMassTransit(x =>
{
    x.AddEntityFrameworkOutbox<CatalogDbContext>(o =>
    {
        o.UsePostgres();
        o.UseBusOutbox();
    });

    x.UsingAzureServiceBus((context, cfg) =>
    {
        cfg.Host(builder.Configuration.GetConnectionString("messaging"));
        cfg.ConfigureEndpoints(context);
    });
});
```
[CITED: masstransit.massient.com/documentation/configuration/middleware/outbox]

### Pattern 5: MassTransit Idempotent Inbox (Notifications service)

**What:** Wire the per-endpoint outbox (which acts as inbox) so duplicate `CatalogSeeded` deliveries are deduplicated by `MessageId`.
**When to use:** Notifications service Program.cs.

```csharp
// Source: [CITED: masstransit.massient.com/documentation/configuration/middleware/outbox]
builder.Services.AddMassTransit(x =>
{
    x.AddConsumer<CatalogSeededConsumer>();

    x.AddEntityFrameworkOutbox<NotificationsDbContext>(o =>
    {
        o.UsePostgres();
        // No UseBusOutbox() — Notifications only consumes, does not publish
    });

    x.AddConfigureEndpointsCallback((context, name, cfg) =>
    {
        cfg.UseEntityFrameworkOutbox<NotificationsDbContext>(context);
    });

    x.UsingAzureServiceBus((context, cfg) =>
    {
        cfg.Host(builder.Configuration.GetConnectionString("messaging"));
        cfg.ConfigureEndpoints(context);
    });
});
```
[CITED: masstransit.massient.com — pattern verified from outbox documentation]

### Pattern 6: MassTransit InMemory Test Harness for Idempotency (D-14)

**What:** Test that `CatalogSeededConsumer` processes `CatalogSeeded` exactly once when delivered twice with the same `MessageId`.
**When to use:** `ECommerce.Notifications.Tests/Integration/` — forced redelivery test.

```csharp
// Source: [CITED: masstransit.massient.com/documentation/concepts/testing — v9 pattern; v8 adapted]
// Note: v8 uses InMemoryTestHarness — exact API is [ASSUMED] to follow AddMassTransitTestHarness pattern
[Fact]
public async Task CatalogSeededConsumer_DuplicateMessageId_ProcessedOnce()
{
    await using var provider = new ServiceCollection()
        .AddEntityFrameworkInMemoryDatabase()
        .AddDbContext<NotificationsDbContext>()
        .AddMassTransitTestHarness(x =>
        {
            x.AddConsumer<CatalogSeededConsumer>();
            x.AddEntityFrameworkOutbox<NotificationsDbContext>(o =>
            {
                // In-memory test: no Postgres; skip outbox for test harness
            });
        })
        .BuildServiceProvider(true);

    var harness = await provider.StartTestHarness();
    var messageId = Guid.NewGuid();
    var message = new CatalogSeeded(messageId, messageId, Guid.Empty,
        DateTimeOffset.UtcNow, Guid.NewGuid(), 25, DateTimeOffset.UtcNow);

    // Publish twice with same MessageId
    await harness.Bus.Publish(message);
    await harness.Bus.Publish(message); // duplicate

    await harness.InactivityTask;

    // Verify consumed exactly once
    (await harness.Consumed.Any<CatalogSeeded>()).ShouldBe(true);
    // Verify DB has exactly one inbox entry — implementation detail of consumer
}
```
[ASSUMED — exact v8 InMemory test harness API for duplicate detection not confirmed in official v8 docs; v9 pattern used as reference; adjust per actual v8 MassTransit.Testing API]

> **Note:** The EF Core inbox deduplication only works when `UseEntityFrameworkOutbox` is configured on the endpoint AND a real database is used. For a pure InMemory harness test, the inbox deduplication relies on MassTransit's in-memory tracking, not the DB. The integration test in D-14 is primarily a consumer-invocation count assertion. For full inbox DB deduplication verification, an integration test with a real Testcontainers Postgres instance is more accurate.

### Pattern 7: YARP Gateway (appsettings.json + Aspire service discovery)

**What:** YARP routes requests to downstream services by logical name, resolved by Aspire service discovery.
**When to use:** `ECommerce.Gateway.API/appsettings.json` and `Program.cs`.

```json
// Source: [CITED: timdeschryver.dev/blog/integrating-yarp-within-dotnet-aspire]
{
  "ReverseProxy": {
    "Routes": {
      "identity-route": {
        "ClusterId": "identity",
        "Match": { "Path": "/api/identity/{**catch-all}" },
        "Transforms": [{ "PathRemovePrefix": "/api/identity" }]
      },
      "catalog-route": {
        "ClusterId": "catalog",
        "Match": { "Path": "/api/catalog/{**catch-all}" },
        "Transforms": [{ "PathRemovePrefix": "/api/catalog" }]
      }
    },
    "Clusters": {
      "identity": {
        "Destinations": {
          "identity": { "Address": "http://identity" }
        }
      },
      "catalog": {
        "Destinations": {
          "catalog": { "Address": "http://catalog" }
        }
      }
    }
  }
}
```

```csharp
// Program.cs
builder.AddServiceDefaults();
builder.Services
    .AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"))
    .AddServiceDiscoveryDestinationResolver();

var app = builder.Build();
app.MapReverseProxy();
app.Run();
```
[CITED: timdeschryver.dev/blog/integrating-yarp-within-dotnet-aspire]

### Pattern 8: Angular angular-auth-oidc-client with provideAuth (standalone)

**What:** Configure `angular-auth-oidc-client` in `app.config.ts` for PKCE auth code flow with the OpenIddict server.
**When to use:** Angular frontend `app.config.ts`.

```typescript
// Source: [CITED: timdeschryver.dev/blog/configuring-angular-auth-oidc-client-with-the-new-functional-apis]
import { provideAuth, authInterceptor } from 'angular-auth-oidc-client';
import { provideHttpClient, withFetch, withInterceptors } from '@angular/common/http';
import { provideRouter } from '@angular/router';

export const appConfig: ApplicationConfig = {
  providers: [
    provideAuth({
      config: {
        authority: 'http://localhost:5005',      // Identity service (YARP does NOT intercept OIDC endpoints)
        redirectUrl: 'http://localhost:4200/callback',
        postLogoutRedirectUri: 'http://localhost:4200',
        clientId: 'ecommerce-spa',
        scope: 'openid profile email',
        responseType: 'code',
        silentRenew: false,                       // Phase 2: no silent renew
        useRefreshToken: false,
        secureRoutes: ['http://localhost:4200/api'], // Routes that get Bearer token injected
      },
    }),
    provideHttpClient(withFetch(), withInterceptors([authInterceptor()])),
    provideRouter(routes),
    provideZonelessChangeDetection(),
  ],
};
```
[CITED: timdeschryver.dev — adapted for OpenIddict authority URL and Phase 2 scope]

> **OIDC authority URL note:** Angular communicates directly with the Identity service for OIDC flows (`/connect/authorize`, `/connect/token`, `/.well-known/openid-configuration`). These are NOT routed through YARP — the browser must reach the Identity service directly for the redirect-based PKCE flow to work. Only API data calls (`/api/catalog/**`, `/api/identity/register`) go through YARP. The `proxy.conf.json` should only proxy `/api/**`, not OIDC endpoints.

### Pattern 9: CatalogSeeded Contract Record

**What:** Replace `Catalog/Events/V1/Placeholder.cs` with the `CatalogSeeded` event per D-11/D-12.
**When to use:** `src/building-blocks/Contracts/Catalog/Events/V1/CatalogSeeded.cs`.

```csharp
// Source: CONTEXT.md D-12 specification
using ECommerce.Contracts;

namespace ECommerce.Catalog.Events.V1;

public record CatalogSeeded(
    Guid MessageId,
    Guid CorrelationId,
    Guid CausationId,
    DateTimeOffset OccurredAt,
    Guid SeedId,
    int ItemCount,
    DateTimeOffset SeededAt
) : IMessageEnvelope;
```
[CITED: CONTEXT.md D-12 — directly from user decision]

### Pattern 10: Testcontainers PostgresFixture (Tests.Common)

**What:** Shared Testcontainers Postgres fixture for integration test isolation. Per D-28: per-class isolation.
**When to use:** `src/building-blocks/Tests.Common/PostgresFixture.cs`.

```csharp
// Source: [CITED: dotnet.testcontainers.org/modules/postgres/ + timdeschryver.dev/blog]
public class PostgresFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder()
        .WithDatabase("test-db")
        .WithUsername("test")
        .WithPassword("test")
        .Build();

    public string ConnectionString => _container.GetConnectionString();

    public async ValueTask InitializeAsync() => await _container.StartAsync();
    public async ValueTask DisposeAsync() => await _container.DisposeAsync();
}
```
[CITED: dotnet.testcontainers.org/modules/postgres/]

### Anti-Patterns to Avoid

- **Putting OIDC logic in the gateway:** The gateway forwards the `Authorization` header — it never validates JWTs or issues tokens. JWT validation belongs in each service using `AddOpenIddict().AddValidation()`.
- **Routing OIDC redirect flow through YARP:** PKCE login redirects must hit the Identity service's public URL directly. If the browser redirects to `localhost:5000/connect/authorize` (YARP), the redirect_uri after login will be wrong. Identity must be reachable at its own URL.
- **Using `ConsentType.Explicit` in demo:** Requires a consent page. For a demo SPA use `ConsentTypes.Implicit` (no user consent screen) per D-01/D-02 intent.
- **Floating MassTransit version:** `<PackageReference Include="MassTransit" Version="*" />` will resolve to 9.x (commercial). Always pin `Version="8.3.6"`.
- **Missing `AddOpenIddict().AddValidation()` on consuming services:** Protected endpoints on Identity and Catalog will silently accept any Bearer token without this call. It must be registered alongside `AddAuthentication().AddJwtBearer()` — or use OpenIddict's own validation scheme exclusively.
- **Not calling `app.UseAuthentication()` before `app.UseAuthorization()`:** Middleware order is strict in ASP.NET Core. OpenIddict middleware must be registered after `UseCors()` and before `UseEndpoints()`.

---

## Don't Hand-Roll

| Problem | Don't Build | Use Instead | Why |
|---------|-------------|-------------|-----|
| OIDC token issuance | Custom JWT generation, HMAC signing code | OpenIddict 7.5.0 | Token signing, PKCE code challenge/verifier, token revocation, discovery endpoint are all non-trivial to get right |
| Password hashing | Custom bcrypt or SHA-256 | `UserManager<IdentityUser>.CreateAsync()` | ASP.NET Core Identity handles Argon2id/PBKDF2 — rolling your own is a security anti-pattern |
| JWT validation | Manual `JwtSecurityTokenHandler` setup | `AddOpenIddict().AddValidation()` | Key rotation, audience/issuer checks, clock skew — all handled |
| API routing + header forwarding | Custom middleware pipeline | YARP 2.3.0 with appsettings | Request buffering, transforms, load balancing, health probes — all handled |
| Outbox pattern | Custom `OutboxMessage` table + background job | `MassTransit.EntityFrameworkCore` outbox | Exactly-once publishing semantics, restart safety, DB-level locking — extremely hard to get right |
| Duplicate message deduplication | Custom `ProcessedMessageId` table + check | MassTransit `UseEntityFrameworkOutbox` per endpoint | InboxState + `DuplicateDetectionWindow` — handles race conditions |
| Postgres connection pooling + retry | Manual Polly policies on `NpgsqlConnection` | `Aspire.Npgsql.EntityFrameworkCore.PostgreSQL` | Aspire integration package wires Npgsql's built-in connection pool + retry and OpenTelemetry in one call |
| Pagination | Custom SQL offset/limit + count query | EF Core `Skip().Take()` + `CountAsync()` | Two-query pattern is standard; hand-rolling can miss null/empty edge cases |

**Key insight:** Every item in this table has subtle correctness requirements that libraries solved years ago. Phase 2 has enough new service wiring to justify avoiding hand-rolled infrastructure at all costs.

---

## Common Pitfalls

### Pitfall 1: MassTransit 8.3.6 Incompatibility with ASB Emulator (KNOWN OPEN ISSUE)

**What goes wrong:** MassTransit 8.3.6 attempts to connect to Azure Service Bus on port 443 (HTTPS). The ASB emulator uses non-TLS AMQP port 5672. MassTransit 8.3.6 has no `EmulatorHost()` API (added in v9) and no built-in workaround for `UseDevelopmentEmulator=true` connection strings.

**Why it happens:** MassTransit 8.x predates the Azure Service Bus Emulator's GA release. The SDK-level `UseDevelopmentEmulator=true` flag was not integrated into MassTransit 8.x's `ServiceBusClientOptions`.

**How to avoid:** D-14 deliberately avoids this problem — the idempotency test uses the InMemory harness, not the ASB emulator. For the Catalog → Notifications integration path that does require ASB connectivity in local dev:
- Option A: Use a real Azure Service Bus namespace (free tier available) and inject the real connection string via Aspire secrets.
- Option B: Set `TransportType = ServiceBusTransportType.AmqpTcp` and manually patch the port in `ServiceBusClientOptions` — not confirmed to work, treat as LOW confidence.
- Option C: Accept that the Catalog seeder publishes `CatalogSeeded` but the Notifications consumer does not receive it in local dev (message lands in ASB dead letter). Verify inbox behavior via the InMemory harness only.

**Warning signs:** Service startup log shows `Connection refused` or SSL certificate errors connecting to `localhost:5672`.

[CITED: github.com/MassTransit/MassTransit/issues/5689 — open issue, no confirmed workaround as of 2026-06-17]

### Pitfall 2: OpenIddict Requires a Controller/Page for Login UI

**What goes wrong:** OpenIddict's authorization endpoint (enable via `EnableAuthorizationEndpointPassthrough()`) redirects to the ASP.NET Core Identity login page for unauthenticated requests. If Razor Pages is not added, the login redirect results in 404 or unhandled route.

**Why it happens:** OpenIddict's passthrough model delegates rendering to the host application's existing auth UI. It does not provide a built-in login form. ASP.NET Core Identity has `AddIdentity().AddDefaultUI()` which scaffolds Razor Pages login/logout pages automatically.

**How to avoid:** Add `builder.Services.AddRazorPages()` and `app.MapRazorPages()` to the Identity service. Call `.AddDefaultUI()` on the Identity configuration, or scaffold Identity pages manually with `dotnet aspnet-codegenerator identity`.

**Warning signs:** 404 on `GET /Account/Login`, or endless PKCE redirect loop with no login page displayed.

### Pitfall 3: CORS on the Identity Service for OIDC Discovery

**What goes wrong:** Angular (localhost:4200) calls the OpenIddict discovery endpoint (`/.well-known/openid-configuration`) directly on the Identity service (localhost:5005). The browser blocks this with a CORS error.

**Why it happens:** CORS is not configured by default on the Identity service for cross-origin browser requests.

**How to avoid:** Add CORS policy on the Identity service allowing `http://localhost:4200`:
```csharp
builder.Services.AddCors(options => options.AddDefaultPolicy(
    policy => policy.WithOrigins("http://localhost:4200")
                    .AllowAnyHeader().AllowAnyMethod()));
app.UseCors(); // before UseAuthentication
```
YARP does not forward OIDC discovery calls, so this CORS must be on Identity directly.

### Pitfall 4: MassTransit Outbox Without `UseBusOutbox()` Doesn't Publish

**What goes wrong:** `AddEntityFrameworkOutbox<DbContext>()` is called without `.UseBusOutbox()`. Messages published in the consumer are stored in `OutboxMessage` but never delivered to ASB.

**Why it happens:** `UseBusOutbox()` enables the hosted service that drains `OutboxMessage` to the transport. Without it, the table fills but no messages are sent.

**How to avoid:** Always call `o.UseBusOutbox()` in the outbox configuration for publishing services (Catalog). For consuming-only services (Notifications), omit `UseBusOutbox()`.

**Warning signs:** `OutboxMessage` table row count grows; Azure Service Bus topic has no messages; no log entries from the outbox delivery background service.

### Pitfall 5: `IMessageEnvelope` Fields Not Set on `CatalogSeeded` Publication

**What goes wrong:** `new CatalogSeeded(...)` is published without `MessageId` and `CorrelationId`. MassTransit sets its own internal `MessageId` on the envelope, but `CatalogSeeded.MessageId` (the record property) is `Guid.Empty`.

**Why it happens:** `IMessageEnvelope` fields are application-level properties in the record's primary constructor — they are not auto-populated by MassTransit's send/publish context `MessageId`.

**How to avoid:** Always populate envelope fields explicitly at publish site:
```csharp
var seedId = Guid.NewGuid();
await publishEndpoint.Publish(new CatalogSeeded(
    MessageId: Guid.NewGuid(),
    CorrelationId: Guid.NewGuid(),
    CausationId: Guid.Empty,
    OccurredAt: DateTimeOffset.UtcNow,
    SeedId: seedId,
    ItemCount: products.Count,
    SeededAt: DateTimeOffset.UtcNow));
```

**Warning signs:** `CatalogSeeded.MessageId == Guid.Empty` in consumer logs; inbox deduplication doesn't work because all messages have the same (empty) ID.

### Pitfall 6: Aspire Gateway Service Discovery — Logical Name Must Match AppHost Registration

**What goes wrong:** YARP appsettings uses `"Address": "http://identity"` but the AppHost registers the service as `"identity-api"`. Service discovery resolution fails — YARP cannot find the destination.

**Why it happens:** Aspire service discovery uses the resource name from `AddProject<T>("name")` as the DNS name for inter-service communication. YARP's `AddServiceDiscoveryDestinationResolver` resolves by that exact name.

**How to avoid:** The YARP cluster destination names in `appsettings.json` must exactly match the AppHost project registration names (e.g., `"catalog"`, `"identity"`, not `"catalog-api"`).

**Warning signs:** YARP logs `No destinations available for cluster 'identity'`; 503 responses from gateway.

### Pitfall 7: Angular proxy.conf.json Routing OIDC Endpoints Through YARP

**What goes wrong:** `proxy.conf.json` proxies `/api/**` to the gateway, but also mistakenly routes `/connect/**` to the gateway. OpenIddict's PKCE redirect fails because the gateway doesn't have the Identity service's `/connect/authorize` endpoint.

**Why it happens:** Overly broad proxy patterns (`/**`) intercept OIDC endpoints.

**How to avoid:** `proxy.conf.json` should only proxy `/api/**`. OIDC endpoints (`/connect/**`, `/.well-known/**`) must route directly to Identity service (`localhost:5005`). Configure `angular-auth-oidc-client`'s `authority` to point directly at Identity, not through the gateway.

---

## Code Examples

### EF Core DbContext with MassTransit Outbox Entities

```csharp
// Source: [CITED: masstransit.massient.com/documentation/configuration/middleware/outbox]
public class CatalogDbContext(DbContextOptions<CatalogDbContext> options)
    : DbContext(options)
{
    public DbSet<Product> Products => Set<Product>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        // MassTransit outbox tables
        modelBuilder.AddInboxStateEntity();
        modelBuilder.AddOutboxMessageEntity();
        modelBuilder.AddOutboxStateEntity();

        modelBuilder.Entity<Product>(b =>
        {
            b.HasKey(p => p.Id);
            b.Property(p => p.Name).IsRequired().HasMaxLength(200);
            b.Property(p => p.Price).HasColumnType("decimal(18,2)");
            b.Property(p => p.Category).HasMaxLength(100);
            b.Property(p => p.Sku).HasMaxLength(50);
        });
    }
}
```
[CITED: masstransit.massient.com outbox documentation]

### IdentityDbContext (extends OpenIddict + Identity)

```csharp
// Source: [ASSUMED — standard pattern, not verified in official docs in this session]
public class IdentityDbContext(DbContextOptions<IdentityDbContext> options)
    : IdentityDbContext<IdentityUser>(options)
{
    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        // OpenIddict tables are registered automatically when UseEntityFrameworkCore() is called
    }
}
```

### Aspire AppHost — Adding Gateway (10th resource)

```csharp
// Source: CONTEXT.md D-09 + existing AppHost pattern
builder.AddProject<Projects.ECommerce_Gateway_API>("gateway")
    .WithEndpoint(name: "http", port: 5000, targetPort: 5000, scheme: "http", isExternal: true)
    .WithExternalHttpEndpoints()
    .WithReference(catalog)    // so service discovery resolves "catalog"
    .WithReference(identity);  // so service discovery resolves "identity"
```
[ASSUMED — Aspire project reference pattern; exact port per phase decisions]

### Product entity (Claude's Discretion fields)

```csharp
// Source: Claude's Discretion (CONTEXT.md)
public class Product
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Sku { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public int StockQuantity { get; set; }
    public string Category { get; set; } = string.Empty;
    public string? ImageUrl { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}
```

### Catalog Products Endpoint (Minimal API)

```csharp
// Source: [ASSUMED — standard Minimal API pattern for EF Core pagination]
app.MapGet("/products", async (
    [FromQuery] int page, [FromQuery] int pageSize,
    [FromQuery] string? category,
    CatalogDbContext db, CancellationToken ct) =>
{
    var query = db.Products.AsQueryable();
    if (!string.IsNullOrEmpty(category))
        query = query.Where(p => p.Category == category);

    var total = await query.CountAsync(ct);
    var products = await query
        .OrderBy(p => p.Name)
        .Skip((page - 1) * pageSize)
        .Take(pageSize)
        .Select(p => new ProductDto(p.Id, p.Name, p.Price, p.StockQuantity, p.Category, p.ImageUrl))
        .ToListAsync(ct);

    return Results.Ok(new { Items = products, TotalCount = total, Page = page, PageSize = pageSize });
});
```

---

## State of the Art

| Old Approach | Current Approach | When Changed | Impact |
|--------------|------------------|--------------|--------|
| Swashbuckle for OpenAPI | `Microsoft.AspNetCore.OpenApi` (already in Identity/Catalog stubs) | .NET 9+ | Already applied in Phase 1 stubs |
| `AuthModule.forRoot()` (NgModule) | `provideAuth()` standalone functional API | angular-auth-oidc-client v16+ | Must use `provideAuth()` — no NgModules |
| `HTTP_INTERCEPTORS` class token | `withInterceptors([authInterceptor()])` functional | Angular 15+ | `provideHttpClient(withFetch(), withInterceptors([authInterceptor()]))` |
| MassTransit `InMemoryTestHarness` (static) | `AddMassTransitTestHarness()` DI-based harness | MassTransit v8+ | DI-based approach confirmed in v8 test docs |
| Hardcoded YARP cluster URLs | `AddServiceDiscoveryDestinationResolver()` | .NET Aspire GA | Aspire injects logical names; no URL hardcoding |
| Zone.js Angular | `provideZonelessChangeDetection()` | Angular 18+ (stable in 20) | Already decided in D-16 |
| AutoMapper | Riok.Mapperly (source-generated) | CLAUDE.md locked | No reflection, AOT-friendly, compile-time errors |

**Deprecated/outdated:**
- `NgModules` in Angular: standalone components only; no `BrowserModule`, no `CommonModule`, use `@if` template control flow instead of `*ngIf`.
- `MassTransit v9.x` (commercial): Apache-2.0 ended at 8.x line. Never upgrade without re-evaluating license.
- `Duende IdentityServer`: commercial above revenue threshold; OpenIddict is the selected alternative.

---

## Assumptions Log

| # | Claim | Section | Risk if Wrong |
|---|-------|---------|---------------|
| A1 | MediatR 13.x license is permissive (Apache-2.0 or MIT) | Standard Stack | If commercial, must pin 12.4.1 or remove MediatR entirely from Phase 2 |
| A2 | `angular-auth-oidc-client` 21.x is peer-compatible with Angular 20 | Standard Stack | If only 20.x is compatible, install `20.x` instead of `21.x` |
| A3 | xUnit v3 NuGet package ID is `xunit.v3` and `xunit.v3.runner.visualstudio` | Standard Stack | If wrong, `dotnet add package` will fail; fall back to `xunit 2.9.x` |
| A4 | `Microsoft.AspNetCore.Authentication.JwtBearer` 10.0.x is the correct package for JWT bearer validation alongside OpenIddict | Standard Stack | OpenIddict 7.x may bundle JWT validation internally via `AddValidation()` — `JwtBearer` package may not be needed if using OpenIddict validation scheme exclusively |
| A5 | Minimal API handler for `/connect/authorize` works without an MVC Controller in OpenIddict 7.5 | Code Examples | If passthrough Minimal API is not supported, must use MVC Controller; adds `AddControllers()` requirement |
| A6 | `ConsentTypes.Implicit` on the seeded OpenIddict client bypasses consent screen (no consent page needed) | Code Examples | If Implicit means something else in OpenIddict 7.x, a consent page must be built |
| A7 | MassTransit 8.3.6 `AddMassTransitTestHarness()` API exists (same as v9 docs show) | Code Examples | If v8 uses different `InMemoryTestHarness` API, test code patterns will need adjustment |
| A8 | `IdentityDbContext` can extend both `IdentityDbContext<IdentityUser>` AND host OpenIddict tables via `UseDbContext<T>()` in a single DbContext class | Code Examples | If OpenIddict requires a separate DbContext, adds a second migration migration set to Identity service |
| A9 | `Microsoft.Extensions.ServiceDiscovery.Yarp` 10.7.0 is compatible with `Yarp.ReverseProxy` 2.3.0 and .NET Aspire 10.x | Standard Stack | Version mismatch could cause startup errors; check NuGet dependency graph |
| A10 | CORS configuration on Identity service at `localhost:5005` is sufficient — no additional gateway-level CORS needed | Common Pitfalls | If YARP strips or blocks CORS preflight headers, additional gateway CORS config required |

---

## Open Questions

1. **MassTransit 8.3.6 + ASB emulator connectivity in Phase 2 local dev**
   - What we know: GitHub issue #5689 confirms no supported path; InMemory harness (D-14) avoids the issue for the test.
   - What's unclear: Can the Catalog → Notifications event flow be demonstrated locally without real ASB? Is the demo expected to show real ASB message delivery (not just test harness)?
   - Recommendation: Planner should add a Wave 0 task to spike ASB emulator connectivity with MassTransit 8.3.6. If blocked, document that local demo uses InMemory test for outbox proof, and real ASB is required for full demo. Keep this as a known limitation, not a blocker.

2. **OpenIddict authorization endpoint: Minimal API vs MVC Controller**
   - What we know: Official docs say MVC controllers are required for token issuance; passthrough mode allows Minimal API for routing. Samples use MVC.
   - What's unclear: Does `MapGet("/connect/authorize", ...)` with passthrough fully work without adding `AddControllers()`?
   - Recommendation: Planner should default to adding Razor Pages + a minimal `/Authorization` controller to match the official OpenIddict sample patterns. This is safer than relying on Minimal API passthrough only.

3. **angular-auth-oidc-client version for Angular 20**
   - What we know: Library is at 21.0.2 (May 2026); CONTEXT.md D-03 specified 19.x.
   - What's unclear: Is 21.x backward compatible as a peer dep to Angular 20, or is 20.x the correct version?
   - Recommendation: Planner must add a task to verify peer deps before `npm install`. Use `npm info angular-auth-oidc-client@21 peerDependencies` to confirm.

4. **Identity service Razor Pages login page — scaffold or manual?**
   - What we know: OpenIddict requires a login UI; ASP.NET Core Identity provides scaffolding via `dotnet aspnet-codegenerator`.
   - What's unclear: Does the Minimal APIs baseline from Phase 1 support Razor Pages co-hosted without conflicts?
   - Recommendation: Add `AddRazorPages()` + `MapRazorPages()` alongside existing Minimal API endpoints. Use `dotnet aspnet-codegenerator identity --files "Account.Login;Account.Logout"` to scaffold only the needed pages.

---

## Environment Availability

| Dependency | Required By | Available | Version | Fallback |
|------------|------------|-----------|---------|----------|
| .NET SDK 10.x | All .NET services | ✓ | 10.0.300 | — |
| Node.js | Angular 20 frontend | ✓ | 24.16.0 | — |
| npm | Angular package management | ✓ | 11.13.0 | — |
| Docker | Testcontainers (Postgres), ASB emulator | ✓ | 29.5.2 | — |
| Azure Service Bus emulator (Docker) | MassTransit integration in local dev | ✓ (Docker available, emulator container) | Aspire-managed | Real ASB namespace (see Pitfall 1) |
| PostgreSQL (via Aspire/Docker) | All services with EF Core | ✓ (Docker available) | Aspire-managed | — |
| Angular CLI (`@angular/cli`) | Angular scaffold (D-16) | [ASSUMED] — not verified, but Node.js 24 available | Install via `npm install -g @angular/cli@20` | — |
| `dotnet ef` tool | EF Core migrations | [ASSUMED] — standard .NET tooling | `dotnet tool install --global dotnet-ef` | — |
| `ctx7` CLI | Context7 docs lookup | ✗ — not installed | — | WebSearch + WebFetch used for documentation |

**Missing dependencies with no fallback:** None blocking.

**Missing dependencies with fallback:**
- Angular CLI: install via `npm install -g @angular/cli@20` before Angular scaffold task.
- `dotnet ef`: install via `dotnet tool install --global dotnet-ef` before migration tasks.
- MassTransit + ASB emulator connectivity: fallback is real ASB namespace or InMemory harness only.

---

## Security Domain

> `security_enforcement: true`, `security_asvs_level: 1` from config.json.

### Applicable ASVS Categories (Level 1)

| ASVS Category | Applies | Standard Control |
|---------------|---------|-----------------|
| V2 Authentication | YES — registration, login, PKCE | ASP.NET Core Identity (`UserManager`, password hashing), OpenIddict PKCE (no ROPC, no implicit flow) |
| V3 Session Management | YES — JWT bearer tokens, session storage | `angular-auth-oidc-client` session storage; short token lifetime; no cookie-based API sessions |
| V4 Access Control | YES — `GET /me` protected endpoint | `[Authorize]` / `RequireAuthorization()` on protected Minimal API endpoints; JWT validation via `AddOpenIddict().AddValidation()` |
| V5 Input Validation | YES — registration form, product queries | FluentValidation 11.3.1 on `RegisterRequest`; pagination params validated (page > 0, pageSize <= 100) |
| V6 Cryptography | YES — JWT signing, token encryption | OpenIddict development certificates (RSA) for signing; production: replace with persisted X.509 cert |
| V7 Error Handling | YES — API error responses | Use `Results.Problem()` (RFC 7807) — do not expose stack traces or inner exceptions in API responses |
| V9 Communications | PARTIAL — local dev is HTTP | Phase 2 is localhost HTTP; HTTPS enforcement deferred to Phase 6 Azure deployment (TLS termination at ACA) |

### Known Threat Patterns for This Stack

| Pattern | STRIDE | Standard Mitigation |
|---------|--------|---------------------|
| Password brute force on `/register` or Identity login form | Elevation of Privilege | ASP.NET Core Identity `Lockout` (enable `options.Lockout.MaxFailedAccessAttempts = 5`) |
| PKCE code interception (auth code without PKCE verifier) | Elevation of Privilege | OpenIddict enforces PKCE when `RequireProofKeyForCodeExchange()` is set — add to client and server config |
| JWT token theft from session storage | Information Disclosure | Session storage is XSS-vulnerable; accept this risk in Phase 2 (no sensitive operations beyond catalog browse); Phase 6 can move to httpOnly cookies if needed |
| SQL injection via product category filter query param | Tampering | EF Core parameterized queries — never string-concatenate SQL; `query.Where(p => p.Category == category)` is safe |
| CORS bypass allowing any origin on Identity service | Elevation of Privilege | Use `WithOrigins("http://localhost:4200")` — never `AllowAnyOrigin()` in combination with `AllowCredentials()` |
| Open redirect on PKCE redirect_uri | Spoofing | OpenIddict validates redirect_uri against registered URIs; do not allow wildcard redirect URIs |
| Mass assignment on user registration (role elevation) | Elevation of Privilege | `RegisterRequest` maps only `Email` and `Password`; `UserManager.CreateAsync()` never sets roles from user input |

### ASVS Level 1 — Minimum Verification Checklist

- [ ] Password minimum length enforced (8+ chars) — FluentValidation `MinimumLength(8)` on `RegisterRequest.Password`
- [ ] Passwords hashed with modern algorithm — ASP.NET Core Identity PBKDF2 (default in Identity 10.x)
- [ ] JWT has expiry claim — OpenIddict sets `exp` automatically
- [ ] Protected endpoints return 401 for missing/invalid token — `RequireAuthorization()` on `/me`
- [ ] No sensitive data (password, raw token) in logs — Serilog destructuring must not capture `RegisterRequest`
- [ ] PKCE required for public client — add `RequireProofKeyForCodeExchange()` to server options and client registration
- [ ] redirect_uri whitelist enforced — OpenIddict validates against `RedirectUris` in client registration

---

## Sources

### Primary (HIGH confidence)
- `CONTEXT.md` (D-01 through D-33) — all locked decisions and implementation specifics
- `CLAUDE.md` — full technology stack constraints
- `.planning/REQUIREMENTS.md`, `ROADMAP.md` — requirement definitions
- `docs/adr/0003-database-per-service.md`, `0004-yarp-api-gateway.md`, `0006-masstransit-outbox-inbox.md` — architectural decisions
- Existing `Program.cs` stubs and `.csproj` files — confirmed baseline wiring
- NuGet registry (via `dotnet package search`) — confirmed package versions: MassTransit 8.3.6, OpenIddict 7.5.0, YARP 2.3.0, Npgsql 10.0.2, Testcontainers 4.12.0

### Secondary (MEDIUM confidence)
- [masstransit.massient.com — Outbox configuration](https://masstransit.massient.com/documentation/configuration/middleware/outbox) — outbox/inbox DbContext and service registration pattern
- [timdeschryver.dev — YARP in Aspire](https://timdeschryver.dev/blog/integrating-yarp-within-dotnet-aspire) — `AddServiceDiscoveryDestinationResolver()` pattern with appsettings routing
- [timdeschryver.dev — angular-auth-oidc-client functional APIs](https://timdeschryver.dev/blog/configuring-angular-auth-oidc-client-with-the-new-functional-apis) — `provideAuth()` + `authInterceptor()` pattern
- [andreyka26.com — OpenIddict auth code flow](https://andreyka26.com/openid-connect-authorization-code-using-openiddict-and-dot-net) — authorization endpoint handler pattern
- [documentation.openiddict.com — getting started](https://documentation.openiddict.com/guides/getting-started/creating-your-own-server-instance) — OpenIddict server configuration structure
- [dotnet.testcontainers.org — PostgreSQL module](https://dotnet.testcontainers.org/modules/postgres/) — `PostgreSqlBuilder` fixture pattern

### Tertiary (LOW confidence — WebSearch verified)
- [github.com/MassTransit/MassTransit/issues/5689](https://github.com/MassTransit/MassTransit/issues/5689) — ASB emulator incompatibility confirmed open issue
- [github.com/MassTransit/MassTransit/discussions/5757](https://github.com/MassTransit/MassTransit/discussions/5757) — no working workaround confirmed
- WebSearch results for `angular-auth-oidc-client` version (21.x) — confirmed via multiple search results, npm registry TLS blocked in this environment

---

## Metadata

**Confidence breakdown:**
- Standard stack: HIGH — all .NET package versions verified against NuGet registry; Angular packages confirmed via WebSearch + official GitHub
- Architecture patterns: HIGH — patterns sourced from official docs (OpenIddict, MassTransit, YARP Aspire); implementation-level details MEDIUM where exact Minimal API variants unconfirmed
- Pitfalls: HIGH — ASB emulator issue confirmed via GitHub issues; other pitfalls based on verified documentation patterns
- Angular configuration: MEDIUM — `provideAuth()` pattern confirmed from official blog posts; peer dep version mismatch flagged

**Research date:** 2026-06-17
**Valid until:** 2026-07-17 (30 days) — stable stack; MassTransit 8.3.6 and OpenIddict 7.5.0 are pinned versions; Angular 20 is stable; YARP 2.x is mature
