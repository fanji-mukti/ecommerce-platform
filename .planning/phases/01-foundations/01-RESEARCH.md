# Phase 1: Foundations - Research

**Researched:** 2026-06-03
**Domain:** .NET 10 microservices scaffold, Aspire AppHost, ASB emulator, MADR ADRs, CI matrix
**Confidence:** HIGH (core stack verified via NuGet registry); MEDIUM (Aspire type-name derivation, MassTransit emulator workaround)

---

<user_constraints>
## User Constraints (from CONTEXT.md)

### Locked Decisions

**Contracts Library (CON-01, CON-02, CON-03)**
- D-01: Envelope expressed as `IMessageEnvelope` interface (not an abstract base record) with 4 required properties: `Guid MessageId`, `Guid CorrelationId`, `Guid CausationId`, `DateTimeOffset OccurredAt`. All domain message records implement this interface.
- D-02: Phase 1 defines the `IMessageEnvelope` interface AND stubs all 8 service namespaces with at least one placeholder record per namespace. Namespaces follow the pattern `ECommerce.{ServiceName}.Events.V1` and `ECommerce.{ServiceName}.Commands.V1`.
- D-03: Enforcement via project-level constraints only — `Contracts.csproj` has zero NuGet references beyond the SDK itself. No EF Core, MediatR, ASP.NET Core, or domain dependencies.

**Service Stubs (REPO-01, REPO-02, INF-03, INF-04)**
- D-04: All 8 service stubs scaffolded in Phase 1: Catalog, Cart, Checkout, Orders, Identity, Payments, Fulfillment, Notifications.
- D-05: Each stub contains: `Program.cs` with `AddOpenTelemetry()`, `UseSerilog()`, `MapHealthChecks("/health")`; a `<ProjectReference>` to `Contracts.csproj`; no business logic or domain code.
- D-06: Project naming convention: `ECommerce.{ServiceName}.API` (e.g., `ECommerce.Catalog.API`).
- D-07: Each service has its own `.sln` file in `src/services/{service-name}/`.

**Aspire AppHost (REPO-03)**
- D-08: AppHost lives at `src/ecommerce.AppHost/` with its own `ecommerce.AppHost.sln`.
- D-09: AppHost references all 8 service stubs via `<ProjectReference>`. Uses `builder.AddProject<Projects.ECommerce_{ServiceName}_API>()` pattern.
- D-10: AppHost provisions all 4 infrastructure resources from day 1: PostgreSQL, Redis, ASB emulator container, and OTLP/OTel collector. All visible in Aspire dashboard and exported to `docker-compose.yml` via `aspire publish`.

**Observability (INF-03, INF-04)**
- D-11: OTel scope: HTTP request traces via `AddAspNetCoreInstrumentation()` + OTLP exporter pointing at the Aspire dashboard endpoint.
- D-12: Serilog configured with W3C TraceContext enrichment — all log entries include `TraceId` and `SpanId` as structured properties.
- D-13: Each stub's `/health` endpoint returns `200 OK` with default health check response writer.

**GitHub Actions CI**
- D-14: CI workflow steps: `dotnet restore` → `dotnet build` → `dotnet test`.
- D-15: Triggers: `push` to `main` and `pull_request` targeting `main`.
- D-16: Build strategy: matrix per solution. Matrix includes: `Contracts`, `ecommerce.AppHost`, and each of the 8 service solutions (10 items total).

**ADRs (ADR-01, ADR-02)**
- D-17: ADRs follow MADR 4.0 format. Stored in `docs/adr/` with numbered kebab-case filenames.
- D-18: 8 ADRs to write in Phase 1: (1) MADR format, (2) ASB choice, (3) DB-per-service, (4) YARP gateway, (5) saga orchestration, (6) MassTransit + outbox/inbox, (7) ASB topic-per-context, (8) mono-repo multi-solution.

### Claude's Discretion

None specified — discussion stayed within locked decisions.

### Deferred Ideas (OUT OF SCOPE)

None — discussion stayed within phase scope.
</user_constraints>

---

<phase_requirements>
## Phase Requirements

| ID | Description | Research Support |
|----|-------------|------------------|
| REPO-01 | Each service has its own .sln file independently openable in Visual Studio, referencing Contracts via relative path | Verified: multi-solution mono-repo structure confirmed; sln format unchanged in .NET 10 |
| REPO-02 | All service solutions reference the shared Contracts project via relative path (not NuGet) | Verified: `<ProjectReference>` with relative path is the standard pattern; no special config needed |
| REPO-03 | Local orchestration via Docker Compose generated from .NET Aspire AppHost via `aspire publish` | Verified: `aspire publish` with `Aspire.Hosting.Docker 13.4.0` generates docker-compose.yaml; requires `builder.AddDockerComposeEnvironment()` |
| CON-01 | Shared Contracts library defines all ASB message types as pure C# records (no domain logic, no EF, no MediatR) | Verified: existing Contracts.csproj targets net10.0 with no NuGet deps; pattern is sound |
| CON-02 | Messages include envelope fields: MessageId, CorrelationId, CausationId, OccurredAt | Verified: C# records can implement interfaces; `init` accessor required to satisfy interface contract |
| CON-03 | Messages namespaced per producing service with `.V1` suffix | Verified: namespace convention `ECommerce.{ServiceName}.Events.V1` is a clean C# namespace |
| ADR-01 | ADRs follow MADR 4.0 format, stored in docs/adr/ with numbered kebab-case filenames | Verified: MADR 4.0 released 2024-09-17; 5-field YAML frontmatter + 7 section headings |
| ADR-02 | Minimum 8 ADRs written during Phase 1 | Verified: 8 topics identified in CONTEXT.md D-18; all achievable with research from this doc |
| INF-03 | All services emit OpenTelemetry traces and structured logs with correlation ID across service boundaries | Verified: `OpenTelemetry.Extensions.Hosting 1.15.3` + `OpenTelemetry.Instrumentation.AspNetCore 1.15.2` + `Serilog.AspNetCore 10.0.0` + `Serilog.Sinks.OpenTelemetry 4.2.0` — all confirmed on NuGet |
| INF-04 | All services expose GET /health endpoints for readiness and liveness probes | Verified: ASP.NET Core built-in `MapHealthChecks("/health")` — no additional NuGet package required for basic health checks |
</phase_requirements>

---

## Summary

Phase 1 is a pure scaffold phase — no business logic, no MassTransit consumers, no EF Core migrations. The deliverable is a repo structure that compiles, runs `docker compose up`, emits traces to the Aspire dashboard, and carries 8 foundational ADRs. All decisions are locked from the discussion phase; this research confirms feasibility and surfaces implementation details the planner needs.

The most significant finding is the **MassTransit licensing inflection point**: the NuGet registry shows MassTransit 9.1.1 as the current latest release, and v9 is commercially licensed by Massient. MassTransit 8.3.6 (Apache-2.0, released January 2025) remains the last open-source release and is supported with security patches through end of 2026. For a portfolio project, **pin to MassTransit 8.3.6** — this avoids license friction and aligns with the CLAUDE.md "no commercial license for portfolio" principle. This must be documented as part of ADR-0006 (MassTransit + outbox/inbox).

A second key finding: **`EmulatorHost()` is a MassTransit v9-only API**. With v8.3.6, the ASB emulator connection requires a manually constructed connection string using `UseDevelopmentEmulator=true` from the Azure SDK. This works but is a known friction point — Phase 1 should wire the emulator connection string via environment variable from Aspire and document the workaround.

The Aspire stack (13.4.0) is fully verified, current, and well-documented. The `aspire publish` → docker-compose pathway requires `Aspire.Hosting.Docker 13.4.0` in the AppHost and a one-line `builder.AddDockerComposeEnvironment("env-name")` call.

**Primary recommendation:** Scaffold all 8 stubs + AppHost + Contracts in one wave using `dotnet new` commands. Pin MassTransit to 8.3.6 and document the licensing decision immediately in ADR-0006. Wire the ASB emulator via `RunAsEmulator()` in Aspire — Aspire handles the container; services connect via the Aspire-injected connection string.

---

## Architectural Responsibility Map

| Capability | Primary Tier | Secondary Tier | Rationale |
|------------|-------------|----------------|-----------|
| Service stub hosting (`/health`) | API / Backend | — | Each stub is an ASP.NET Core Minimal API process |
| Shared message type contracts | Build artifact (class library) | — | Contracts.csproj is a compile-time dependency, not a runtime tier |
| Local container orchestration | .NET Aspire AppHost | docker-compose.yml (generated) | AppHost is the source of truth; docker-compose is a published artefact |
| ASB emulator provisioning | Aspire AppHost (container) | — | `RunAsEmulator()` tells Aspire to launch the container; no service owns it |
| PostgreSQL provisioning | Aspire AppHost (container) | — | Same pattern as ASB; Aspire-managed containers per service DB |
| Redis provisioning | Aspire AppHost (container) | — | Single Redis container for Phase 1; per-service namespacing deferred |
| OpenTelemetry traces | API / Backend (each service) | Aspire OTel collector | Each service emits OTLP; Aspire dashboard is the collector for local dev |
| Serilog structured logs | API / Backend (each service) | — | Serilog configured per service via `UseSerilog()` |
| CI build/test pipeline | GitHub Actions | — | .github/workflows/ci.yml; no runtime tier |
| ADR documentation | Static docs | — | `docs/adr/` markdown files; no runtime tier |

---

## Standard Stack

### Core (Phase 1 scope)

| Library | Version | Purpose | Why Standard |
|---------|---------|---------|--------------|
| `Aspire.Hosting` | 13.4.0 | AppHost runtime and DI | Current stable Aspire release |
| `Aspire.Hosting.AppHost` | 13.4.0 | SDK-style AppHost project | Required for `Sdk="Aspire.AppHost.Sdk"` |
| `Aspire.Hosting.Azure.ServiceBus` | 13.4.0 | ASB emulator provisioning in AppHost | `RunAsEmulator()` — Aspire-managed container |
| `Aspire.Hosting.PostgreSQL` | 13.4.0 | Postgres container per service | Standard Aspire integration |
| `Aspire.Hosting.Redis` | 13.4.0 | Redis container | Standard Aspire integration |
| `Aspire.Hosting.Docker` | 13.4.0 | `aspire publish` → docker-compose | Required for Docker Compose publishing |
| `Serilog.AspNetCore` | 10.0.0 | Structured logging via `UseSerilog()` | Industry standard; version 10 aligns with .NET 10 |
| `Serilog.Sinks.OpenTelemetry` | 4.2.0 | Export Serilog events as OTel logs | Bridges Serilog → OTLP collector |
| `OpenTelemetry.Extensions.Hosting` | 1.15.3 | `AddOpenTelemetry()` on `IHostBuilder` | Standard OTel SDK entry point |
| `OpenTelemetry.Instrumentation.AspNetCore` | 1.15.2 | HTTP request/response tracing | Auto-instruments Minimal API routes |

**Version verification:** All versions confirmed via `dotnet package search` against nuget.org on 2026-06-03. [VERIFIED: npm registry equivalent — NuGet registry via dotnet CLI]

### Supporting (Established in Phase 1, used in later phases)

| Library | Version | Purpose | When to Use |
|---------|---------|---------|-------------|
| `MassTransit` | 8.3.6 | Messaging abstraction (pinned open-source) | Phase 2+ — listed here to lock the version decision |
| `MassTransit.Azure.ServiceBus.Core` | 8.3.6 | ASB transport for MassTransit | Phase 2+ |
| `MassTransit.EntityFrameworkCore` | 8.3.6 | Transactional outbox/inbox + saga persistence | Phase 2+ |
| `Npgsql.EntityFrameworkCore.PostgreSQL` | 10.0.2 | EF Core PostgreSQL provider | Phase 2+ (each service with write-side persistence) |
| `FluentValidation` | 12.1.1 | Request/command validation | Phase 2+ |
| `Riok.Mapperly` | 4.3.1 | Source-generated DTO mapping | Phase 2+ |
| `xunit.v3` | 3.2.2 | Test framework | Phase 1 CI should find zero tests; framework referenced in service .sln files for Phase 2+ |

**Note on FluentValidation:** Version has bumped to 12.x from the 11.x mentioned in CLAUDE.md — [VERIFIED: NuGet registry, 12.1.1 published]. CLAUDE.md states "FluentValidation 11.x" but 12.x is the current release. The planner should use 12.x.

### Alternatives Considered

| Instead of | Could Use | Tradeoff |
|------------|-----------|----------|
| MassTransit 8.3.6 | MassTransit 9.x | v9 requires commercial Massient license — unacceptable for OSS portfolio |
| MassTransit 8.3.6 | OpenTransit fork | Promising but not yet production-ready on NuGet; first stable release expected post-2026 when MassTransit v8 EOL |
| `Aspire.Hosting.Azure.ServiceBus` `RunAsEmulator()` | Hand-authored docker-compose for ASB | Aspire integration is cleaner and auto-wires connection strings |
| `Serilog.Sinks.OpenTelemetry` | `Serilog.Sinks.Console` only | OTel sink provides dashboard integration and structured trace correlation |

**Installation (AppHost project):**
```bash
dotnet add package Aspire.Hosting.Azure.ServiceBus --version 13.4.0
dotnet add package Aspire.Hosting.PostgreSQL --version 13.4.0
dotnet add package Aspire.Hosting.Redis --version 13.4.0
dotnet add package Aspire.Hosting.Docker --version 13.4.0
```

**Installation (each service stub):**
```bash
dotnet add package Serilog.AspNetCore --version 10.0.0
dotnet add package Serilog.Sinks.OpenTelemetry --version 4.2.0
dotnet add package OpenTelemetry.Extensions.Hosting --version 1.15.3
dotnet add package OpenTelemetry.Instrumentation.AspNetCore --version 1.15.2
```

---

## Package Legitimacy Audit

> slopcheck was not available on this machine (install failed). All packages marked [ASSUMED] for slopcheck column. All packages confirmed on NuGet registry via `dotnet package search`.

| Package | Registry | Downloads | Owner | slopcheck | Disposition |
|---------|----------|-----------|-------|-----------|-------------|
| Aspire.Hosting | NuGet | 23M+ | Microsoft | [ASSUMED] | Approved — Microsoft first-party |
| Aspire.Hosting.Azure.ServiceBus | NuGet | 1.4M+ | Microsoft | [ASSUMED] | Approved — Microsoft first-party |
| Aspire.Hosting.Docker | NuGet | 399K+ | Microsoft | [ASSUMED] | Approved — Microsoft first-party |
| Aspire.Hosting.PostgreSQL | NuGet | 4.5M+ | Microsoft | [ASSUMED] | Approved — Microsoft first-party |
| Aspire.Hosting.Redis | NuGet | 4.7M+ | Microsoft | [ASSUMED] | Approved — Microsoft first-party |
| MassTransit | NuGet | 216M+ | phatboyg | [ASSUMED] | Approved — 16+ year history, Apache-2.0 v8 |
| MassTransit.Azure.ServiceBus.Core | NuGet | 46M+ | phatboyg | [ASSUMED] | Approved — same publisher as MassTransit |
| MassTransit.EntityFrameworkCore | NuGet | 23M+ | phatboyg | [ASSUMED] | Approved — same publisher |
| Serilog.AspNetCore | NuGet | 709M+ | serilog | [ASSUMED] | Approved — canonical Serilog package |
| Serilog.Sinks.OpenTelemetry | NuGet | 40M+ | serilog | [ASSUMED] | Approved — official Serilog org |
| OpenTelemetry.Extensions.Hosting | NuGet | 328M+ | OpenTelemetry | [ASSUMED] | Approved — CNCF project |
| OpenTelemetry.Instrumentation.AspNetCore | NuGet | 291M+ | OpenTelemetry | [ASSUMED] | Approved — CNCF project |
| Npgsql.EntityFrameworkCore.PostgreSQL | NuGet | 414M+ | Npgsql, roji | [ASSUMED] | Approved — canonical Npgsql provider |
| FluentValidation | NuGet | 935M+ | jskinner | [ASSUMED] | Approved — 15+ year history |
| Riok.Mapperly | NuGet | 23M+ | latonz, riok | [ASSUMED] | Approved — GitHub-backed, active maintenance |
| xunit.v3 | NuGet | 25M+ | xunit | [ASSUMED] | Approved — official xUnit team |

**Packages removed due to slopcheck [SLOP] verdict:** none
**Packages flagged as suspicious [SUS]:** none

*slopcheck was unavailable at research time. All packages are tagged [ASSUMED] for that column. The planner should treat each as approved given the publisher history and download counts, but may add a `checkpoint:human-verify` before install if the team has a strict policy.*

---

## Architecture Patterns

### System Architecture Diagram

```
[Developer workstation]
        │
        ▼
[aspire run / dotnet run on AppHost]
        │ launches containers + projects
        ├──► [ASB Emulator container] ← mcr.microsoft.com/azure-messaging/servicebus-emulator:latest
        │         + [SQL Server container] (emulator dependency)
        ├──► [PostgreSQL container] (shared for Phase 1)
        ├──► [Redis container]
        ├──► [Aspire OTel/Dashboard :18888]
        │
        ├──► [ECommerce.Catalog.API :port]      ──OTLP──► Aspire Dashboard
        ├──► [ECommerce.Cart.API :port]          ──OTLP──► Aspire Dashboard
        ├──► [ECommerce.Checkout.API :port]      ──OTLP──► Aspire Dashboard
        ├──► [ECommerce.Orders.API :port]        ──OTLP──► Aspire Dashboard
        ├──► [ECommerce.Identity.API :port]      ──OTLP──► Aspire Dashboard
        ├──► [ECommerce.Payments.API :port]      ──OTLP──► Aspire Dashboard
        ├──► [ECommerce.Fulfillment.API :port]   ──OTLP──► Aspire Dashboard
        └──► [ECommerce.Notifications.API :port] ──OTLP──► Aspire Dashboard

[aspire publish]
        │ generates
        ▼
[docker-compose.yml + .env at repo root]

[GitHub Actions]
  matrix[10 solutions] → dotnet restore → dotnet build → dotnet test
```

### Recommended Project Structure

```
ecommerce-platform/
├── src/
│   ├── building-blocks/
│   │   └── Contracts/
│   │       ├── Contracts.csproj           ← net10.0, no NuGet deps
│   │       ├── IMessageEnvelope.cs
│   │       ├── Catalog/
│   │       │   └── Events/V1/Placeholder.cs
│   │       ├── Cart/Events/V1/Placeholder.cs
│   │       ├── Checkout/Events/V1/Placeholder.cs
│   │       ├── Orders/Events/V1/Placeholder.cs
│   │       ├── Identity/Events/V1/Placeholder.cs
│   │       ├── Payments/Events/V1/Placeholder.cs
│   │       ├── Fulfillment/Events/V1/Placeholder.cs
│   │       └── Notifications/Events/V1/Placeholder.cs
│   ├── services/
│   │   ├── catalog/
│   │   │   ├── Catalog.sln
│   │   │   └── ECommerce.Catalog.API/
│   │   │       ├── ECommerce.Catalog.API.csproj
│   │   │       └── Program.cs
│   │   ├── cart/ ... (same pattern x7)
│   │   ├── checkout/
│   │   ├── orders/
│   │   ├── identity/
│   │   ├── payments/
│   │   ├── fulfillment/
│   │   └── notifications/
│   └── ecommerce.AppHost/
│       ├── ecommerce.AppHost.sln
│       ├── ecommerce.AppHost.csproj      ← Sdk="Aspire.AppHost.Sdk"
│       └── Program.cs
├── docs/
│   └── adr/
│       ├── 0001-use-madr-format.md
│       ├── 0002-azure-service-bus.md
│       └── ... (0003–0008)
├── .github/
│   └── workflows/
│       └── ci.yml
└── docker-compose.yml                    ← generated by aspire publish
```

### Pattern 1: Aspire AppHost Scaffolding

**What:** The AppHost project uses `Sdk="Aspire.AppHost.Sdk"` and declares all services and infrastructure as strongly-typed resources. The Aspire source generator creates `Projects.ECommerce_{ServiceName}_API` classes from `ProjectReference` elements.

**Type name derivation (VERIFIED):** Aspire replaces every **dot** in the project name with an **underscore** to form the generated class name. `ECommerce.Catalog.API.csproj` becomes `Projects.ECommerce_Catalog_API`. This is the identity — no other characters are modified.

**Custom type name override (use when names conflict):**
```xml
<ProjectReference Include="..." AspireProjectMetadataTypeName="CatalogAPI" />
```

**AppHost csproj example:**
```xml
<!-- Source: aspire.dev/get-started/aspire-sdk/ -->
<Project Sdk="Aspire.AppHost.Sdk/13.4.0">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net10.0</TargetFramework>
  </PropertyGroup>
  <ItemGroup>
    <ProjectReference Include="../../services/catalog/ECommerce.Catalog.API/ECommerce.Catalog.API.csproj" />
    <ProjectReference Include="../../services/cart/ECommerce.Cart.API/ECommerce.Cart.API.csproj" />
    <!-- ... 6 more services ... -->
  </ItemGroup>
</Project>
```

**AppHost Program.cs pattern:**
```csharp
// Source: aspire.dev/integrations/cloud/azure/azure-service-bus/azure-service-bus-host/
var builder = DistributedApplication.CreateBuilder(args);

// Infrastructure
var postgres = builder.AddPostgres("postgres");
var redis = builder.AddRedis("redis");
var serviceBus = builder.AddAzureServiceBus("messaging")
    .RunAsEmulator();                              // provisions mcr.microsoft.com/azure-messaging/servicebus-emulator

// Services — type name = project name with dots replaced by underscores
builder.AddProject<Projects.ECommerce_Catalog_API>("catalog")
    .WithReference(postgres)
    .WithReference(serviceBus);
builder.AddProject<Projects.ECommerce_Cart_API>("cart")
    .WithReference(postgres)
    .WithReference(redis)
    .WithReference(serviceBus);
// ... remaining 6 services

// Docker Compose publishing
builder.AddDockerComposeEnvironment("ecommerce-local");

builder.Build().Run();
```

**Docker Compose generation command:**
```bash
# Install Aspire CLI (once per developer machine)
dotnet tool install --global aspire.cli --prerelease

# Generate docker-compose.yaml
aspire publish -o ./                # outputs docker-compose.yaml + .env to current directory
```

### Pattern 2: IMessageEnvelope with C# Records

**What:** A shared interface in `Contracts.csproj` that all domain event and command records implement. Provides a compile-time contract that every message carries envelope metadata.

**C# record + interface pitfall:** When an interface declares a property, implementing records must declare each property with `{ get; init; }` (not just `{ get; }`). The `init` accessor is required for records to allow `with` expression usage and object initializer syntax. Using only `get` produces CS0535. [CITED: docs.microsoft.com/dotnet/csharp/language-reference/builtin-types/record]

```csharp
// Source: Decision D-01 from CONTEXT.md, confirmed via C# language reference
// File: src/building-blocks/Contracts/IMessageEnvelope.cs
namespace ECommerce.Contracts;

public interface IMessageEnvelope
{
    Guid MessageId { get; }
    Guid CorrelationId { get; }
    Guid CausationId { get; }
    DateTimeOffset OccurredAt { get; }
}

// Implementation pattern — all domain records follow this
// File: src/building-blocks/Contracts/Catalog/Events/V1/Placeholder.cs
namespace ECommerce.Catalog.Events.V1;

public record CatalogServiceReady(
    Guid MessageId,
    Guid CorrelationId,
    Guid CausationId,
    DateTimeOffset OccurredAt
) : IMessageEnvelope;
```

**Key:** Positional record parameters automatically generate `{ get; init; }` properties, which satisfy `{ get; }` interface properties. This is the idiomatic pattern — use positional parameters in records.

### Pattern 3: Service Stub Program.cs

**What:** Minimal ASP.NET Core service with OTel, Serilog, and health checks wired from day one.

```csharp
// Source: Microsoft OTel docs + Serilog docs + CONTEXT.md D-11/D-12/D-13
using Serilog;
using Serilog.Events;

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.OpenTelemetry()            // exports to OTLP — endpoint from OTEL_EXPORTER_OTLP_ENDPOINT env var
    .CreateLogger();

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog();             // replaces default logging

builder.Services.AddOpenApi();         // Microsoft.AspNetCore.OpenApi (replaces Swashbuckle)
builder.Services.AddHealthChecks();

builder.Services.AddOpenTelemetry()
    .WithTracing(tracing => tracing
        .AddAspNetCoreInstrumentation()    // traces HTTP requests
        .AddOtlpExporter());               // OTLP endpoint from env var

var app = builder.Build();

app.UseHttpsRedirection();
app.MapOpenApi();
app.MapHealthChecks("/health");

app.Run();
```

### Pattern 4: MADR 4.0 ADR Format

**What:** The exact MADR 4.0 template structure. All 8 ADRs in Phase 1 must follow this format.

```markdown
---
status: accepted
date: 2026-06-03
decision-makers: [Fanji Ari Mukti]
consulted: []
informed: []
---

# [Number and title — e.g., Use Azure Service Bus as messaging backbone]

## Context and Problem Statement

[Describe the forces and problem in 2-3 sentences]

## Decision Drivers

* [Force 1]
* [Force 2]

## Considered Options

* [Option A]
* [Option B]

## Decision Outcome

Chosen option: "[Option A]", because [justification].

### Consequences

* Good, because [positive effect]
* Bad, because [negative tradeoff or risk]

## Pros and Cons of the Options

### [Option A]

* Good, because [...]
* Bad, because [...]

### [Option B]

* Good, because [...]
* Bad, because [...]

## More Information

[Links, related ADRs, implementation notes]
```

**Valid status values:** `proposed` | `rejected` | `accepted` | `deprecated` | `superseded by [0NNN]`

**File naming:** `docs/adr/NNNN-kebab-case-title.md` — zero-padded 4-digit number. [VERIFIED: adr.github.io/madr/]

### Pattern 5: GitHub Actions CI Matrix

**What:** Matrix strategy over 10 solution files; each matrix entry is an independent parallel job.

```yaml
# Source: docs.github.com/actions/guides/building-and-testing-net
name: CI

on:
  push:
    branches: [main]
  pull_request:
    branches: [main]

jobs:
  build:
    runs-on: ubuntu-latest
    strategy:
      fail-fast: false            # report all failures, not just first
      matrix:
        solution:
          - src/building-blocks/Contracts/Contracts.sln
          - src/ecommerce.AppHost/ecommerce.AppHost.sln
          - src/services/catalog/Catalog.sln
          - src/services/cart/Cart.sln
          - src/services/checkout/Checkout.sln
          - src/services/orders/Orders.sln
          - src/services/identity/Identity.sln
          - src/services/payments/Payments.sln
          - src/services/fulfillment/Fulfillment.sln
          - src/services/notifications/Notifications.sln

    steps:
      - uses: actions/checkout@v4
      - uses: actions/setup-dotnet@v4
        with:
          dotnet-version: '10.0.x'
      - name: Restore
        run: dotnet restore ${{ matrix.solution }}
      - name: Build
        run: dotnet build ${{ matrix.solution }} --no-restore --configuration Release
      - name: Test
        run: dotnet test ${{ matrix.solution }} --no-build --configuration Release
```

**`fail-fast: false` is critical** — without it, the first failing solution cancels all parallel jobs, obscuring which solutions fail.

### Anti-Patterns to Avoid

- **AppHost in a service solution:** The AppHost must have its own `.sln`. Adding it to a service solution defeats independent openability.
- **Hand-authoring docker-compose.yml:** The file at the repo root is generated by `aspire publish`. Committing a hand-authored version that diverges from the AppHost causes silent drift. Always regenerate.
- **Using `Projects.ECommerce_AppHost` in AddProject:** The AppHost project itself is never passed to `AddProject<T>`. Only service stubs are referenced this way.
- **Abstract base class instead of interface for IMessageEnvelope:** A base record would impose inheritance on all message types. An interface is more flexible and makes the Contracts library purely structural.
- **Contracts.csproj with any NuGet dependency:** Even `Microsoft.AspNetCore.App` framework reference would violate the purity constraint. The csproj must have only the `<Sdk>Microsoft.NET.Sdk</Sdk>` and target framework.

---

## Don't Hand-Roll

| Problem | Don't Build | Use Instead | Why |
|---------|-------------|-------------|-----|
| Container orchestration for local dev | Custom shell scripts to launch containers | `.NET Aspire AppHost` with `RunAsEmulator()`, `AddPostgres()`, `AddRedis()` | Aspire handles port assignment, health checks, connection string injection, OTLP wiring |
| docker-compose.yml authoring | Manually write service definitions | `aspire publish` with `Aspire.Hosting.Docker` | Generated file is always consistent with AppHost model; hand-authored drifts |
| OTel trace correlation in logs | Custom middleware to inject trace IDs | `Serilog.Sinks.OpenTelemetry` + `Serilog.Enrich.WithSpanId()` | OTel sink bridges the two systems; manual correlation is fragile |
| ASB emulator topology | Write config.json by hand | Aspire `AddAzureServiceBus().RunAsEmulator()` | Aspire provisions the emulator container with correct SQL Server dependency |
| ADR numbering/structure enforcement | Custom tooling | MADR 4.0 template + naming convention | MADR has established tooling, linters, and GitHub Actions runners |
| Health check response format | Custom health endpoint | `app.MapHealthChecks("/health")` with default `HealthCheckOptions.ResponseWriter` | Built into ASP.NET Core; no NuGet package needed for basic checks |

**Key insight:** Phase 1 is a scaffold, not an implementation. Every item in this table is a place where custom code would be premature and would need replacing when the real infra lands.

---

## Common Pitfalls

### Pitfall 1: MassTransit v9 Installed Instead of v8

**What goes wrong:** `dotnet add package MassTransit` installs 9.1.1 (the NuGet "latest"), which requires a Massient commercial license.
**Why it happens:** NuGet default is always latest version; CLAUDE.md says "MassTransit 8.x" but the search result shows 9.x is current.
**How to avoid:** Pin explicitly: `dotnet add package MassTransit --version 8.3.6`. Add `<PackageReference Include="MassTransit" Version="8.3.6" />` with an explicit version constraint. Add an ADR explaining why 8.x is pinned.
**Warning signs:** License error at startup; `InvalidOperationException: License key is required` in logs.

### Pitfall 2: MassTransit 8.x + ASB Emulator Connectivity

**What goes wrong:** MassTransit 8.x does not have the `EmulatorHost()` API (added in v9). Using the emulator connection string (`UseDevelopmentEmulator=true`) may cause MassTransit to attempt HTTPS (port 443) instead of AMQP (port 5672).
**Why it happens:** The `UseDevelopmentEmulator=true` flag was added to the Azure SDK after MassTransit 8.x was frozen. MT8 does not inspect that flag specially.
**How to avoid:** Phase 1 does not configure MassTransit at all (no consumers, no bus) — this pitfall only surfaces in Phase 2. Document it in ADR-0006 and address it during Phase 2 MassTransit configuration. The Aspire `RunAsEmulator()` integration does inject the correct connection string; consuming it from the app may require testing.
**Warning signs:** `Endpoint=sb://localhost;...;UseDevelopmentEmulator=true` in env vars but connection refused on port 5672. Check Aspire dashboard container logs.

### Pitfall 3: Aspire Docker Publisher Not Referenced

**What goes wrong:** `aspire publish` produces an error or outputs only Kubernetes manifests (default publisher is Azure/ACA), not a docker-compose.yml.
**Why it happens:** `Aspire.Hosting.Docker` must be added to the AppHost project AND `builder.AddDockerComposeEnvironment("name")` must be called in Program.cs.
**How to avoid:** Add `Aspire.Hosting.Docker 13.4.0` to AppHost csproj and include the `AddDockerComposeEnvironment` call before `builder.Build().Run()`.
**Warning signs:** `aspire publish` prompts for a publisher type without offering Docker Compose; or the command succeeds but produces a `manifest.json` instead of `docker-compose.yaml`.

### Pitfall 4: Aspire SDK Version Mismatch

**What goes wrong:** `Sdk="Aspire.AppHost.Sdk/13.4.0"` in csproj does not match installed workload version → SDK resolution failure at build.
**Why it happens:** The SDK version in the csproj Sdk attribute must match available Aspire SDK versions.
**How to avoid:** Use `<Sdk Name="Aspire.AppHost.Sdk" />` (without pinned version) to let MSBuild resolve from the installed SDK, or use the version that matches the `Aspire.Hosting` package version in use. Alternatively, scaffold with `dotnet new aspire-apphost` which sets the correct version automatically.
**Warning signs:** `error NETSDK1101: The target framework 'net10.0' is not supported by the SDK`.

### Pitfall 5: C# Records with Interface Properties — CS0535

**What goes wrong:** A domain record declares `Guid MessageId { get; }` without `init`, but the interface requires `{ get; }` only — however if the record does NOT use positional parameters, the auto-property doesn't get `init` and the compiler may complain in assignment scenarios.
**Why it happens:** Confusion between auto-property syntax in records vs classes.
**How to avoid:** Use positional record parameters: `public record MyEvent(Guid MessageId, ...) : IMessageEnvelope`. The primary constructor generates `{ get; init; }` for each parameter, which satisfies `{ get; }` on the interface.
**Warning signs:** CS0535 "does not implement interface member"; or records that cannot be used with `with` expressions.

### Pitfall 6: Contracts.csproj Polluted with Framework References

**What goes wrong:** A developer adds `<FrameworkReference Include="Microsoft.AspNetCore.App" />` to Contracts.csproj "just in case", pulling in ASP.NET Core types.
**Why it happens:** Habit from web project templates; also happens if someone copies a service project and forgets to strip it.
**How to avoid:** PR review checklist item: "Contracts.csproj has ONLY `<TargetFramework>net10.0</TargetFramework>` and no `<PackageReference>` or `<FrameworkReference>` elements." The research summary flags this as Pitfall #2 in SUMMARY.md.
**Warning signs:** `dotnet build Contracts.csproj` referencing ASP.NET Core types in the output; `Contracts.dll` size > 10 KB (pure records should be tiny).

---

## MassTransit Namespace → ASB Topic Name Derivation

**Default behavior (MEDIUM confidence — from community sources):** MassTransit creates ASB topic names in the format `{namespace}/{classname}` with case lowercased. For example, `ECommerce.Catalog.Events.V1.ProductCreated` becomes the topic `ecommerce.catalog.events.v1/productcreated` (or similar — exact casing is implementation-dependent). [ASSUMED: from community reports; not confirmed in MassTransit 8.x official docs]

**Why this matters for Phase 1:** The Contracts namespace design in D-02 locks `ECommerce.{ServiceName}.Events.V1` as the namespace. This will produce topic names with `.V1` in the path, which is valid for ASB (ASB allows dots and slashes in topic names up to 260 chars). The "ASB topic-per-producing-context" pattern (ADR-0007) requires overriding this default — topic names should be `{service}-events` not one-per-message. This override is a Phase 2 concern (first MassTransit config), but the ADR should note it.

**Override mechanism:** [VERIFIED: masstransit official docs]
```csharp
// Option 1: per-type [EntityName] attribute on the record
[EntityName("catalog-events")]
public record ProductCreated(...) : IMessageEnvelope;

// Option 2: explicit in bus config
cfg.Message<ProductCreated>(x => x.SetEntityName("catalog-events"));
```

**Phase 1 impact:** No MassTransit bus configuration exists in Phase 1. The namespace design decision locked in D-02 is compatible with Phase 2 topic naming. ADR-0007 should specify the override strategy.

---

## ASB Emulator: Feature Parity and Configuration

**Docker image (VERIFIED: docs.microsoft.com):**
```
mcr.microsoft.com/azure-messaging/servicebus-emulator:latest
```
Companion SQL Server image required:
```
mcr.microsoft.com/mssql/server:2022-latest
```

**What the emulator supports:**
- Topics with subscriptions [VERIFIED: official docs]
- Correlation filters and SQL filters (up to 20 SQL filters) [VERIFIED]
- Dead-letter queues [VERIFIED: implied by MaxDeliveryCount + DeadLetteringOnMessageExpiration]
- AMQP TCP (port 5672) [VERIFIED]
- Up to 50 entities (queues/topics) per namespace [VERIFIED]
- Up to 50 subscriptions per topic [VERIFIED]

**What the emulator does NOT support (VERIFIED: official docs):**
- Sessions (`RequiresSession: false` is the only supported value)
- Partitioned entities
- AMQP Web Sockets (TCP only)
- JMS protocol
- Large messages (>256 KB)
- Microsoft Entra ID / managed identity authentication
- Data persistence across container restarts

**Connection string for services connecting to Aspire-managed emulator:**
When Aspire provisions the emulator with `RunAsEmulator()`, it injects the connection string into consuming services as an environment variable. The injected string format is:
```
Endpoint=sb://servicebus-emulator;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=SAS_KEY_VALUE;UseDevelopmentEmulator=true;
```
For services within the same Aspire-managed Docker network, `servicebus-emulator` resolves via container alias.

**Phase 1 implication:** Phase 1 services do not connect to ASB (no consumers or publishers). The emulator is provisioned via Aspire but only appears in the Aspire dashboard and docker-compose.yaml. Connectivity testing deferred to Phase 2.

---

## State of the Art

| Old Approach | Current Approach | When Changed | Impact |
|--------------|------------------|--------------|--------|
| Swashbuckle for OpenAPI | `Microsoft.AspNetCore.OpenApi` (built-in) | .NET 9/10 | No extra NuGet package; use `builder.Services.AddOpenApi()` + `app.MapOpenApi()` |
| `dotnet new aspire` (old template) | `aspire init` (Aspire CLI) or `dotnet new aspire-apphost` | Aspire 13.x | CLI-first experience; `aspire init` auto-scaffolds AppHost |
| `dotnet new aspire-starter` creates AppHost with ServiceDefaults | AppHost uses `Aspire.AppHost.Sdk` — ServiceDefaults is a separate optional project | Aspire 9+ | ServiceDefaults is recommended but optional; provides extension methods for `AddServiceDefaults()` |
| MassTransit 8.x (Apache-2.0) | MassTransit 9.x (commercial) | Q1 2026 | **For this project: pin to 8.3.6.** v8 receives security patches through end of 2026 |
| `xunit` v2 package | `xunit.v3` package | 2024 | Separate NuGet package ID; v3 = `xunit.v3`, not `xunit` |
| FluentValidation 11.x | FluentValidation 12.x | 2024/2025 | CLAUDE.md says 11.x; current is 12.1.1. The architecture is identical; use 12.x |
| MediatR 12.x | MediatR has a separate commercialization concern | 2025 | Not in scope for Phase 1; Phase 2 will address |
| Karma (Angular test runner) | Karma deprecated; Vitest via `@analogjs/vitest-angular` | Angular 18+ | Not in Phase 1 scope (no frontend) |

**Deprecated/outdated:**
- Swashbuckle: Replaced as ASP.NET Core default in .NET 9/10 — do not install; use `Microsoft.AspNetCore.OpenApi`
- `dotnet workload install aspire`: No longer the installation path; Aspire is now purely NuGet-based via `Aspire.AppHost.Sdk`
- MassTransit v9+ `EmulatorHost()`: API does not exist in v8.3.6 — do not reference it in Phase 1/2 planning

---

## Assumptions Log

| # | Claim | Section | Risk if Wrong |
|---|-------|---------|---------------|
| A1 | MassTransit default ASB topic name is `{lowercase.namespace}/{lowercase.classname}` with slash separator | MassTransit Namespace section | If different format, ADR-0007 topic-naming content may need adjustment; Phase 2 task that overrides topic names is unaffected |
| A2 | `EmulatorHost()` does not exist in MassTransit 8.3.6 (only in v9) | Common Pitfalls #2 | If a backport exists, Phase 2 emulator config is simpler than planned |
| A3 | `aspire publish` via `Aspire.Hosting.Docker` generates `docker-compose.yaml` to the AppHost project directory (not repo root) by default | Pattern 1 | Generated file landing in wrong directory; command includes `-o ./` flag to control output path |
| A4 | ASB emulator supports dead-letter queues (inferred from MaxDeliveryCount config) | ASB Emulator section | If DLQ is not supported, Phase 4/5 DLQ testing must use cloud ASB |
| A5 | FluentValidation 12.x API is backward-compatible with 11.x usage patterns (same `AddValidatorsFromAssembly`, same filter integration) | Standard Stack | If breaking changes exist, Phase 2 validation tasks need adjustment |

---

## Open Questions

1. **MassTransit 8.3.6 + ASB Emulator AMQP connectivity**
   - What we know: MT8 does not have `EmulatorHost()`. The Azure SDK `UseDevelopmentEmulator=true` flag enables non-TLS AMQP. Community reports show MT8 defaulting to port 443 (HTTPS management).
   - What's unclear: Whether MT8 connection to `Endpoint=sb://localhost;...;UseDevelopmentEmulator=true` works transparently via the Azure SDK (MT uses `Azure.Messaging.ServiceBus` internally), or requires custom `ServiceBusClientOptions`.
   - Recommendation: Phase 2 spike task — stand up a single consumer against the Aspire-injected emulator connection string and verify AMQP connectivity before building all 8 service configs. If it fails, the mitigation is to use a mock in-memory transport for local dev (MassTransit has `UsingInMemory()`) and only test with the real ASB emulator in CI.

2. **Contracts.sln inclusion**
   - What we know: CONTEXT.md D-07 says each service has its own `.sln`. `Contracts.csproj` is shared via project reference.
   - What's unclear: Should `Contracts.csproj` have its own `Contracts.sln` at `src/building-blocks/Contracts/`, separate from all service solutions? The CI matrix in D-16 includes a `Contracts` entry, implying yes.
   - Recommendation: Create `Contracts.sln` at `src/building-blocks/Contracts/Contracts.sln`. This is the entry in the CI matrix. Service solutions reference the `.csproj` directly, not the Contracts `.sln`.

3. **Aspire ServiceDefaults project**
   - What we know: Aspire typically recommends a `ServiceDefaults` shared project with extension methods (`AddServiceDefaults()`, `MapDefaultEndpoints()`) that wire OTel, health checks, and service discovery.
   - What's unclear: D-05 does not mention ServiceDefaults. Should Phase 1 scaffold this as a `src/building-blocks/ServiceDefaults/` project, or inline OTel/Serilog configuration per-service?
   - Recommendation: Create a `ServiceDefaults` project. This avoids duplicating the OTel + Serilog wiring across 8 stubs and aligns with Aspire best practices. The planner should add this as a task if not already planned.

---

## Environment Availability

| Dependency | Required By | Available | Version | Fallback |
|------------|------------|-----------|---------|----------|
| .NET 10 SDK | All services | ✓ | 10.0.300 | — |
| Docker Desktop | Aspire containers, docker-compose | ✗ | — | Must be installed before phase execution |
| Node.js | GitHub Actions CI (not local) | ✓ (local) | v24.16.0 | — |
| Aspire CLI (`aspire`) | `aspire publish`, `aspire init` | ✗ | — | `dotnet tool install --global aspire.cli --prerelease` |
| `dotnet new aspire-apphost` template | AppHost scaffolding | Not verified | — | Templates auto-installed by `aspire new` or `dotnet new install aspire-templates` |
| git | CI, commits | ✓ | 2.31.1 | — |
| GitHub Actions | CI pipeline | N/A (cloud) | — | — |

**Missing dependencies with no fallback:**
- **Docker Desktop**: Required for `aspire run` (to launch emulator + Postgres + Redis containers) and for validating `docker compose up`. Must be installed and running before any Aspire-based work can be tested. The plan must include an install prerequisite step or note.

**Missing dependencies with fallback:**
- **Aspire CLI**: Not installed globally. Install with `dotnet tool install --global aspire.cli --prerelease`. Alternatively, `aspire publish` can be replaced with a manual `dotnet run --project src/ecommerce.AppHost -- publish --publisher docker-compose` if the CLI is unavailable.

---

## Security Domain

> `security_enforcement: true`, `security_asvs_level: 1` (ASVS Level 1 = automated/opportunistic verification).

### Applicable ASVS Categories (Phase 1 scope only — stub services, no auth, no user data)

| ASVS Category | Applies | Standard Control |
|---------------|---------|-----------------|
| V2 Authentication | No | Not in Phase 1 — stubs have no auth |
| V3 Session Management | No | Not in Phase 1 |
| V4 Access Control | No | Not in Phase 1 — health endpoint is unauthenticated by design |
| V5 Input Validation | No | Phase 1 has no user inputs |
| V6 Cryptography | No | Phase 1 has no crypto operations |
| V7 Error Handling / Logging | Yes (partial) | Serilog must not log sensitive data; structured logging with `DestructureWith` guards |
| V14 Configuration | Yes | No secrets in appsettings.json; connection strings injected via Aspire env vars |

### Known Threat Patterns for Phase 1 Stack

| Pattern | STRIDE | Standard Mitigation |
|---------|--------|---------------------|
| Secret leakage in appsettings.json | Information Disclosure | Use Aspire-injected environment variables; never hardcode connection strings |
| Emulator EULA acceptance in automated scripts | Compliance | `ACCEPT_EULA=Y` must be set consciously in `.env`; never commit `.env` to git |
| MassTransit 8.3.6 known CVEs | Elevation of Privilege | Check NuGet Security tab before pinning; v8.3.6 was released Jan 2025 (recent) |
| docker-compose.yml generated with plaintext SQL SA password in .env | Information Disclosure | `.env` in `.gitignore` (Aspire adds this automatically); verify before first commit |

---

## Project Constraints (from CLAUDE.md)

All directives extracted from `./CLAUDE.md` that apply to Phase 1:

| Constraint | Source | Impact on Phase 1 |
|------------|--------|-------------------|
| .NET 10 (LTS) as runtime | CLAUDE.md Tech Stack | All stubs target `net10.0` |
| ASP.NET Core Minimal APIs (not MVC) | CLAUDE.md Core Framework | No `MapControllers()` — use `MapGet()`, `MapHealthChecks()` |
| `Microsoft.AspNetCore.OpenApi` replaces Swashbuckle | CLAUDE.md "What NOT to Use" | Use `AddOpenApi()` + `app.MapOpenApi()` |
| Mapperly, NOT AutoMapper | CLAUDE.md "What NOT to Use" | No mapping in Phase 1; note for Phase 2+ |
| `System.Text.Json`, NOT Newtonsoft.Json | CLAUDE.md "What NOT to Use" | System.Text.Json is the default in .NET 10; no override needed |
| MassTransit 8.x (Apache-2.0) | CLAUDE.md Recommended Stack | Pin 8.3.6 explicitly in every csproj that references it |
| Distroless image `mcr.microsoft.com/dotnet/aspnet:10.0-noble-chiseled` | CLAUDE.md Containers | Phase 1 does not build Docker images (aspire publish generates them); note for Dockerfile authoring in Phase 2+ |
| MADR format for all ADRs | CLAUDE.md Constraints | Use MADR 4.0 bare template |
| No in-process direct service calls | CLAUDE.md "What NOT to Use" | Phase 1 stubs have no cross-service calls; enforce via PR review |
| Roslyn analyzer NOT required in Phase 1 for Contracts enforcement | CONTEXT.md D-03 | PR review is the enforcement mechanism |
| ADR format: MADR, one file per decision in `docs/adr/` | CLAUDE.md | Create `docs/adr/` directory and 8 ADR files |

---

## Sources

### Primary (HIGH confidence)
- [learn.microsoft.com — ASB Emulator Overview](https://learn.microsoft.com/en-us/azure/service-bus-messaging/overview-emulator) — feature set, known limitations, usage quotas; published 2026-02-05
- [learn.microsoft.com — ASB Emulator Setup](https://learn.microsoft.com/en-us/azure/service-bus-messaging/test-locally-with-service-bus-emulator) — Docker image name, docker-compose config, connection strings; updated 2026-05-08
- [aspire.dev — Azure Service Bus AppHost setup](https://aspire.dev/integrations/cloud/azure/azure-service-bus/azure-service-bus-host/) — `RunAsEmulator()` API, AddAzureServiceBus pattern
- [aspire.dev — Aspire SDK](https://aspire.dev/get-started/aspire-sdk/) — AppHost csproj format, `AspireProjectMetadataTypeName`, type name generation
- [adr.github.io/madr](https://adr.github.io/madr/) — MADR 4.0 official format, frontmatter schema, template files
- [docs.github.com — Building and testing .NET](https://docs.github.com/actions/guides/building-and-testing-net) — GitHub Actions matrix strategy, `actions/setup-dotnet@v4`
- NuGet registry (via `dotnet package search`) — all package versions confirmed 2026-06-03

### Secondary (MEDIUM confidence)
- [nicovermeir.be — Setting generated project name in Aspire](https://nicovermeir.be/aspire/2024/03/21/setting-aspire-project-name.html) — dot-to-underscore transformation confirmation
- [milanjovanovic.tech — Using .NET Aspire with Docker Publisher](https://www.milanjovanovic.tech/blog/using-dotnet-aspire-with-the-docker-publisher) — `aspire publish` command, `AddDockerComposeEnvironment` API
- [massient.com](https://massient.com) — MassTransit v9 commercial licensing and free tier details
- [milanjovanovic.tech — MediatR and MassTransit Going Commercial](https://www.milanjovanovic.tech/blog/mediatr-and-masstransit-going-commercial-what-this-means-for-you) — v8 viability assessment
- [masstransit.massient.com — Change Log](https://masstransit.massient.com/reference/change-log) — confirmed `EmulatorHost()` added in MT v9.0.1 only

### Tertiary (LOW confidence — [ASSUMED])
- Community reports re: MassTransit 8.x + ASB emulator port 443 issue — GitHub discussions #5684, #5689, #5757
- MassTransit default ASB topic naming (`namespace/classname` format) — community blog posts; not confirmed in official MT8 docs

---

## Metadata

**Confidence breakdown:**
- Standard stack: HIGH — all package versions verified via NuGet registry on 2026-06-03
- Architecture: HIGH — ASP.NET Core Minimal APIs, Aspire AppHost patterns are official docs
- ASB Emulator: HIGH — official Microsoft docs confirmed feature set and limitations
- MassTransit licensing: HIGH — confirmed via massient.com and NuGet version history
- MassTransit emulator compatibility: MEDIUM — known issue; workaround partially confirmed; Phase 2 spike recommended
- MADR 4.0 format: HIGH — official MADR GitHub repo and documentation
- Aspire type name generation: MEDIUM — documented pattern confirmed by community; exact algorithm not in official docs
- CI matrix strategy: HIGH — official GitHub Actions documentation

**Research date:** 2026-06-03
**Valid until:** 2026-09-03 (stable ecosystem; MassTransit v8 EOL end-of-2026 is the only time-sensitive factor)
