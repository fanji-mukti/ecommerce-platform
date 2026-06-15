# Phase 2: Identity, Catalog & Gateway - Discussion Log

> **Audit trail only.** Do not use as input to planning, research, or execution agents.
> Decisions are captured in CONTEXT.md — this log preserves the alternatives considered.

**Date:** 2026-06-16
**Phase:** 2-Identity, Catalog & Gateway
**Areas discussed:** Auth flow design, YARP gateway placement, Outbox/inbox proof event, Angular shell setup, Unit Test, Namespac

---

## Auth Flow Design

| Option | Description | Selected |
|--------|-------------|----------|
| PKCE auth code flow | Full OIDC — Angular redirects to OpenIddict /connect/authorize, code exchanged for JWT. Strongest portfolio signal. | ✓ |
| Password grant (ROPC) | Angular POSTs credentials to /connect/token. Simpler, no redirects, but deprecated in OAuth 2.1. | |
| Custom JWT endpoint | POST /auth/login returns JWT, bypasses OpenIddict OIDC flows entirely. | |

**User's choice:** PKCE auth code flow

| Option | Description | Selected |
|--------|-------------|----------|
| Seeded at startup | DbInitializer seeds Angular SPA client into OpenIddict EF Core tables on first run. | ✓ |
| Static config in appsettings | Client registrations in appsettings.json via AddApplications(). | |

**User's choice:** Seeded at startup

| Option | Description | Selected |
|--------|-------------|----------|
| angular-auth-oidc-client (session storage) | Library manages PKCE callback, token storage, silent renew automatically. | ✓ |
| localStorage (manual) | Simple but exposes token to XSS. | |
| HttpOnly cookie (BFF pattern) | Most secure but requires BFF proxy layer — out of scope Phase 2. | |

**User's choice:** angular-auth-oidc-client (session storage)

| Option | Description | Selected |
|--------|-------------|----------|
| Identity tables in own PostgreSQL DB | Standard DB-per-service pattern. AspNetUsers + OpenIddict tables in Identity service DB. | ✓ |
| Shared users table with other services | Violates ADR-0003 DB-per-service rule. | |

**User's choice:** Identity tables in Identity service's own PostgreSQL DB

---

## YARP Gateway Placement

| Option | Description | Selected |
|--------|-------------|----------|
| Separate 9th service | src/services/gateway/ECommerce.Gateway.API/ with own .sln, Aspire resource, CI entry. | ✓ |
| Co-hosted in Identity service | YARP middleware in ECommerce.Identity.API. Fewer containers but muddies Identity bounded context. | |

**User's choice:** Separate 9th service

| Option | Description | Selected |
|--------|-------------|----------|
| appsettings.json (declarative) | Routes in ReverseProxy section; Aspire injects cluster URLs via env vars. Code-free. | ✓ |
| Code-based config | Routes in Program.cs via C# builders. More flexible but duplicates what appsettings does declaratively. | |

**User's choice:** appsettings.json

| Option | Description | Selected |
|--------|-------------|----------|
| Pass-through only | Forward Authorization header as-is; downstream services validate JWT against OpenIddict discovery. | ✓ |
| Validate at gateway + forward claims | Gateway validates JWT and adds X-User-Id/X-User-Roles headers. Adds complexity and SSRF surface. | |

**User's choice:** Pass-through only

| Option | Description | Selected |
|--------|-------------|----------|
| Add to both Aspire AppHost and CI matrix | Gateway in Aspire as 10th resource; Gateway.sln as 11th CI matrix entry. | ✓ |
| Gateway in Aspire only, not CI yet | Skip CI until Phase 2 complete. Risks undetected build failures. | |

**User's choice:** Add to both Aspire AppHost and CI matrix

---

## Outbox/Inbox Proof Event

| Option | Description | Selected |
|--------|-------------|----------|
| CatalogSeeded | Published once when seed completes. Simple, deterministic, easy to verify. | ✓ |
| ProductViewed | Published on every GET /products/{id}. More realistic but noisier in tests. | |
| UserRegistered from Identity instead | Shifts proof event off Catalog onto Identity→Notifications flow. | |

**User's choice:** CatalogSeeded

| Option | Description | Selected |
|--------|-------------|----------|
| Notifications service (inbox consumer) | Gets first real consumer wired; persists placeholder inbox entry. Proves cross-service inbox. | ✓ |
| Self-consumer inside Catalog | Catalog consumes its own event — unusual pattern. | |
| Test-only consumer | InMemory harness only; no real running consumer across ASB hop. | |

**User's choice:** Notifications service

| Option | Description | Selected |
|--------|-------------|----------|
| Catalog (outbox) and Notifications (inbox) only | Minimal scope for Phase 2. Other services get MT in their phases. | ✓ |
| All 8 services in Phase 2 | Wire MT everywhere now. Over-scopes Phase 2. | |

**User's choice:** Catalog and Notifications only

| Option | Description | Selected |
|--------|-------------|----------|
| xUnit InMemory harness + forced redelivery | Publishes CatalogSeeded twice, verifies single processing. No ASB emulator needed. Automatable in CI. | ✓ |
| Manual test via ASB emulator dead-letter redelivery | Hard to automate; not reliable as a CI gate. | |

**User's choice:** xUnit InMemory harness + forced redelivery

---

## Angular Shell Setup

| Option | Description | Selected |
|--------|-------------|----------|
| ng new with zoneless + standalone flags | Angular 20, zoneless, standalone, signals — matches CLAUDE.md exactly. No Nx overhead. | ✓ |
| Nx workspace with Angular plugin | Monorepo management but significant toolchain overhead for single app. | |

**User's choice:** ng new with zoneless flags

| Option | Description | Selected |
|--------|-------------|----------|
| Angular Material 20 | Minimal setup, clean defaults for forms and card grids. Portfolio-standard. | ✓ |
| PrimeNG 18 | Richer components for catalog grids but higher setup cost. | |
| Tailwind CSS only | Full control but all components hand-rolled. | |

**User's choice:** Angular Material 20

| Option | Description | Selected |
|--------|-------------|----------|
| HttpClient with provideHttpClient(withFetch()) | CLAUDE.md specifies this. Bearer token via angular-auth-oidc-client interceptor. | ✓ |
| GraphQL (Apollo) | Out of scope — REST API backend. | |

**User's choice:** provideHttpClient(withFetch())

| Option | Description | Selected |
|--------|-------------|----------|
| Standalone ng serve (not in Aspire) | Simpler — Angular proxies /api/* to gateway. Phase 5+ for Aspire integration. | ✓ |
| Add Angular to Aspire AppHost as Node.js resource | Unified dashboard but adds Aspire Node.js complexity for Phase 2. | |

**User's choice:** Standalone ng serve on localhost:4200

---

## Unit Tests

**Which services get test projects:** Identity, Catalog, Notifications, Gateway only (not the 5 uninmplemented stubs).

**Test project location:** Same service folder sibling (src/services/catalog/ECommerce.Catalog.Tests/), in service's own .sln.

**Naming:** ECommerce.{ServiceName}.Tests

**Framework:** xUnit v3 + FluentAssertions + NSubstitute

**DB for integration tests:** Testcontainers-for-.NET PostgreSQL (per-class IClassFixture — full isolation)

**API tests:** WebApplicationFactory for Identity and Catalog (full ASP.NET Core pipeline)

**Test pattern:**
- Two-class per test suite: *Tests class (contains [Fact] methods) + *Steps class (Given_/When_/Then_ methods)
- Method naming: MethodName_StateUnderTest_ExpectedBehavior
- BDD Given-When-Then naming in Steps class

**Folder structure:** Unit/ and Integration/ subdirectories within each test project

**Shared infrastructure:** src/building-blocks/Tests.Common/ECommerce.Tests.Common.csproj
  - PostgresFixture (Testcontainers)
  - WebApplicationFactory base
  - Builder classes (ProductBuilder, UserBuilder)

**CI:** Added to matrix in Phase 2; coverage collected (no gate)

**Angular:** Vitest via @analogjs/vitest-angular configured at scaffold time

---

## Namespaces

**Root namespace:** ECommerce.{ServiceName}.API (mirrors project name — e.g. ECommerce.Catalog.API)

**Sub-namespace structure:** Vertical-slice feature folders (e.g. ECommerce.Catalog.API.Products contains endpoint, handler, DTOs, entity)

**Contracts:** Only ECommerce.Catalog.Events.V1.CatalogSeeded replaces a placeholder in Phase 2. All other placeholders unchanged.

---

## Claude's Discretion

- Catalog product schema fields (beyond Name, Price, Stock, Category) — Claude decides sensible set for demo
- Pagination style for GET /products — offset-based acceptable for Phase 2
- OpenIddict OIDC scopes — standard openid/profile/email sufficient
- YARP gateway route prefix structure — Claude decides the /api/{service}/... prefix convention

## Deferred Ideas

- Angular in Aspire AppHost as Node.js resource — Phase 5+
- Playwright E2E tests — Phase 4 (saga demo worthy of E2E coverage)
- Coverage gate in CI — establish baseline in Phase 2, add gate in Phase 3+
- MassTransit for remaining 6 services — each phase adds MT when that service gains real consumers
- Resource-server OIDC scopes per service — Phase 4+ when multi-service auth matters
