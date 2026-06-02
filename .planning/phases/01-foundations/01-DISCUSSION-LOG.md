# Phase 1: Foundations - Discussion Log

> **Audit trail only.** Do not use as input to planning, research, or execution agents.
> Decisions are captured in CONTEXT.md — this log preserves the alternatives considered.

**Date:** 2026-06-03
**Phase:** 1-Foundations
**Areas discussed:** Contracts scope, Service stub breadth, Aspire AppHost placement, OTel demo depth, GitHub Actions CI

---

## Contracts Scope

### Envelope base type

| Option | Description | Selected |
|--------|-------------|----------|
| Abstract base record | `public abstract record MessageEnvelope` with the 4 fields. Messages inherit. Strong typing and IDE support. | |
| Interface IMessageEnvelope | Each message record implements the interface explicitly. More flexible; messages stay fully independent records. | ✓ |
| Convention only | Each record has the 4 properties by convention. No shared base. | |

**User's choice:** Interface IMessageEnvelope
**Notes:** Preferred the interface approach — keeps message records fully independent and avoids inheritance hierarchy.

---

### Initial message set

| Option | Description | Selected |
|--------|-------------|----------|
| Envelope interface + all 8 service namespaces stubbed | Create namespace folders with at least one placeholder record per namespace. Locks namespace structure early. | ✓ |
| Envelope interface only — messages grow per phase | Just the IMessageEnvelope interface. Messages added organically per phase. | |
| Envelope + Phase 2 messages now | Define Identity and Catalog messages now so Phase 2 can reference them immediately. | |

**User's choice:** Envelope interface + all 8 service namespaces stubbed
**Notes:** Locking the namespace structure in Phase 1 ensures all phases follow the same convention from the start.

---

### Enforcement of no-EF/MediatR/domain-logic rule

| Option | Description | Selected |
|--------|-------------|----------|
| Project-level dependencies only | Contracts.csproj has zero NuGet references. Violations caught by project ref addition in PR review. | ✓ |
| Roslyn analyzer / ArchUnit rule | Custom analyzer or test that fails the build on forbidden type references. | |
| Code review convention only | Document the rule in the ADR; rely on PR reviews. | |

**User's choice:** Project-level dependencies only
**Notes:** Simplest approach; the Contracts.csproj having zero NuGet refs is self-enforcing in practice.

---

## Service Stub Breadth

### Number of stubs

| Option | Description | Selected |
|--------|-------------|----------|
| All 8 service stubs | Catalog, Cart, Checkout, Orders, Identity, Payments, Fulfillment, Notifications — each gets minimal API + .sln. | ✓ |
| 3–4 representative stubs | Scaffold Identity, Catalog, Checkout now; others come with their phase. | |
| 1 stub to prove the pattern | One reference implementation to validate Aspire + OTel wiring. | |

**User's choice:** All 8 service stubs
**Notes:** Having all service solutions present from Phase 1 means every later phase starts with a project already in place.

---

### Stub contents

| Option | Description | Selected |
|--------|-------------|----------|
| Minimal API + /health + OTel + Serilog | Program.cs with AddOpenTelemetry, UseSerilog, MapHealthChecks, Contracts ref. | ✓ |
| Minimal API + /health only | OTel and Serilog wiring added per phase. | |
| Minimal API + /health + OTel + Serilog + one sample endpoint | Also adds GET /ping to make Aspire dashboard more visually interesting. | |

**User's choice:** Minimal API + /health + OTel + Serilog
**Notes:** Full observability wiring from day 1 is a key project constraint (OTel never retrofitted).

---

### Project naming

| Option | Description | Selected |
|--------|-------------|----------|
| ECommerce.{ServiceName}.API | e.g., ECommerce.Catalog.API. PascalCase, matches Contracts namespace prefix ECommerce.*. | ✓ |
| {ServiceName}.API | e.g., Catalog.API. Shorter, matches eShopOnContainers style. | |
| {ServiceName}Service.API | e.g., CatalogService.API. More verbose. | |

**User's choice:** ECommerce.{ServiceName}.API
**Notes:** Consistency with Contracts namespace root `ECommerce.*` was the deciding factor.

---

## Aspire AppHost Placement

### Location in repo

| Option | Description | Selected |
|--------|-------------|----------|
| src/ecommerce.AppHost/ | Peer of services/, building-blocks/, frontend/ inside src/. Named after the product. Own .sln. | ✓ |
| src/AppHost/ | Shorter path. Generic name but unambiguous. | |
| Repo root level | AppHost at the root next to src/, infra/, docs/. Common in MS samples. | |

**User's choice:** src/ecommerce.AppHost/
**Notes:** Keeps AppHost as a src/-level concern, not mixed with build/infra at the root.

---

### How AppHost references services

| Option | Description | Selected |
|--------|-------------|----------|
| Project references to each service's .API.csproj | Standard Aspire pattern — builder.AddProject<T>() works out of the box. | ✓ |
| Executable references (no project refs) | AppHost launches published executables. Loses live-reload and F5 debugging. | |
| Docker container resources only | All services as published containers. Requires publishing before local dev. | |

**User's choice:** Project references to each service's .API.csproj
**Notes:** Preserves the Aspire DX goal — one `dotnet run` in AppHost starts everything.

---

## OTel Demo Depth

### OTel trace scope

| Option | Description | Selected |
|--------|-------------|----------|
| HTTP traces + structured logs with correlation ID | OTel middleware instruments /health; Serilog enriches logs with TraceId/SpanId. Satisfies SC3 + SC5 with zero custom code. | ✓ |
| HTTP traces + one MassTransit publish/consume roundtrip | Proves messaging pipeline but adds ASB emulator dependency and real MassTransit config in Phase 1. | |
| HTTP traces only — Serilog correlation deferred | Minimal OTel. Correlation ID wiring added in Phase 2+. | |

**User's choice:** HTTP traces + structured logs with correlation ID
**Notes:** The "simulated service boundaries" in SC5 are satisfied by all 8 stubs emitting traces into the same Aspire dashboard — no real cross-service messaging needed in Phase 1.

---

### Infrastructure in AppHost

| Option | Description | Selected |
|--------|-------------|----------|
| Postgres + Redis + ASB emulator + OTEL collector | All 4 infra dependencies from day 1. Matches SC3 exactly. docker-compose.yml is complete from the start. | ✓ |
| Postgres + ASB emulator only | Skip Redis in Phase 1; add when Cart service built in Phase 3. | |
| Postgres only | Minimal infra. ASB emulator and Redis added per phase. | |

**User's choice:** Postgres + Redis + ASB emulator + OTEL collector
**Notes:** Full infrastructure from day 1 means docker-compose.yml is representative of the final architecture and won't need structural changes later.

---

## GitHub Actions CI

### Workflow scope

| Option | Description | Selected |
|--------|-------------|----------|
| Build + test all solutions | dotnet restore → dotnet build → dotnet test. All 3 steps. | ✓ |
| Build only, tests deferred to Phase 2 | CI just verifies compile. Test jobs added when real unit tests exist. | |
| Build + lint + test | Also adds dotnet format --verify-no-changes as a lint gate. | |

**User's choice:** Build + test all solutions
**Notes:** Even though Phase 1 stubs have no real tests, the test step is wired from day 1 so later phases' tests run automatically.

---

### Triggers

| Option | Description | Selected |
|--------|-------------|----------|
| Push to main + PR to main | Every PR validated before merge; every push to main re-validates. | ✓ |
| PR to main only | Direct pushes to main skip CI. | |
| Push to any branch + PR to main | Validates on every push regardless of branch. | |

**User's choice:** Push to main + PR to main
**Notes:** Standard gate for a solo portfolio project.

---

### Build strategy

| Option | Description | Selected |
|--------|-------------|----------|
| Matrix strategy per solution | One job with matrix over all 10 solutions (Contracts, AppHost, 8 services). Parallel builds, clear failure reporting. | ✓ |
| Single job, build all serially | Simpler YAML; slightly slower (sequential). | |
| Single dotnet sln at root | Root-level ecommerce.sln including all projects; one dotnet build command. | |

**User's choice:** Matrix strategy per solution
**Notes:** Matrix gives the best CI feedback — a break in one service is immediately obvious without waiting for serial steps.

---

## Claude's Discretion

- Specific PostgreSQL container count in AppHost (one shared container with multiple databases vs one container per service) — Claude to pick the approach that makes `aspire publish` output cleanest for Phase 1; can evolve to per-service in Phase 2.
- Exact OTLP collector configuration (Aspire includes a built-in OTLP endpoint; separate OpenTelemetry Collector container may not be needed if Aspire handles it natively).

## Deferred Ideas

None — discussion stayed within phase scope.
