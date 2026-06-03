# Phase 1: Foundations - Pattern Map

**Mapped:** 2026-06-03
**Files analyzed:** 38 new/modified files across 4 categories
**Analogs found:** 0 / 38 from existing codebase (greenfield project)
**Pattern source:** RESEARCH.md patterns + existing Contracts.csproj scaffold

> **Greenfield note:** The only existing source code is
> `src/building-blocks/Contracts/Contracts.csproj` (a 9-line .NET 10 SDK-style
> class library) and `src/building-blocks/Contracts/Class1.cs` (a 6-line stub).
> There are no controllers, services, Program.cs files, or solution files to
> mine for patterns. Every pattern in this document is derived from RESEARCH.md
> verified code excerpts and the project settings established by Contracts.csproj.

---

## File Classification

### Group A: Contracts Library (modify existing + add files)

| New/Modified File | Role | Data Flow | Closest Analog | Match Quality |
|-------------------|------|-----------|----------------|---------------|
| `src/building-blocks/Contracts/Contracts.csproj` | config | — | Self (existing) | exact — reuse as-is, no changes needed |
| `src/building-blocks/Contracts/Class1.cs` | — | — | None | delete — replaced by below |
| `src/building-blocks/Contracts/IMessageEnvelope.cs` | model (interface) | — | None in codebase | use RESEARCH.md Pattern 2 |
| `src/building-blocks/Contracts/Catalog/Events/V1/Placeholder.cs` | model (record stub) | — | None in codebase | use RESEARCH.md Pattern 2 |
| `src/building-blocks/Contracts/Cart/Events/V1/Placeholder.cs` | model (record stub) | — | None in codebase | use RESEARCH.md Pattern 2 |
| `src/building-blocks/Contracts/Checkout/Events/V1/Placeholder.cs` | model (record stub) | — | None in codebase | use RESEARCH.md Pattern 2 |
| `src/building-blocks/Contracts/Orders/Events/V1/Placeholder.cs` | model (record stub) | — | None in codebase | use RESEARCH.md Pattern 2 |
| `src/building-blocks/Contracts/Identity/Events/V1/Placeholder.cs` | model (record stub) | — | None in codebase | use RESEARCH.md Pattern 2 |
| `src/building-blocks/Contracts/Payments/Events/V1/Placeholder.cs` | model (record stub) | — | None in codebase | use RESEARCH.md Pattern 2 |
| `src/building-blocks/Contracts/Fulfillment/Events/V1/Placeholder.cs` | model (record stub) | — | None in codebase | use RESEARCH.md Pattern 2 |
| `src/building-blocks/Contracts/Notifications/Events/V1/Placeholder.cs` | model (record stub) | — | None in codebase | use RESEARCH.md Pattern 2 |
| `src/building-blocks/Contracts/Contracts.sln` | config | — | None in codebase | use standard `dotnet new sln` output |

### Group B: Service Stubs (8 services, create from scratch)

| New/Modified File | Role | Data Flow | Closest Analog | Match Quality |
|-------------------|------|-----------|----------------|---------------|
| `src/services/catalog/ECommerce.Catalog.API/ECommerce.Catalog.API.csproj` | config | — | `Contracts.csproj` | partial — copy PropertyGroup; add PackageReferences |
| `src/services/catalog/ECommerce.Catalog.API/Program.cs` | provider (entry point) | request-response | None in codebase | use RESEARCH.md Pattern 3 |
| `src/services/catalog/Catalog.sln` | config | — | None in codebase | `dotnet new sln` |
| `src/services/cart/ECommerce.Cart.API/ECommerce.Cart.API.csproj` | config | — | `Contracts.csproj` | partial |
| `src/services/cart/ECommerce.Cart.API/Program.cs` | provider | request-response | None in codebase | use RESEARCH.md Pattern 3 |
| `src/services/cart/Cart.sln` | config | — | None | `dotnet new sln` |
| `src/services/checkout/ECommerce.Checkout.API/ECommerce.Checkout.API.csproj` | config | — | `Contracts.csproj` | partial |
| `src/services/checkout/ECommerce.Checkout.API/Program.cs` | provider | request-response | None | use RESEARCH.md Pattern 3 |
| `src/services/checkout/Checkout.sln` | config | — | None | `dotnet new sln` |
| `src/services/orders/ECommerce.Orders.API/ECommerce.Orders.API.csproj` | config | — | `Contracts.csproj` | partial |
| `src/services/orders/ECommerce.Orders.API/Program.cs` | provider | request-response | None | use RESEARCH.md Pattern 3 |
| `src/services/orders/Orders.sln` | config | — | None | `dotnet new sln` |
| `src/services/identity/ECommerce.Identity.API/ECommerce.Identity.API.csproj` | config | — | `Contracts.csproj` | partial |
| `src/services/identity/ECommerce.Identity.API/Program.cs` | provider | request-response | None | use RESEARCH.md Pattern 3 |
| `src/services/identity/Identity.sln` | config | — | None | `dotnet new sln` |
| `src/services/payments/ECommerce.Payments.API/ECommerce.Payments.API.csproj` | config | — | `Contracts.csproj` | partial |
| `src/services/payments/ECommerce.Payments.API/Program.cs` | provider | request-response | None | use RESEARCH.md Pattern 3 |
| `src/services/payments/Payments.sln` | config | — | None | `dotnet new sln` |
| `src/services/fulfillment/ECommerce.Fulfillment.API/ECommerce.Fulfillment.API.csproj` | config | — | `Contracts.csproj` | partial |
| `src/services/fulfillment/ECommerce.Fulfillment.API/Program.cs` | provider | request-response | None | use RESEARCH.md Pattern 3 |
| `src/services/fulfillment/Fulfillment.sln` | config | — | None | `dotnet new sln` |
| `src/services/notifications/ECommerce.Notifications.API/ECommerce.Notifications.API.csproj` | config | — | `Contracts.csproj` | partial |
| `src/services/notifications/ECommerce.Notifications.API/Program.cs` | provider | request-response | None | use RESEARCH.md Pattern 3 |
| `src/services/notifications/Notifications.sln` | config | — | None | `dotnet new sln` |

### Group C: Aspire AppHost (create from scratch)

| New/Modified File | Role | Data Flow | Closest Analog | Match Quality |
|-------------------|------|-----------|----------------|---------------|
| `src/ecommerce.AppHost/ecommerce.AppHost.csproj` | config | — | None in codebase | use RESEARCH.md Pattern 1 |
| `src/ecommerce.AppHost/Program.cs` | provider (orchestrator) | event-driven | None in codebase | use RESEARCH.md Pattern 1 |
| `src/ecommerce.AppHost/ecommerce.AppHost.sln` | config | — | None | `dotnet new sln` |

### Group D: Cross-cutting Delivery Files (create from scratch)

| New/Modified File | Role | Data Flow | Closest Analog | Match Quality |
|-------------------|------|-----------|----------------|---------------|
| `.github/workflows/ci.yml` | config (CI) | — | None in codebase | use RESEARCH.md Pattern 5 |
| `docs/adr/0001-use-madr-format.md` | documentation | — | None | use RESEARCH.md Pattern 4 |
| `docs/adr/0002-azure-service-bus.md` | documentation | — | None | use RESEARCH.md Pattern 4 |
| `docs/adr/0003-database-per-service.md` | documentation | — | None | use RESEARCH.md Pattern 4 |
| `docs/adr/0004-yarp-api-gateway.md` | documentation | — | None | use RESEARCH.md Pattern 4 |
| `docs/adr/0005-saga-orchestration.md` | documentation | — | None | use RESEARCH.md Pattern 4 |
| `docs/adr/0006-masstransit-outbox-inbox.md` | documentation | — | None | use RESEARCH.md Pattern 4 |
| `docs/adr/0007-asb-topic-per-context.md` | documentation | — | None | use RESEARCH.md Pattern 4 |
| `docs/adr/0008-mono-repo-multi-solution.md` | documentation | — | None | use RESEARCH.md Pattern 4 |

---

## Pattern Assignments

### Group A: Contracts Library

---

#### `src/building-blocks/Contracts/Contracts.csproj` (config — reuse existing)

**Source:** Existing file at `src/building-blocks/Contracts/Contracts.csproj` (lines 1–9)

**Full file content (read, do not re-read):**
```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
  </PropertyGroup>

</Project>
```

**Action:** Reuse as-is. No NuGet `<PackageReference>` elements are ever added. No `<FrameworkReference>` elements are added. These three PropertyGroup settings (`net10.0`, `ImplicitUsings enable`, `Nullable enable`) are the canonical C# project settings for this repo — all service stub `.csproj` files copy them.

**Constraint from D-03/RESEARCH.md Pitfall 6:** The Contracts csproj must never contain `<PackageReference>` or `<FrameworkReference>`. Only `Microsoft.NET.Sdk` and target framework. PR checklist: `dotnet build src/building-blocks/Contracts/Contracts.csproj` output size < 10 KB.

---

#### `src/building-blocks/Contracts/IMessageEnvelope.cs` (model/interface)

**Source:** RESEARCH.md Pattern 2 (lines 344–356)

**Core pattern:**
```csharp
// File: src/building-blocks/Contracts/IMessageEnvelope.cs
namespace ECommerce.Contracts;

public interface IMessageEnvelope
{
    Guid MessageId { get; }
    Guid CorrelationId { get; }
    Guid CausationId { get; }
    DateTimeOffset OccurredAt { get; }
}
```

**Key notes:**
- Namespace is `ECommerce.Contracts` (not `ECommerce.Building.Blocks` — intentionally short).
- Interface uses `{ get; }` only (not `{ get; init; }`) — records satisfy this via positional constructor synthesis.
- No class file header comments, no using directives (implicit usings enabled on the project).
- No `sealed` modifier — the interface must remain open for implementation by records across all services.

---

#### `src/building-blocks/Contracts/Catalog/Events/V1/Placeholder.cs` (model/record stub)

**Source:** RESEARCH.md Pattern 2 (lines 358–367)

**Core pattern (copy for all 8 service namespace stubs):**
```csharp
// File: src/building-blocks/Contracts/{ServiceName}/Events/V1/Placeholder.cs
namespace ECommerce.{ServiceName}.Events.V1;

public record {ServiceName}ServiceReady(
    Guid MessageId,
    Guid CorrelationId,
    Guid CausationId,
    DateTimeOffset OccurredAt
) : IMessageEnvelope;
```

**Namespace substitution table** (D-02):

| Service | Namespace | Record name |
|---------|-----------|-------------|
| Catalog | `ECommerce.Catalog.Events.V1` | `CatalogServiceReady` |
| Cart | `ECommerce.Cart.Events.V1` | `CartServiceReady` |
| Checkout | `ECommerce.Checkout.Events.V1` | `CheckoutServiceReady` |
| Orders | `ECommerce.Orders.Events.V1` | `OrdersServiceReady` |
| Identity | `ECommerce.Identity.Events.V1` | `IdentityServiceReady` |
| Payments | `ECommerce.Payments.Events.V1` | `PaymentsServiceReady` |
| Fulfillment | `ECommerce.Fulfillment.Events.V1` | `FulfillmentServiceReady` |
| Notifications | `ECommerce.Notifications.Events.V1` | `NotificationsServiceReady` |

**Key notes:**
- Positional record parameters generate `{ get; init; }` automatically, satisfying `IMessageEnvelope`'s `{ get; }` contract. Do NOT add `{ get; init; }` explicitly to positional parameters — it is redundant and non-idiomatic.
- The `IMessageEnvelope` interface is in the `ECommerce.Contracts` namespace. Because `ImplicitUsings` only includes system namespaces, each Placeholder.cs file needs either a `using ECommerce.Contracts;` directive or a global using configured at the project level. Prefer `using ECommerce.Contracts;` in each file for explicitness since Contracts is a class library (not a web app).
- File path follows the namespace exactly: `Catalog/Events/V1/Placeholder.cs` under the Contracts project root.

---

#### `src/building-blocks/Contracts/Contracts.sln` (config)

**Pattern:** Standard `dotnet new sln` output. References only `Contracts.csproj`. Used as the CI matrix entry for the Contracts build job.

**Shell command to generate:**
```bash
dotnet new sln -n Contracts -o src/building-blocks/Contracts
dotnet sln src/building-blocks/Contracts/Contracts.sln add src/building-blocks/Contracts/Contracts.csproj
```

---

### Group B: Service Stubs

All 8 service stubs are structurally identical. The pattern is defined once here and applied to all 8. Substitution table follows each excerpt.

---

#### `src/services/{service}/ECommerce.{Service}.API/ECommerce.{Service}.API.csproj` (config)

**Source:** `src/building-blocks/Contracts/Contracts.csproj` property group (established baseline) + RESEARCH.md Standard Stack for package versions.

**Full csproj pattern:**
```xml
<Project Sdk="Microsoft.NET.Sdk.Web">

  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Serilog.AspNetCore" Version="10.0.0" />
    <PackageReference Include="Serilog.Sinks.OpenTelemetry" Version="4.2.0" />
    <PackageReference Include="OpenTelemetry.Extensions.Hosting" Version="1.15.3" />
    <PackageReference Include="OpenTelemetry.Instrumentation.AspNetCore" Version="1.15.2" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="../../../building-blocks/Contracts/Contracts.csproj" />
  </ItemGroup>

</Project>
```

**Key notes:**
- SDK changes from `Microsoft.NET.Sdk` (class library) to `Microsoft.NET.Sdk.Web` (web app). This is the only csproj difference between Contracts and a service stub.
- The three `PropertyGroup` settings (`net10.0`, `ImplicitUsings`, `Nullable`) are copied verbatim from `Contracts.csproj` — they are the repo standard (D-05, existing scaffold).
- `ProjectReference` path is relative from each service's project folder. For a service at `src/services/{service}/ECommerce.{Service}.API/`, the relative path to Contracts is `../../../building-blocks/Contracts/Contracts.csproj`.
- Package versions are pinned exactly as verified in RESEARCH.md Standard Stack.
- No `<FrameworkReference Include="Microsoft.AspNetCore.App" />` needed — `Microsoft.NET.Sdk.Web` includes it implicitly.

**Service substitution table:**

| Service | Folder | Project file |
|---------|--------|--------------|
| Catalog | `src/services/catalog/ECommerce.Catalog.API/` | `ECommerce.Catalog.API.csproj` |
| Cart | `src/services/cart/ECommerce.Cart.API/` | `ECommerce.Cart.API.csproj` |
| Checkout | `src/services/checkout/ECommerce.Checkout.API/` | `ECommerce.Checkout.API.csproj` |
| Orders | `src/services/orders/ECommerce.Orders.API/` | `ECommerce.Orders.API.csproj` |
| Identity | `src/services/identity/ECommerce.Identity.API/` | `ECommerce.Identity.API.csproj` |
| Payments | `src/services/payments/ECommerce.Payments.API/` | `ECommerce.Payments.API.csproj` |
| Fulfillment | `src/services/fulfillment/ECommerce.Fulfillment.API/` | `ECommerce.Fulfillment.API.csproj` |
| Notifications | `src/services/notifications/ECommerce.Notifications.API/` | `ECommerce.Notifications.API.csproj` |

---

#### `src/services/{service}/ECommerce.{Service}.API/Program.cs` (provider, request-response)

**Source:** RESEARCH.md Pattern 3 (lines 376–405)

**Full file pattern:**
```csharp
using Serilog;
using Serilog.Events;

// Bootstrap logger — captures startup failures before host is built
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.OpenTelemetry()
    .CreateLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    builder.Host.UseSerilog();

    builder.Services.AddOpenApi();
    builder.Services.AddHealthChecks();

    builder.Services.AddOpenTelemetry()
        .WithTracing(tracing => tracing
            .AddAspNetCoreInstrumentation()
            .AddOtlpExporter());

    var app = builder.Build();

    app.UseHttpsRedirection();
    app.MapOpenApi();
    app.MapHealthChecks("/health");

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
    return 1;
}
finally
{
    Log.CloseAndFlush();
}

return 0;
```

**Key notes:**
- The `try/catch/finally` wrapper around the host is the Serilog-recommended pattern for capturing startup exceptions in structured logs. Without it, exceptions thrown during `builder.Build()` are lost.
- `WriteTo.OpenTelemetry()` exports Serilog events as OTLP log records to the endpoint specified by `OTEL_EXPORTER_OTLP_ENDPOINT` environment variable — injected by Aspire at runtime. No hardcoded endpoint.
- `.AddOtlpExporter()` similarly reads `OTEL_EXPORTER_OTLP_ENDPOINT` from env. Both are set automatically by Aspire when the service is registered with `WithReference(otelCollector)` (or Aspire's built-in dashboard).
- `app.MapOpenApi()` uses `Microsoft.AspNetCore.OpenApi` (built-in to .NET 10, no extra NuGet). Do NOT install Swashbuckle.
- `MapHealthChecks("/health")` uses built-in ASP.NET Core health checks. Default `ResponseWriter` returns JSON with status `Healthy`. No additional NuGet package needed (D-13).
- The `return 0` / `return 1` pattern requires `<Nullable>enable</Nullable>` and C# 9+ top-level statements — both already established by the project settings.
- `app.UseHttpsRedirection()` is included for completeness; Aspire will override HTTPS settings via env vars in development.
- All 8 services use this exact pattern. Only the service-specific `using` statements or registered services differ in later phases.

---

#### `src/services/{service}/{Service}.sln` (config)

**Pattern:** Standard `dotnet new sln` referencing only the service's API project and the Contracts project.

**Shell commands (example for Catalog):**
```bash
dotnet new sln -n Catalog -o src/services/catalog
dotnet sln src/services/catalog/Catalog.sln add src/services/catalog/ECommerce.Catalog.API/ECommerce.Catalog.API.csproj
dotnet sln src/services/catalog/Catalog.sln add src/building-blocks/Contracts/Contracts.csproj
```

**Service solution name table** (D-07):

| Service | Solution file | Service project to add |
|---------|---------------|------------------------|
| Catalog | `src/services/catalog/Catalog.sln` | `ECommerce.Catalog.API.csproj` |
| Cart | `src/services/cart/Cart.sln` | `ECommerce.Cart.API.csproj` |
| Checkout | `src/services/checkout/Checkout.sln` | `ECommerce.Checkout.API.csproj` |
| Orders | `src/services/orders/Orders.sln` | `ECommerce.Orders.API.csproj` |
| Identity | `src/services/identity/Identity.sln` | `ECommerce.Identity.API.csproj` |
| Payments | `src/services/payments/Payments.sln` | `ECommerce.Payments.API.csproj` |
| Fulfillment | `src/services/fulfillment/Fulfillment.sln` | `ECommerce.Fulfillment.API.csproj` |
| Notifications | `src/services/notifications/Notifications.sln` | `ECommerce.Notifications.API.csproj` |

**Note from RESEARCH.md Open Question 2:** Each service solution references `Contracts.csproj` directly (not `Contracts.sln`). The CI matrix entry `Contracts.sln` exists only for the standalone Contracts build job.

---

### Group C: Aspire AppHost

---

#### `src/ecommerce.AppHost/ecommerce.AppHost.csproj` (config)

**Source:** RESEARCH.md Pattern 1 (lines 288–299)

**Full csproj pattern:**
```xml
<Project Sdk="Aspire.AppHost.Sdk/13.4.0">

  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net10.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Aspire.Hosting.Azure.ServiceBus" Version="13.4.0" />
    <PackageReference Include="Aspire.Hosting.PostgreSQL" Version="13.4.0" />
    <PackageReference Include="Aspire.Hosting.Redis" Version="13.4.0" />
    <PackageReference Include="Aspire.Hosting.Docker" Version="13.4.0" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="../services/catalog/ECommerce.Catalog.API/ECommerce.Catalog.API.csproj" />
    <ProjectReference Include="../services/cart/ECommerce.Cart.API/ECommerce.Cart.API.csproj" />
    <ProjectReference Include="../services/checkout/ECommerce.Checkout.API/ECommerce.Checkout.API.csproj" />
    <ProjectReference Include="../services/orders/ECommerce.Orders.API/ECommerce.Orders.API.csproj" />
    <ProjectReference Include="../services/identity/ECommerce.Identity.API/ECommerce.Identity.API.csproj" />
    <ProjectReference Include="../services/payments/ECommerce.Payments.API/ECommerce.Payments.API.csproj" />
    <ProjectReference Include="../services/fulfillment/ECommerce.Fulfillment.API/ECommerce.Fulfillment.API.csproj" />
    <ProjectReference Include="../services/notifications/ECommerce.Notifications.API/ECommerce.Notifications.API.csproj" />
  </ItemGroup>

</Project>
```

**Key notes:**
- SDK is `Aspire.AppHost.Sdk/13.4.0` — this is not `Microsoft.NET.Sdk.Web`. This SDK handles the Aspire source generation that produces `Projects.ECommerce_Catalog_API` typed references.
- `OutputType>Exe` is required — AppHost is an executable that runs the orchestration loop.
- `TargetFramework`, `ImplicitUsings`, and `Nullable` are copied from the existing Contracts.csproj baseline (repo standard).
- RESEARCH.md Pitfall 4: If SDK version in the csproj Sdk attribute mismatches the installed Aspire workload, use `Sdk="Aspire.AppHost.Sdk"` (without version suffix) to let MSBuild resolve from the installed SDK. Alternatively, scaffold using `dotnet new aspire-apphost` to have the version set correctly.
- `Aspire.Hosting.Docker` is required for `aspire publish` to produce docker-compose output (RESEARCH.md Pitfall 3).
- `Aspire.Hosting` core is a transitive dependency of `Aspire.AppHost.Sdk` — do not add it explicitly.

---

#### `src/ecommerce.AppHost/Program.cs` (provider/orchestrator, event-driven)

**Source:** RESEARCH.md Pattern 1 (lines 302–327)

**Full file pattern:**
```csharp
var builder = DistributedApplication.CreateBuilder(args);

// Infrastructure resources
var postgres = builder.AddPostgres("postgres");
var redis = builder.AddRedis("redis");
var serviceBus = builder.AddAzureServiceBus("messaging")
    .RunAsEmulator();

// Service stubs — Aspire derives class name by replacing dots with underscores:
// ECommerce.Catalog.API.csproj → Projects.ECommerce_Catalog_API
builder.AddProject<Projects.ECommerce_Catalog_API>("catalog")
    .WithReference(postgres)
    .WithReference(serviceBus);

builder.AddProject<Projects.ECommerce_Cart_API>("cart")
    .WithReference(postgres)
    .WithReference(redis)
    .WithReference(serviceBus);

builder.AddProject<Projects.ECommerce_Checkout_API>("checkout")
    .WithReference(postgres)
    .WithReference(serviceBus);

builder.AddProject<Projects.ECommerce_Orders_API>("orders")
    .WithReference(postgres)
    .WithReference(serviceBus);

builder.AddProject<Projects.ECommerce_Identity_API>("identity")
    .WithReference(postgres);

builder.AddProject<Projects.ECommerce_Payments_API>("payments")
    .WithReference(serviceBus);

builder.AddProject<Projects.ECommerce_Fulfillment_API>("fulfillment")
    .WithReference(serviceBus);

builder.AddProject<Projects.ECommerce_Notifications_API>("notifications")
    .WithReference(serviceBus);

// Required for aspire publish → docker-compose.yaml (RESEARCH.md Pitfall 3)
builder.AddDockerComposeEnvironment("ecommerce-local");

builder.Build().Run();
```

**Key notes on `AddProject` type names** (RESEARCH.md Pattern 1, lines 279–280):
- Aspire source generator replaces every dot (`.`) in the project filename with an underscore (`_`) to form the `Projects.*` class name.
- `ECommerce.Catalog.API.csproj` → `Projects.ECommerce_Catalog_API`
- `ECommerce.Cart.API.csproj` → `Projects.ECommerce_Cart_API`
- The `ecommerce.AppHost` project itself is never passed to `AddProject<T>` — only service stubs are.
- `RunAsEmulator()` on `AddAzureServiceBus` provisions `mcr.microsoft.com/azure-messaging/servicebus-emulator:latest` (+ companion SQL Server) as Aspire-managed containers. Connection strings are injected into services as environment variables.
- Phase 1 services do not consume the connection strings yet (no MassTransit config) — the references are registered now so services appear in the Aspire dashboard topology view.
- `AddDockerComposeEnvironment("ecommerce-local")` must be called before `builder.Build().Run()`. The string `"ecommerce-local"` becomes the docker compose project name.

---

#### `src/ecommerce.AppHost/ecommerce.AppHost.sln` (config)

**Pattern:** Standard `dotnet new sln` referencing only the AppHost csproj.

```bash
dotnet new sln -n ecommerce.AppHost -o src/ecommerce.AppHost
dotnet sln src/ecommerce.AppHost/ecommerce.AppHost.sln add src/ecommerce.AppHost/ecommerce.AppHost.csproj
```

**Note:** The AppHost solution does NOT include the 8 service projects in the solution file — each service is a `<ProjectReference>` in the csproj, but the `.sln` file for the AppHost only contains the AppHost project itself (D-08).

---

### Group D: GitHub Actions CI

---

#### `.github/workflows/ci.yml` (config/CI)

**Source:** RESEARCH.md Pattern 5 (lines 472–510)

**Full file pattern:**
```yaml
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
      fail-fast: false
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

**Key notes:**
- `fail-fast: false` is load-bearing — without it, the first failing solution cancels all 9 remaining parallel jobs, hiding which solutions are broken (RESEARCH.md Pattern 5, line 484).
- `actions/setup-dotnet@v4` with `dotnet-version: '10.0.x'` installs the latest .NET 10 patch. Pinning to `10.0.x` (not `10.0.300`) allows patch updates without workflow file changes.
- `dotnet test` with `--no-build` on Phase 1 is safe — no test projects exist yet; the command exits with `0` (no tests found is not an error in xUnit v3 or `dotnet test`).
- The 10-item matrix (D-16) covers: 1 Contracts + 1 AppHost + 8 services = 10 solutions.

---

### Group D: MADR ADRs

All 8 ADRs follow the same structural template. The pattern is defined once; content differs per decision.

---

#### `docs/adr/000N-{slug}.md` (documentation, MADR 4.0)

**Source:** RESEARCH.md Pattern 4 (lines 412–461)

**Template pattern (copy for all 8 ADRs):**
```markdown
---
status: accepted
date: 2026-06-03
decision-makers: [Fanji Ari Mukti]
consulted: []
informed: []
---

# [Short imperative title matching the filename slug]

## Context and Problem Statement

[2–3 sentences describing the architectural force and problem being resolved.]

## Decision Drivers

* [Force 1 — e.g., portfolio visibility in job market]
* [Force 2 — e.g., OSS license compatibility]
* [Force 3 — e.g., alignment with Azure deployment target]

## Considered Options

* [Option A — the chosen option]
* [Option B — the rejected option]
* [Option C if applicable]

## Decision Outcome

Chosen option: "[Option A]", because [1–2 sentence justification referencing the decision drivers].

### Consequences

* Good, because [positive effect on the system or developer experience]
* Bad, because [accepted tradeoff or known risk]

## Pros and Cons of the Options

### [Option A]

* Good, because [...]
* Bad, because [...]

### [Option B]

* Good, because [...]
* Bad, because [...]

## More Information

[Links to official docs, related ADRs (e.g., "See ADR-0002"), implementation notes, or
open questions to resolve in later phases.]
```

**ADR file list and content guidance** (D-17, D-18):

| File | Title | Key content notes |
|------|-------|-------------------|
| `docs/adr/0001-use-madr-format.md` | Use MADR for Architectural Decision Records | Options: MADR 4.0 vs plain markdown vs RFC vs Log4Brains. Chosen: MADR 4.0. |
| `docs/adr/0002-azure-service-bus.md` | Use Azure Service Bus as messaging backbone | Options: ASB vs RabbitMQ vs Kafka vs in-process events. Chosen: ASB (Azure deployment target, topics/subscriptions, native Azure managed identity). |
| `docs/adr/0003-database-per-service.md` | Enforce database-per-service isolation | Options: shared DB vs DB-per-service. Chosen: DB-per-service. Note: Phase 1 provisions one shared Postgres container for simplicity — per-database isolation enforced at schema/connection-string level. |
| `docs/adr/0004-yarp-api-gateway.md` | Use YARP as the API gateway | Options: YARP vs Ocelot vs Azure API Management vs no gateway. Chosen: YARP (Ocelot maintenance-mode, APIM cost). |
| `docs/adr/0005-saga-orchestration.md` | Use saga orchestration over choreography | Options: orchestration (MassTransit state machine) vs choreography (event chain). Chosen: orchestration (explicit state, testable, compensation paths visible). |
| `docs/adr/0006-masstransit-outbox-inbox.md` | Use MassTransit 8.3.6 with transactional outbox/inbox | Options: MassTransit 8.x vs MassTransit 9.x vs NServiceBus vs raw ASB SDK. Chosen: MassTransit 8.3.6 (Apache-2.0 last OSS version; v9 requires Massient commercial license). MUST document: v9 pinning risk, `EmulatorHost()` missing from v8 workaround, and v8 EOL end-2026. |
| `docs/adr/0007-asb-topic-per-context.md` | One ASB topic per producing bounded context | Options: topic-per-message vs topic-per-context. Chosen: topic-per-context (fewer entities, aligned with bounded context principle). Note Phase 2 MassTransit `[EntityName]` override. |
| `docs/adr/0008-mono-repo-multi-solution.md` | Mono-repo with independent per-service solution files | Options: mono-repo multi-sln vs poly-repo vs mono-repo single-sln. Chosen: mono-repo multi-sln (Visual Studio independent openability + shared Contracts via project reference). |

**MADR frontmatter rules** (RESEARCH.md Pattern 4, lines 463–465):
- `status` must be one of: `proposed`, `rejected`, `accepted`, `deprecated`, `superseded by [NNNN]`
- `date` format: `YYYY-MM-DD`
- File naming: `docs/adr/NNNN-kebab-case-title.md` — zero-padded 4-digit number

---

## Shared Patterns

### C# Project Settings Baseline

**Source:** `src/building-blocks/Contracts/Contracts.csproj` (lines 4–6) — the only existing source file in the repo establishes these as the project-wide C# standard.

**Apply to:** Every `.csproj` file in this phase (Contracts, 8 service stubs, AppHost).

```xml
<TargetFramework>net10.0</TargetFramework>
<ImplicitUsings>enable</ImplicitUsings>
<Nullable>enable</Nullable>
```

**Implication:** `ImplicitUsings enable` means `using System;`, `using System.Collections.Generic;`, `using System.Linq;`, `using System.Threading.Tasks;` etc. are available without explicit `using` directives in all `.cs` files. `Guid`, `DateTimeOffset`, `Task`, `List<T>`, and `IEnumerable<T>` are all available without extra usings.

---

### OTel + Serilog Wiring

**Source:** RESEARCH.md Pattern 3 (lines 376–405) — the same four packages and same wiring appear in every service stub.

**Apply to:** All 8 `Program.cs` files.

```csharp
// Bootstrap logger (before builder)
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.OpenTelemetry()   // OTLP endpoint from OTEL_EXPORTER_OTLP_ENDPOINT env var
    .CreateLogger();

// On builder.Host
builder.Host.UseSerilog();

// On builder.Services
builder.Services.AddOpenTelemetry()
    .WithTracing(tracing => tracing
        .AddAspNetCoreInstrumentation()
        .AddOtlpExporter());
```

**W3C TraceContext enrichment** (D-12): `Enrich.FromLogContext()` combined with `WriteTo.OpenTelemetry()` carries the active `TraceId` and `SpanId` from the OTel activity into every Serilog log event. No additional enricher package is required — the OTel sink bridges the two automatically.

---

### Health Check Registration

**Source:** RESEARCH.md Pattern 3 (lines 392–403) — built-in ASP.NET Core, no NuGet.

**Apply to:** All 8 `Program.cs` files.

```csharp
// Registration
builder.Services.AddHealthChecks();

// Mapping
app.MapHealthChecks("/health");
```

**Response:** Default `HealthCheckOptions.ResponseWriter` returns `Content-Type: application/json` with `{"status":"Healthy"}`. D-13 specifies this default — no custom writer needed in Phase 1.

---

### No Analog Found

| File | Role | Data Flow | Reason |
|------|------|-----------|--------|
| All 38 files listed above | various | various | Greenfield project — no existing service, controller, middleware, or test files exist in the codebase. All patterns sourced from RESEARCH.md verified code excerpts and the single existing `Contracts.csproj` scaffold. |

---

## Metadata

**Analog search scope:** `src/` (entire source tree)
**Files scanned:** 2 (`Contracts.csproj`, `Class1.cs`)
**Pattern extraction date:** 2026-06-03
**Primary pattern source:** `.planning/phases/01-foundations/01-RESEARCH.md` (Patterns 1–5)
**Secondary pattern source:** `src/building-blocks/Contracts/Contracts.csproj` (PropertyGroup baseline)

**Scaffolding tool to use:** `dotnet new` CLI commands — do NOT hand-author `.sln` files or AppHost csproj. Use:
- `dotnet new sln` for all solution files
- `dotnet new aspire-apphost` or manual csproj for AppHost (verify template availability)
- `dotnet new webapi --use-minimal-apis` for service stubs, then trim to match the Program.cs pattern above

**Files NOT to hand-author:**
- `docker-compose.yml` — generated by `aspire publish -o ./` from the AppHost (RESEARCH.md Anti-Pattern)
- Any `.sln` file body — always generate with `dotnet new sln` + `dotnet sln add`
