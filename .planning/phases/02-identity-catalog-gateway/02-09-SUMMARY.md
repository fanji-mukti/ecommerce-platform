---
phase: 02-identity-catalog-gateway
plan: "09"
subsystem: test-infrastructure
tags: [gap-closure, catalog, notifications, masstransit, ef-core, inbox-deduplication]
dependency_graph:
  requires: []
  provides: [catalog-integration-test-fix, notifications-inbox-dedup-fix, notifications-consumer-di-fix]
  affects: [catalog-tests, notifications-tests]
tech_stack:
  added: [Microsoft.EntityFrameworkCore.InMemory 10.0.9]
  patterns: [RemoveAll<T> for hosted service exclusion, transport-level MessageId pinning, in-memory EF provider for consumer DI]
key_files:
  modified:
    - src/services/catalog/ECommerce.Catalog.Tests/Integration/ProductsEndpointSteps.cs
    - src/services/notifications/ECommerce.Notifications.Tests/Integration/CatalogSeededInboxDeduplicationSteps.cs
    - src/services/notifications/ECommerce.Notifications.Tests/Integration/CatalogSeededConsumerSteps.cs
    - src/services/notifications/ECommerce.Notifications.Tests/ECommerce.Notifications.Tests.csproj
decisions:
  - "Added Microsoft.EntityFrameworkCore.InMemory package to Notifications test project to enable UseInMemoryDatabase for consumer DI test (Rule 3 fix — missing package caused build failure)"
  - "Used fully-qualified type name for RemoveAll<ECommerce.Catalog.API.Data.DbInitializer> to avoid ambiguity"
metrics:
  duration: "~10 minutes"
  completed: "2026-06-19"
  tasks: 2
  files_changed: 4
---

# Phase 02 Plan 09: Fix Three Test Infrastructure Gaps Summary

**One-liner:** Fixed DbInitializer conflict in CatalogWebApplicationFactory, inbox dedup MessageId pinning in notifications dedup test, and missing NotificationsDbContext registration in consumer harness test.

## Tasks Completed

| # | Task | Commit | Files |
|---|------|--------|-------|
| 1 | Fix CatalogWebApplicationFactory — remove DbInitializer and strip outbox from in-memory test transport | f4e0f6e | ProductsEndpointSteps.cs |
| 2 | Fix inbox dedup test (Gap 4) and consumer DI test (Gap 6) in Notifications test files | 376775f | CatalogSeededInboxDeduplicationSteps.cs, CatalogSeededConsumerSteps.cs, ECommerce.Notifications.Tests.csproj |

## What Was Built

Three test infrastructure fixes targeting gaps 3, 4, and 6 from the Phase 2 UAT gap analysis. All changes are confined to test files and test project configuration — no production code was modified.

**Gap 3 (CatalogWebApplicationFactory):**
- Added `services.RemoveAll<ECommerce.Catalog.API.Data.DbInitializer>()` after the MassTransit descriptor removal loop so DbInitializer no longer runs during WebApplicationFactory startup
- Removed `AddEntityFrameworkOutbox<CatalogDbContext>` from the in-memory MassTransit registration; the EF outbox only makes sense with a real ASB transport and interfered with raw CatalogDbContext seeding in Given_CatalogHasProducts

**Gap 4 (CatalogSeededInboxDeduplicationSteps):**
- Both `Bus.Publish(message)` calls in `When_SameMessagePublishedTwice` now pass `ctx => ctx.MessageId = messageId`, pinning the transport-level MessageId header to the same Guid on both publishes so the EF Core inbox stores exactly one InboxState row

**Gap 6 (CatalogSeededConsumerSteps):**
- Added `services.AddDbContext<NotificationsDbContext>(o => o.UseInMemoryDatabase("notifications-consumer-test"))` before the `AddMassTransitTestHarness` call so CatalogSeededConsumer can resolve its required DbContext from DI
- Added `using Microsoft.EntityFrameworkCore;` and `using ECommerce.Notifications.API.Data;` to support the new registration

## Deviations from Plan

### Auto-fixed Issues

**1. [Rule 3 - Blocking] Added Microsoft.EntityFrameworkCore.InMemory package**
- **Found during:** Task 2 build verification
- **Issue:** `UseInMemoryDatabase` extension method does not exist without the `Microsoft.EntityFrameworkCore.InMemory` NuGet package. The ECommerce.Notifications.Tests.csproj did not reference it, causing CS1061 build error.
- **Fix:** Added `<PackageReference Include="Microsoft.EntityFrameworkCore.InMemory" Version="10.0.9" />` to ECommerce.Notifications.Tests.csproj, matching the existing EF Core version pinned in that project.
- **Files modified:** src/services/notifications/ECommerce.Notifications.Tests/ECommerce.Notifications.Tests.csproj
- **Commit:** 376775f

## Verification Results

- `dotnet build src/services/catalog/ECommerce.Catalog.Tests/ECommerce.Catalog.Tests.csproj --no-incremental` — 0 errors, 5 warnings (pre-existing)
- `dotnet build src/services/notifications/ECommerce.Notifications.Tests/ECommerce.Notifications.Tests.csproj --no-incremental` — 0 errors, 0 warnings

## Known Stubs

None — all fixes are complete implementations, not placeholders.

## Threat Flags

None — changes are confined to test files and test project configuration. No new network endpoints, auth paths, file access patterns, or schema changes were introduced in production code.

## Self-Check: PASSED

- f4e0f6e exists in git log
- 376775f exists in git log
- ProductsEndpointSteps.cs contains `RemoveAll<ECommerce.Catalog.API.Data.DbInitializer>` and no `AddEntityFrameworkOutbox`
- CatalogSeededInboxDeduplicationSteps.cs contains `ctx => ctx.MessageId = messageId` on both publish calls, no bare `Bus.Publish(message)` calls
- CatalogSeededConsumerSteps.cs contains `AddDbContext<NotificationsDbContext>` with `UseInMemoryDatabase`, plus required using statements
