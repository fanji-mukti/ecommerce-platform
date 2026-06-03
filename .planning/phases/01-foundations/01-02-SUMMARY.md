---
phase: 01-foundations
plan: 02
subsystem: service-stubs
tags: [dotnet, aspnetcore, otel, serilog, microservices, scaffolding]
dependency_graph:
  requires:
    - 01-01 (Contracts library — required for ProjectReference in each service .csproj)
  provides:
    - 8 compilable service stub projects (catalog, cart, checkout, orders, identity, payments, fulfillment, notifications)
    - 8 standalone .sln files (independently openable in Visual Studio)
    - OTel + Serilog wiring on every service from day one
    - /health endpoint on every service
  affects:
    - 01-03 (Aspire AppHost references all 8 service projects via ProjectReference)
    - 01-04 (CI matrix references all 8 .sln files)
tech_stack:
  added:
    - Microsoft.AspNetCore.OpenApi 10.0.8 (AddOpenApi/MapOpenApi — explicit NuGet required by .NET 10)
    - Serilog.AspNetCore 10.0.0 (host integration + bootstrap logger)
    - Serilog.Sinks.OpenTelemetry 4.2.0 (structured log export via OTLP)
    - OpenTelemetry.Extensions.Hosting 1.15.3 (AddOpenTelemetry host integration)
    - OpenTelemetry.Instrumentation.AspNetCore 1.15.2 (AddAspNetCoreInstrumentation)
    - OpenTelemetry.Exporter.OpenTelemetryProtocol 1.15.3 (AddOtlpExporter — separate exporter package)
  patterns:
    - Serilog bootstrap logger (pre-host startup exception capture via try/catch/finally)
    - OTel WithTracing(AddAspNetCoreInstrumentation + AddOtlpExporter) on every service
    - W3C TraceContext correlation via Enrich.FromLogContext() + WriteTo.OpenTelemetry()
    - OTLP endpoint injected by Aspire via OTEL_EXPORTER_OTLP_ENDPOINT (not hardcoded)
    - /health via built-in AddHealthChecks() + MapHealthChecks() (no extra NuGet)
    - Multi-solution mono-repo: each service has own .sln (VS independent open) + Contracts via ProjectReference
key_files:
  created:
    - src/services/catalog/ECommerce.Catalog.API/ECommerce.Catalog.API.csproj
    - src/services/catalog/ECommerce.Catalog.API/Program.cs
    - src/services/catalog/Catalog.sln
    - src/services/cart/ECommerce.Cart.API/ECommerce.Cart.API.csproj
    - src/services/cart/ECommerce.Cart.API/Program.cs
    - src/services/cart/Cart.sln
    - src/services/checkout/ECommerce.Checkout.API/ECommerce.Checkout.API.csproj
    - src/services/checkout/ECommerce.Checkout.API/Program.cs
    - src/services/checkout/Checkout.sln
    - src/services/orders/ECommerce.Orders.API/ECommerce.Orders.API.csproj
    - src/services/orders/ECommerce.Orders.API/Program.cs
    - src/services/orders/Orders.sln
    - src/services/identity/ECommerce.Identity.API/ECommerce.Identity.API.csproj
    - src/services/identity/ECommerce.Identity.API/Program.cs
    - src/services/identity/Identity.sln
    - src/services/payments/ECommerce.Payments.API/ECommerce.Payments.API.csproj
    - src/services/payments/ECommerce.Payments.API/Program.cs
    - src/services/payments/Payments.sln
    - src/services/fulfillment/ECommerce.Fulfillment.API/ECommerce.Fulfillment.API.csproj
    - src/services/fulfillment/ECommerce.Fulfillment.API/Program.cs
    - src/services/fulfillment/Fulfillment.sln
    - src/services/notifications/ECommerce.Notifications.API/ECommerce.Notifications.API.csproj
    - src/services/notifications/ECommerce.Notifications.API/Program.cs
    - src/services/notifications/Notifications.sln
  modified: []
decisions:
  - "Microsoft.AspNetCore.OpenApi added as explicit NuGet (10.0.8): despite PATTERNS.md claiming AddOpenApi is built-in to .NET 10 Sdk.Web, the method requires the NuGet package — confirmed by build failure CS1061 then successful build after adding package"
  - "OpenTelemetry.Exporter.OpenTelemetryProtocol added (1.15.3): AddOtlpExporter not included in OpenTelemetry.Extensions.Hosting — requires separate exporter package"
  - "using OpenTelemetry.Trace added to all Program.cs: ImplicitUsings=enable does not include OTel namespaces; extension methods (AddAspNetCoreInstrumentation, AddOtlpExporter) unreachable without explicit using"
  - "dotnet new sln --format sln used: .NET SDK 10.0.300 defaults to .slnx format; --format sln flag required to produce classic .sln format for VS compatibility"
metrics:
  duration_minutes: 11
  completed_date: "2026-06-03"
  tasks_completed: 3
  tasks_total: 3
  files_created: 24
  files_modified: 8
---

# Phase 1 Plan 2: Service Stubs Summary

**One-liner:** 8 ASP.NET Core service stubs with OTel tracing (OTLP), Serilog structured logs (W3C correlation), GET /health, and standalone .sln files each referencing Contracts via relative ProjectReference.

## What Was Built

Eight independent microservice stubs under `src/services/` — Catalog, Cart, Checkout, Orders, Identity, Payments, Fulfillment, and Notifications. Each service has:

- A `.csproj` targeting `net10.0` with 6 pinned NuGet packages and a `ProjectReference` to `src/building-blocks/Contracts/Contracts.csproj`
- A `Program.cs` with a Serilog bootstrap logger (pre-host startup exception capture), OTel tracing wired from startup, and `GET /health`
- A `.sln` file containing the service project and Contracts — independently openable in Visual Studio without loading the entire mono-repo

## Deviations from Plan

### Auto-fixed Issues

**1. [Rule 1 - Bug] Missing Microsoft.AspNetCore.OpenApi NuGet package**
- **Found during:** Task 2 verification (CS1061: 'IServiceCollection' does not contain 'AddOpenApi')
- **Issue:** PATTERNS.md stated "`AddOpenApi()` is the .NET 10 built-in OpenAPI method (no extra NuGet package needed)" — this is incorrect. In .NET 10 SDK with `Microsoft.NET.Sdk.Web`, `AddOpenApi()` and `MapOpenApi()` require the `Microsoft.AspNetCore.OpenApi` NuGet package to be explicitly referenced.
- **Fix:** Added `Microsoft.AspNetCore.OpenApi Version="10.0.8"` to all 8 .csproj files. Package is from the `Microsoft` publisher, cached locally at 10.0.8, same version as the runtime.
- **Files modified:** All 8 service .csproj files
- **Commit:** 6821ad9

**2. [Rule 1 - Bug] Missing OpenTelemetry.Exporter.OpenTelemetryProtocol package**
- **Found during:** Task 2 verification (CS1061: 'TracerProviderBuilder' does not contain 'AddOtlpExporter')
- **Issue:** `AddOtlpExporter()` is not included in `OpenTelemetry.Extensions.Hosting`. It requires the separate `OpenTelemetry.Exporter.OpenTelemetryProtocol` package. PATTERNS.md listed only 4 packages; this is the missing 5th.
- **Fix:** Added `OpenTelemetry.Exporter.OpenTelemetryProtocol Version="1.15.3"` to all 8 .csproj files. Package is from the OpenTelemetry Foundation (official .NET OTel project).
- **Files modified:** All 8 service .csproj files
- **Commit:** 6821ad9

**3. [Rule 1 - Bug] Missing `using OpenTelemetry.Trace` directive**
- **Found during:** Task 2 verification (CS1061: 'TracerProviderBuilder' does not contain 'AddAspNetCoreInstrumentation')
- **Issue:** `ImplicitUsings=enable` does not import OpenTelemetry namespaces. Extension methods `AddAspNetCoreInstrumentation()` and `AddOtlpExporter()` are in the `OpenTelemetry.Trace` namespace. Without the explicit `using` directive, the compiler cannot resolve them.
- **Fix:** Added `using OpenTelemetry.Trace;` to all 8 Program.cs files (alongside existing `using Serilog;` and `using Serilog.Events;`).
- **Files modified:** All 8 service Program.cs files
- **Commit:** 6821ad9

**4. [Rule 1 - Bug] .NET SDK 10.0.300 generates .slnx by default**
- **Found during:** Task 3 execution (`dotnet new sln` created Catalog.slnx)
- **Issue:** .NET SDK 10.0.300 changed the default solution format from `.sln` to `.slnx`. The PATTERNS.md commands assumed `.sln` output. The plan requires `.sln` files for Visual Studio compatibility.
- **Fix:** Used `dotnet new sln --format sln` flag for all 8 solutions. Removed the auto-created `Catalog.slnx` before creating `Catalog.sln`.
- **Files modified:** n/a (creation path)
- **Commit:** b50da31

## Verification Results

| Check | Result |
|-------|--------|
| All 8 .sln build independently (Release) | PASS — all 8 |
| No Swashbuckle in any .csproj | PASS |
| All 8 Program.cs wire /health (MapHealthChecks) | PASS — 8/8 |
| All 8 Program.cs wire OTel traces (AddOtlpExporter) | PASS — 8/8 |
| All 8 Program.cs wire Serilog logs (WriteTo.OpenTelemetry) | PASS — 8/8 |
| All 8 Program.cs enrich logs (Enrich.FromLogContext) | PASS — 8/8 |
| All 8 Program.cs use AddOpenApi (not AddSwaggerGen) | PASS — 8/8 |
| No Contracts PackageReference (uses ProjectReference) | PASS |
| All 8 .csproj reference building-blocks/Contracts | PASS — 8/8 |
| dotnet sln list: 2 projects per solution | PASS (confirmed on Catalog) |

## Requirements Satisfied

| Requirement | Status |
|-------------|--------|
| REPO-01: 8 independently-openable .sln files | Satisfied |
| REPO-02: All solutions reference Contracts via relative ProjectReference | Satisfied |
| INF-03: OTel + Serilog wired with W3C correlation IDs | Satisfied |
| INF-04: GET /health on each service | Satisfied |

## Known Stubs

All files are intentional stubs. The Program.cs files contain no business logic, no database configuration, and no domain code — this is by design for Phase 1. Observability and health are wired from day one; service-specific functionality begins in Phase 2 onward.

## Threat Flags

No new threat surface introduced beyond what the plan's threat model covers. The `/health` endpoint is unauthenticated by design (Phase 1 stubs; noted in T-02-01). OTLP endpoint is injected via environment variable — not hardcoded (T-02-01 mitigated). No secrets in appsettings (T-02-02 confirmed — no appsettings.json generated since we used Write tool, not `dotnet new web`).

## Self-Check: PASSED

| Item | Status |
|------|--------|
| src/services/catalog/ECommerce.Catalog.API/ECommerce.Catalog.API.csproj | FOUND |
| src/services/catalog/ECommerce.Catalog.API/Program.cs | FOUND |
| src/services/catalog/Catalog.sln | FOUND |
| src/services/notifications/ECommerce.Notifications.API/ECommerce.Notifications.API.csproj | FOUND |
| src/services/notifications/ECommerce.Notifications.API/Program.cs | FOUND |
| src/services/notifications/Notifications.sln | FOUND |
| .planning/phases/01-foundations/01-02-SUMMARY.md | FOUND |
| commit f969911 (Task 1: csproj files) | FOUND |
| commit 6821ad9 (Task 2: Program.cs + csproj fix) | FOUND |
| commit b50da31 (Task 3: .sln files) | FOUND |
