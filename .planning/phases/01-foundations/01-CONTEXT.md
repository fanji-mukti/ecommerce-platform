# Phase 1: Foundations - Context

**Gathered:** 2026-06-03
**Status:** Ready for planning

<domain>
## Phase Boundary

Scaffold the entire repo structure (8 service solution stubs + Aspire AppHost), lock the Contracts library interface and namespace shape, wire observability (OTel + Serilog) from day 1, write 8 foundational MADR ADRs, and set up GitHub Actions CI. This phase delivers a runnable `docker compose up` baseline with all infrastructure resources visible in the Aspire dashboard. No business logic is implemented — all services are stubs with `/health` endpoints only.

**Requirements in scope:** REPO-01, REPO-02, REPO-03, CON-01, CON-02, CON-03, ADR-01, ADR-02, INF-03, INF-04

</domain>

<decisions>
## Implementation Decisions

### Contracts Library (CON-01, CON-02, CON-03)

- **D-01:** Envelope expressed as `IMessageEnvelope` interface (not an abstract base record) with 4 required properties: `Guid MessageId`, `Guid CorrelationId`, `Guid CausationId`, `DateTimeOffset OccurredAt`. All domain message records implement this interface.
- **D-02:** Phase 1 defines the `IMessageEnvelope` interface AND stubs all 8 service namespaces with at least one placeholder record per namespace. Namespaces follow the pattern `ECommerce.{ServiceName}.Events.V1` and `ECommerce.{ServiceName}.Commands.V1` (e.g., `ECommerce.Catalog.Events.V1`, `ECommerce.Orders.Commands.V1`). This locks the namespace structure so every later phase follows the same convention.
- **D-03:** Enforcement via project-level constraints only — `Contracts.csproj` has zero NuGet references beyond the SDK itself. No EF Core, MediatR, ASP.NET Core, or domain dependencies. Violations are caught in PR review; no Roslyn analyzer needed in Phase 1.

### Service Stubs (REPO-01, REPO-02, INF-03, INF-04)

- **D-04:** All 8 service stubs scaffolded in Phase 1: Catalog, Cart, Checkout, Orders, Identity, Payments, Fulfillment, Notifications.
- **D-05:** Each stub contains: `Program.cs` with `AddOpenTelemetry()`, `UseSerilog()`, `MapHealthChecks("/health")`; a `<ProjectReference>` to `Contracts.csproj`; no business logic or domain code.
- **D-06:** Project naming convention: `ECommerce.{ServiceName}.API` (e.g., `ECommerce.Catalog.API`, `ECommerce.Orders.API`, `ECommerce.Checkout.API`).
- **D-07:** Each service has its own `.sln` file in `src/services/{service-name}/` (e.g., `src/services/catalog/Catalog.sln`). Each solution references `ECommerce.{ServiceName}.API.csproj` and `Contracts.csproj` via relative path — never via NuGet.

### Aspire AppHost (REPO-03)

- **D-08:** AppHost lives at `src/ecommerce.AppHost/` with its own `ecommerce.AppHost.sln`.
- **D-09:** AppHost references all 8 service stubs via `<ProjectReference>` to each `ECommerce.*.API.csproj`. Uses `builder.AddProject<Projects.ECommerce_{ServiceName}_API>()` pattern.
- **D-10:** AppHost provisions all 4 infrastructure resources from day 1: PostgreSQL (one container per service DB, or a shared container with multiple databases for Phase 1), Redis, ASB emulator container, and OTLP/OTel collector. All visible in the Aspire dashboard and exported to `docker-compose.yml` via `aspire publish`.

### Observability (INF-03, INF-04)

- **D-11:** OTel scope: HTTP request traces via `AddAspNetCoreInstrumentation()` + OTLP exporter pointing at the Aspire dashboard endpoint. All 8 stubs appear as separate trace sources in the dashboard.
- **D-12:** Serilog configured with W3C TraceContext enrichment: all log entries include `TraceId` and `SpanId` as structured properties. This satisfies SC5's "correlation ID across simulated service boundaries" — the OTel trace ID flows through HTTP headers and appears in Serilog output.
- **D-13:** Each stub's `/health` endpoint returns `200 OK` with a simple JSON body (ASP.NET Core `HealthCheckOptions.ResponseWriter` default). No complex health check logic in Phase 1.

### GitHub Actions CI

- **D-14:** CI workflow steps: `dotnet restore` → `dotnet build` → `dotnet test`. All 3 steps run on every trigger.
- **D-15:** Triggers: `push` to `main` and `pull_request` targeting `main`.
- **D-16:** Build strategy: matrix per solution. Matrix includes: `Contracts`, `ecommerce.AppHost`, and each of the 8 service solutions (10 items total). Parallel builds with per-solution failure reporting.

### ADRs (ADR-01, ADR-02)

- **D-17:** ADRs follow MADR 4.0 format. Stored in `docs/adr/` with numbered kebab-case filenames (e.g., `0001-record-architectural-decisions.md`).
- **D-18:** 8 ADRs to write in Phase 1 covering: (1) use of MADR format, (2) Azure Service Bus over alternatives, (3) database-per-service rule, (4) YARP as API gateway, (5) saga orchestration over choreography, (6) MassTransit + transactional outbox/inbox, (7) ASB topic-per-producing-context pattern, (8) mono-repo multi-solution structure. These are the foundational decisions every later phase depends on.

</decisions>

<canonical_refs>
## Canonical References

**Downstream agents MUST read these before planning or implementing.**

### Phase Requirements and Roadmap
- `.planning/ROADMAP.md` — Phase 1 goal, 5 success criteria (SC1–SC5), and requirements mapping (REPO-01/02/03, CON-01/02/03, ADR-01/02, INF-03/04)
- `.planning/REQUIREMENTS.md` — Full requirement definitions + traceability table; Phase 1 requirements section

### Project Structure and Constraints
- `.planning/PROJECT.md` — Repo directory layout (`src/services/{name}/`, `src/building-blocks/Contracts/`, `src/frontend/ecommerce-app/`, `infra/`, `docs/adr/`), multi-solution mono-repo rationale, key architectural decisions
- `CLAUDE.md` — Full technology stack constraints, what NOT to use and why, pre-pin verification checklist, MADR ADR format requirement

### Research and Architecture Guidance
- `.planning/research/SUMMARY.md` — Tech stack recommendations, top 5 pitfalls (especially Pitfall #2: bloated Contracts), suggested ADR list for Phase 1 (ADRs 0001–0008), checkout saga event flow table, service minimum-viable specs

### Existing Scaffold
- `src/building-blocks/Contracts/Contracts.csproj` — existing .NET 10 class library to be updated in Phase 1 (remove Class1.cs, add IMessageEnvelope + namespace stubs)

</canonical_refs>

<code_context>
## Existing Code Insights

### Reusable Assets
- `src/building-blocks/Contracts/Contracts.csproj`: Already scaffolded as a `net10.0` class library with `ImplicitUsings` and `Nullable` enabled. Reuse as-is — no property changes needed. Remove `Class1.cs` and populate with `IMessageEnvelope` and namespace stubs.

### Established Patterns
- The `Contracts.csproj` target framework `net10.0` is the established baseline — all service stub projects must target `net10.0` for consistency.
- `ImplicitUsings` + `Nullable enable` are the repo's standard C# project settings (established by the existing Contracts project).

### Integration Points
- The AppHost at `src/ecommerce.AppHost/` will hold project references to all 8 service stubs. This is the single file that wires all services together for local dev.
- `docker-compose.yml` at the repo root is generated by `aspire publish` from the AppHost — do not hand-author it.
- `docs/adr/` directory must be created in Phase 1 (does not exist yet).
- `.github/workflows/ci.yml` must be created in Phase 1 (does not exist yet).

</code_context>

<specifics>
## Specific Ideas

- Service folder names should be lowercase kebab-case matching the service domain: `catalog`, `cart`, `checkout`, `orders`, `identity`, `payments`, `fulfillment`, `notifications`.
- The `ecommerce.AppHost` project name should match the pattern used in `builder.AddProject<Projects.ECommerce_AppHost>()` — Aspire derives the type name from the project name, so `ecommerce.AppHost.csproj` → `Projects.ecommerce_AppHost` (verify at scaffold time).
- For the ASB emulator container in Aspire, use the official Microsoft emulator image (`mcr.microsoft.com/azure-messaging/servicebus-emulator`) or the community image that's most current — verify image name at planning time.

</specifics>

<deferred>
## Deferred Ideas

None — discussion stayed within phase scope.

</deferred>

---

*Phase: 1-Foundations*
*Context gathered: 2026-06-03*
