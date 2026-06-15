# Phase 2: Identity, Catalog & Gateway - Context

**Gathered:** 2026-06-16
**Status:** Ready for planning

<domain>
## Phase Boundary

Ship three functional pillars through a YARP gateway with MassTransit outbox/inbox wired from day one:
1. **Identity service** — register, login (JWT via OpenIddict PKCE), GET /me, seeded demo users
2. **Catalog service** — list/view products (pagination + category filtering), seeded 20–50 SKUs, CatalogSeeded event through transactional outbox
3. **YARP gateway** — separate 9th service, appsettings-based routing, JWT pass-through to downstream services
4. **Angular shell** — scaffolded standalone zoneless app with /catalog, /product/:id, /login, /register pages via Angular Material 20
5. **Notifications stub** — first real MassTransit consumer, receives CatalogSeeded via idempotent inbox
6. **Tests.Common** — shared test infrastructure (Testcontainers, WebApplicationFactory base, builders)

**Requirements in scope:** IDN-01, IDN-02, IDN-03, IDN-04, CAT-01, CAT-02, CAT-03, FE-01, FE-04, INF-01, INF-02

</domain>

<decisions>
## Implementation Decisions

### Auth Flow (IDN-01, IDN-02, IDN-03, IDN-04)

- **D-01:** OpenIddict PKCE auth code flow is the authentication mechanism. Angular redirects to `/connect/authorize`, receives an auth code, exchanges it for a JWT. No ROPC, no custom JWT endpoint — full OIDC.
- **D-02:** Angular SPA client (client_id, redirect_uri, allowed scopes) is seeded at startup via a `DbInitializer` / `IHostedService` that writes into OpenIddict's EF Core tables on first run. No manual configuration steps required to run the demo.
- **D-03:** Angular uses `angular-auth-oidc-client` to handle the PKCE callback, token storage (session storage), and silent renew. An HTTP interceptor attaches the `Bearer` token to every `HttpClient` call.
- **D-04:** User store uses ASP.NET Core Identity tables inside the Identity service's own PostgreSQL database. DB-per-service boundary is strictly maintained — no other service reads the Identity DB.
- **D-05:** Demo user accounts are seeded by the same `DbInitializer` that seeds the OpenIddict client registration. At least 2 seeded users (e.g. `demo@example.com` / `demo123`).

### YARP Gateway (new service)

- **D-06:** YARP gateway is a separate 9th service at `src/services/gateway/ECommerce.Gateway.API/` with its own `Gateway.sln`. It is **not** co-hosted in the Identity service.
- **D-07:** All routes are defined in the `ReverseProxy` section of `appsettings.json` — no C# code-based route registration. Aspire injects cluster destination URLs via environment variables.
- **D-08:** The gateway does **not** validate JWTs. It forwards the `Authorization` header as-is to downstream services. Each service validates the JWT against OpenIddict's OIDC discovery endpoint (`/connect/.well-known/openid-configuration`).
- **D-09:** Gateway is added to both the Aspire AppHost (as the 10th resource) and the CI matrix (as the 11th solution). CI matrix now covers: Contracts, ecommerce.AppHost, Gateway, and the 8 service solutions.

### MassTransit Outbox / Inbox (INF-01, INF-02)

- **D-10:** Only Catalog service (outbox publisher) and Notifications service (inbox consumer) get MassTransit wired in Phase 2. The other 6 service stubs remain MT-free until their respective phases.
- **D-11:** The proof event is `ECommerce.Catalog.Events.V1.CatalogSeeded` — published once by the Catalog service's seeder when the demo catalog is loaded. This replaces the `Placeholder.cs` in `Contracts/Catalog/Events/V1/`.
- **D-12:** The `CatalogSeeded` record includes at minimum: `Guid SeedId`, `int ItemCount`, `DateTimeOffset SeededAt` (plus `IMessageEnvelope` fields: `MessageId`, `CorrelationId`, `CausationId`, `OccurredAt`).
- **D-13:** Notifications service consumes `CatalogSeeded` and persists a placeholder inbox entry (demonstrates the inbox; no real notification logic needed in Phase 2).
- **D-14:** The forced redelivery test is an xUnit integration test using the MassTransit InMemory test harness: publishes `CatalogSeeded` twice with the same `MessageId`, verifies the Notifications consumer processed it exactly once. No ASB emulator needed for this test.
- **D-15:** MassTransit 8.3.6 is pinned explicitly in every `.csproj` that references it. No floating versions. `MassTransit.EntityFrameworkCore` provides the outbox/inbox tables; `MassTransit.Testing` provides the InMemory harness for tests.

### Angular Shell (FE-01, FE-04)

- **D-16:** Angular app scaffolded via `ng new ecommerce-app --routing --style=scss` with `provideZonelessChangeDetection()` enabled. Angular 20, standalone components, signals — matches CLAUDE.md spec exactly.
- **D-17:** UI component library: Angular Material 20. Provides mat-card (product grid), mat-form-field (auth forms), mat-toolbar (nav) with minimal setup effort.
- **D-18:** HTTP: `provideHttpClient(withFetch())` as per CLAUDE.md. A single `authInterceptor` (functional interceptor) attaches `Authorization: Bearer {token}` from `angular-auth-oidc-client` to all calls.
- **D-19:** In Phase 2, Angular runs via `ng serve` on `localhost:4200`. It is **not** added to the Aspire AppHost. A `proxy.conf.json` proxies `/api/*` to the YARP gateway URL. Aspire manages backend services only.
- **D-20:** Angular routes: `/catalog` (product list), `/product/:id` (product detail), `/login` (PKCE redirect trigger), `/register` (registration form), `/callback` (OIDC callback handler via angular-auth-oidc-client).
- **D-21:** Vitest via `@analogjs/vitest-angular` is configured at scaffold time (`vitest.config.ts`). Angular tests are written from the start — not deferred to a later phase.

### Test Infrastructure

- **D-22:** Test projects are added only for services actively implemented in Phase 2: Identity, Catalog, Notifications, Gateway.
- **D-23:** Test project naming: `ECommerce.{ServiceName}.Tests` (e.g. `ECommerce.Catalog.Tests`). Located at `src/services/{name}/ECommerce.{ServiceName}.Tests/`, sibling to the API project, added to the service's own `.sln`.
- **D-24:** Shared test infrastructure project: `src/building-blocks/Tests.Common/ECommerce.Tests.Common.csproj`. Contains: `PostgresFixture` (Testcontainers), `WebApplicationFactory` base class, test data builders (`ProductBuilder`, `UserBuilder`). All Phase 2 test projects reference it.
- **D-25:** Test pattern — two-class per test suite:
  - `*Tests` class: contains `[Fact]` methods that read as specifications (calls steps)
  - `*Steps` class: defines `Given_...()`, `When_...()`, `Then_...()` methods (setup, action, assertion)
  - Method naming in Tests class: `MethodName_StateUnderTest_ExpectedBehavior`
- **D-26:** Test project folder structure: each project has `Unit/` and `Integration/` subdirectories. Fast unit tests (no I/O) go in `Unit/`; `WebApplicationFactory` + Testcontainers tests go in `Integration/`.
- **D-27:** Framework: xUnit v3 + FluentAssertions + NSubstitute. Builder classes in `Tests.Common` for test data construction.
- **D-28:** Database isolation: per-class `IClassFixture<PostgresFixture>` — each test class gets its own Testcontainers Postgres instance for full isolation.
- **D-29:** API tests use `Microsoft.AspNetCore.Mvc.Testing` `WebApplicationFactory` for Identity and Catalog — exercises the full ASP.NET Core pipeline (routing, middleware, endpoint wiring).
- **D-30:** CI update: Phase 2 adds 4 test projects to the CI matrix. `dotnet test --collect "XPlat Code Coverage"` collects coverage; report uploaded as artifact. No coverage gate in Phase 2.

### Namespaces and Code Structure

- **D-31:** Root namespace for all Phase 2 service code follows the project name: `ECommerce.{ServiceName}.API` (e.g. `ECommerce.Catalog.API`, `ECommerce.Identity.API`, `ECommerce.Gateway.API`).
- **D-32:** Sub-namespace structure: vertical-slice feature folders. Each feature lives in one folder containing endpoint handler, DTOs, entity, and any service it needs (e.g. `ECommerce.Catalog.API.Products` contains `ProductsEndpoints.cs`, `Product.cs`, `ProductDto.cs`).
- **D-33:** In the Contracts library, only `ECommerce.Catalog.Events.V1.Placeholder` is replaced in Phase 2 (with `CatalogSeeded`). All other placeholder records remain unchanged until their owning service gains real message types.

### Claude's Discretion

- Catalog product schema (fields beyond Name, Price, Stock, Category) — Claude can choose a sensible set of fields for the demo (e.g. Description, ImageUrl, SKU). No specific requirements from the user.
- Pagination style for `GET /products` — offset-based is acceptable for Phase 2 (cursor-based can come in Phase 3+ when performance matters).
- OpenIddict OIDC scopes — standard `openid profile email` scopes are sufficient for Phase 2.
- YARP route prefixes — Claude decides the gateway route structure (e.g. `/api/identity/...`, `/api/catalog/...`).

</decisions>

<canonical_refs>
## Canonical References

**Downstream agents MUST read these before planning or implementing.**

### Phase Requirements and Roadmap
- `.planning/ROADMAP.md` — Phase 2 goal, 5 success criteria (SC1–SC5), requirements mapping (IDN-01/02/03/04, CAT-01/02/03, FE-01/04, INF-01/02)
- `.planning/REQUIREMENTS.md` — Full requirement definitions; Phase 2 traceability section

### Project Structure and Constraints
- `.planning/PROJECT.md` — Repo directory layout, multi-solution mono-repo rationale, key architecture decisions
- `CLAUDE.md` — Full technology stack constraints: MassTransit 8.3.6 pin, Mapperly (not AutoMapper), OpenIddict 6.x, Angular 20 conventions, what NOT to use and why

### Architecture Decision Records (all in `docs/adr/`)
- `docs/adr/0003-database-per-service.md` — DB-per-service rule; Identity DB isolation requirement
- `docs/adr/0004-yarp-api-gateway.md` — YARP 2.x choice, appsettings routing, JWT delegation design; **MUST read before planning Gateway service**
- `docs/adr/0006-masstransit-outbox-inbox.md` — MassTransit 8.3.6 pin (Apache-2.0), outbox/inbox pattern, EmulatorHost() absence workaround, EOL note; **MUST read before wiring MassTransit**

### Prior Phase Context
- `.planning/phases/01-foundations/01-CONTEXT.md` — Locked decisions from Phase 1: project naming (ECommerce.{ServiceName}.API), .sln per service pattern, OTel/Serilog wiring, Contracts namespace convention

### Existing Scaffold
- `src/building-blocks/Contracts/Contracts.csproj` — Contracts library with placeholder records; `Contracts/Catalog/Events/V1/Placeholder.cs` is the file to replace with `CatalogSeeded`
- `src/services/identity/ECommerce.Identity.API/Program.cs` — Existing stub to expand with OpenIddict + ASP.NET Core Identity
- `src/services/catalog/ECommerce.Catalog.API/Program.cs` — Existing stub to expand with EF Core + Catalog endpoints
- `src/services/notifications/ECommerce.Notifications.API/Program.cs` — Existing stub to expand with MassTransit consumer
- `src/ecommerce.AppHost/Program.cs` — AppHost to update with Gateway service reference

</canonical_refs>

<code_context>
## Existing Code Insights

### Reusable Assets
- All 8 `Program.cs` stubs share identical Serilog + OTel + `/health` wiring — reuse as the baseline pattern when expanding each service. Do not remove these wiring lines.
- `src/building-blocks/Contracts/Contracts.csproj` — zero-dependency class library; maintain this constraint when adding `CatalogSeeded`.
- `ECommerce.Identity.API.csproj` and `ECommerce.Catalog.API.csproj` already reference Contracts and have OTel/Serilog packages. Add EF Core, OpenIddict, MassTransit etc. on top.

### Established Patterns (from Phase 1 Context)
- `net10.0 + ImplicitUsings + Nullable enable` — all projects use this baseline; maintain it.
- One `.sln` per service in `src/services/{service-name}/`; each `.sln` references the API `.csproj` and `Contracts.csproj` via relative path.
- Gateway service follows the same pattern: `src/services/gateway/Gateway.sln` referencing `ECommerce.Gateway.API.csproj` and `Contracts.csproj`.
- MassTransit 8.3.6 explicitly pinned in every `.csproj` that uses it — no floating `*` or `8.*` versions.

### Integration Points
- `src/ecommerce.AppHost/Program.cs` — add `builder.AddProject<Projects.ECommerce_Gateway_API>()` to wire the Gateway into the Aspire dashboard and docker-compose output.
- `.github/workflows/ci.yml` — extend the matrix to add `Gateway.sln` and the 4 new test project `.sln` files.
- Contracts `Catalog/Events/V1/` — replace `Placeholder.cs` with `CatalogSeeded.cs`; do not change the namespace `ECommerce.Catalog.Events.V1`.
- Angular proxy config (`proxy.conf.json`) — points `/api/*` at the YARP gateway's Aspire-assigned localhost port.

### Creative Options Enabled by Architecture
- Identity service can expose standard OpenIddict endpoints (`/connect/token`, `/connect/authorize`, `/connect/.well-known/openid-configuration`) with minimal custom code — OpenIddict wires these automatically via `MapOpenIdConnectEndpoints()`.
- YARP appsettings routes can forward `/api/catalog/**` to Catalog service and `/api/identity/**` to Identity service — keeps the gateway config readable and route-prefix-consistent for Angular.

</code_context>

<specifics>
## Specific Ideas

- `CatalogSeeded` contract shape: `record CatalogSeeded(Guid SeedId, int ItemCount, DateTimeOffset SeededAt) : IMessageEnvelope { ... }` — use a primary constructor with envelope fields implemented explicitly.
- Demo user seed example: `demo@example.com` / `demo123` and `admin@example.com` / `admin123` — simple enough for a live demo without exposing real credentials.
- YARP route prefix convention: `/api/catalog/` → Catalog service, `/api/identity/` → Identity service, `/api/notifications/` → Notifications service. Angular calls `/api/...` and the proxy proxies to the gateway.
- Angular feature folder layout: `src/app/features/catalog/` (catalog-list, product-detail components), `src/app/features/auth/` (login, register, callback components). Each feature folder is self-contained.
- `Tests.Common` builder example: `new ProductBuilder().WithName("Test Widget").WithPrice(19.99m).WithCategory("Electronics").WithStock(100).Build()` returns a `Product` entity ready for DB seeding in integration tests.

</specifics>

<deferred>
## Deferred Ideas

- Angular added to Aspire AppHost as a Node.js resource — deferred to Phase 5+ when all backend services are running together and a unified dashboard view is more valuable.
- Playwright E2E tests — CLAUDE.md lists Playwright for E2E. Deferred to Phase 4 when the full checkout saga can be demonstrated end-to-end.
- Coverage gate in CI — collect coverage from Phase 2 onward; set an enforcement threshold in Phase 3+ after a baseline is established.
- MassTransit wiring for Cart, Orders, Checkout, Payments, Fulfillment services — each gets MT in its implementing phase (Phases 3–5).
- Real OpenIddict scopes beyond `openid profile email` (e.g. per-service resource scopes) — sufficient for Phase 2 demo; resource-server scopes can be added in Phase 4+ when multi-service auth matters.

</deferred>

---

*Phase: 2-Identity, Catalog & Gateway*
*Context gathered: 2026-06-16*
