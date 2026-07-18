---
phase: 02-identity-catalog-gateway
plan: 01
subsystem: building-blocks
tags: [contracts, testing, masstransit, testcontainers, xunit]
dependency_graph:
  requires: []
  provides:
    - CatalogSeeded event contract (ECommerce.Catalog.Events.V1)
    - Tests.Common shared test infrastructure
  affects:
    - All Phase 2 service test projects (will reference Tests.Common)
    - Catalog service (publishes CatalogSeeded via outbox)
    - Notifications service (consumes CatalogSeeded via inbox)
tech_stack:
  added:
    - Testcontainers.PostgreSql 4.12.0
    - FluentAssertions 8.10.0
    - NSubstitute 5.3.0
    - MassTransit.TestFramework 8.3.6
    - Microsoft.AspNetCore.Mvc.Testing 10.0.9
    - xunit.v3.extensibility.core 3.2.2
  patterns:
    - IAsyncLifetime Testcontainers fixture (per-class isolation)
    - Fluent test data builder pattern (ProductBuilder, UserBuilder)
    - Generic WebApplicationFactory base class with connection string swap
key_files:
  created:
    - src/building-blocks/Contracts/Catalog/Events/V1/CatalogSeeded.cs
    - src/building-blocks/Tests.Common/ECommerce.Tests.Common.csproj
    - src/building-blocks/Tests.Common/PostgresFixture.cs
    - src/building-blocks/Tests.Common/ServiceWebApplicationFactory.cs
    - src/building-blocks/Tests.Common/Builders/ProductBuilder.cs
    - src/building-blocks/Tests.Common/Builders/UserBuilder.cs
  modified:
    - src/building-blocks/Contracts/Catalog/Events/V1/Placeholder.cs (deleted, replaced by CatalogSeeded.cs)
decisions:
  - "Used MassTransit.TestFramework (not MassTransit.Testing) — correct NuGet package name for 8.3.6 test harness"
  - "Used xunit.v3.extensibility.core for shared library IAsyncLifetime (not xunit.v3, which requires OutputType=Exe)"
  - "Used PostgreSqlBuilder with explicit image postgres:17-alpine to avoid deprecated parameterless constructor"
  - "ProductBuilder returns ProductData record (not the Catalog service's Product entity) to keep Tests.Common service-agnostic"
metrics:
  duration: "~15 minutes"
  completed: "2026-06-17"
  tasks: 2
  files: 6
---

# Phase 02 Plan 01: Contracts & Tests.Common Summary

**One-liner:** CatalogSeeded event record (7-param IMessageEnvelope) replaces Placeholder.cs, plus a shared Tests.Common library providing Testcontainers PostgresFixture, WebApplicationFactory base, ProductBuilder, and UserBuilder for all Phase 2 test projects.

---

## Tasks Completed

| # | Task | Commit | Status |
|---|------|--------|--------|
| 1 | Replace CatalogServiceReady placeholder with CatalogSeeded contract | a6547c0 | Done |
| 2 | Create Tests.Common shared infrastructure project | 27c0977 | Done |

---

## What Was Built

### Task 1: CatalogSeeded contract

`src/building-blocks/Contracts/Catalog/Events/V1/CatalogSeeded.cs` — replaces `Placeholder.cs` (which held `CatalogServiceReady`). The new record implements `IMessageEnvelope` with all four envelope fields first (`MessageId`, `CorrelationId`, `CausationId`, `OccurredAt`), followed by three domain-specific fields (`SeedId`, `int ItemCount`, `SeededAt`). Namespace is `ECommerce.Catalog.Events.V1`, single `using ECommerce.Contracts;` directive, zero new package dependencies in `Contracts.csproj`.

### Task 2: Tests.Common shared test infrastructure

`src/building-blocks/Tests.Common/` — new class library consumed by all Phase 2 test projects:

- **PostgresFixture**: `IAsyncLifetime` fixture using Testcontainers `PostgreSqlBuilder("postgres:17-alpine")`, exposing `ConnectionString` for per-class test isolation (D-28).
- **ServiceWebApplicationFactory\<TProgram\>**: Generic `WebApplicationFactory` base that swaps `ConnectionStrings:postgres` in `IConfiguration` for integration tests (D-29).
- **ProductBuilder**: Fluent builder returning `ProductData` record with sensible defaults (`Name="Test Product"`, `Sku="TST-001"`, `Price=9.99m`, `StockQuantity=100`, `Category="Electronics"`). Service-agnostic — test projects map `ProductData` to their service's `Product` entity by property name.
- **UserBuilder**: Fluent builder returning `UserData(Email, Password)` record with defaults `test@example.com` / `Password123!`.

---

## Deviations from Plan

### Auto-fixed Issues

**1. [Rule 1 - Bug] Wrong MassTransit testing package name**
- **Found during:** Task 2
- **Issue:** Plan specified `MassTransit.Testing` which does not exist on NuGet.org. The correct package for MassTransit 8.x test harness is `MassTransit.TestFramework`.
- **Fix:** Changed `PackageReference` to `MassTransit.TestFramework Version="8.3.6"` — same version pin, correct package ID.
- **Files modified:** `src/building-blocks/Tests.Common/ECommerce.Tests.Common.csproj`
- **Commit:** 27c0977

**2. [Rule 1 - Bug] xUnit.v3 cannot be used in a class library**
- **Found during:** Task 2
- **Issue:** `xunit.v3` package requires `<OutputType>Exe</OutputType>` (it's a test runner package). `Tests.Common` is a shared library, not an executable test project. Build error: "xUnit.net v3 test projects must be executable".
- **Fix:** Changed reference from `xunit.v3` to `xunit.v3.extensibility.core` which provides `IAsyncLifetime` in `Xunit` namespace for class library use.
- **Files modified:** `src/building-blocks/Tests.Common/ECommerce.Tests.Common.csproj`, `src/building-blocks/Tests.Common/PostgresFixture.cs` (added `using Xunit;`)
- **Commit:** 27c0977

**3. [Rule 2 - Missing functionality] PostgreSqlBuilder() parameterless constructor deprecated**
- **Found during:** Task 2
- **Issue:** Testcontainers 4.12.0 has deprecated the parameterless `PostgreSqlBuilder()` constructor. Build warning CS0618 flagged this as obsolete. The recommended fix is to pass the image name explicitly.
- **Fix:** Changed `new PostgreSqlBuilder()` to `new PostgreSqlBuilder("postgres:17-alpine")` to use the current non-deprecated API.
- **Files modified:** `src/building-blocks/Tests.Common/PostgresFixture.cs`
- **Commit:** 27c0977

---

## Known Stubs

None. This plan delivers foundational infrastructure (a contract record and test helpers). No UI rendering, no data sources, no placeholder values that block the plan's goal.

---

## Threat Flags

No new security-relevant surface introduced. `CatalogSeeded.cs` is a compile-time-only contract record. `Tests.Common` is test infrastructure only — never deployed to production. Hardcoded Testcontainers credentials (`test`/`test`) are ephemeral local instances, consistent with `T-02-01-02` accept disposition in the plan's threat model.

---

## Self-Check: PASSED

| Item | Result |
|------|--------|
| CatalogSeeded.cs exists | FOUND |
| ECommerce.Tests.Common.csproj exists | FOUND |
| PostgresFixture.cs exists | FOUND |
| ServiceWebApplicationFactory.cs exists | FOUND |
| ProductBuilder.cs exists | FOUND |
| UserBuilder.cs exists | FOUND |
| Commit a6547c0 (Task 1) | FOUND |
| Commit 27c0977 (Task 2) | FOUND |
