---
phase: 02-identity-catalog-gateway
plan: "08"
subsystem: identity, catalog, notifications
tags: [gap-closure, seed, password-policy, service-bus, standalone-startup]
dependency_graph:
  requires: []
  provides:
    - identity-demo-seed-working
    - catalog-standalone-startup
    - notifications-standalone-startup
  affects:
    - identity-login-flow
    - catalog-messaging
    - notifications-messaging
tech_stack:
  added: []
  patterns:
    - IdentityResult error check with startup-time throw
    - appsettings.Development.json ASB emulator fallback
key_files:
  created:
    - src/services/catalog/ECommerce.Catalog.API/appsettings.Development.json
    - src/services/notifications/ECommerce.Notifications.API/appsettings.Development.json
  modified:
    - src/services/identity/ECommerce.Identity.API/Data/DbInitializer.cs
    - src/services/identity/ECommerce.Identity.API/Program.cs
decisions:
  - Relaxed RequireUppercase and RequireNonAlphanumeric in Identity options for dev; demo passwords Demo123!/Admin123! satisfy remaining defaults (RequireDigit, RequiredLength=6)
  - appsettings.Development.json with UseDevelopmentEmulator=true added to Catalog and Notifications; Aspire overrides at runtime; no postgres entry added (handled by Aspire service discovery)
  - SeedUserIfNotExists now throws InvalidOperationException on IdentityResult failure — startup exception is preferable to silent discard
metrics:
  duration: ~8min
  completed_date: "2026-06-19"
  tasks: 2
  files_changed: 4
---

# Phase 02 Plan 08: Demo User Seed Fix and Standalone ASB Fallback Summary

Fixed two independent backend startup gaps: silent seed failure (Gap 5) and null ASB connection string crash (Gap 2).

## What Was Built

**Task 1 — Identity demo user seed fix:**
- `DbInitializer.cs`: Updated seed calls to use `Demo123!` / `Admin123!` passwords. Modified `SeedUserIfNotExists` to capture `IdentityResult` from `CreateAsync` and throw `InvalidOperationException` with descriptive error when `result.Succeeded` is false.
- `Program.cs`: Added `options.Password.RequireUppercase = false` and `options.Password.RequireNonAlphanumeric = false` inside the `AddIdentity` lambda. Passwords `Demo123!` and `Admin123!` satisfy remaining defaults (`RequireDigit=true` via "1", `RequiredLength=6`).

**Task 2 — Standalone ASB fallback connection string:**
- Created `appsettings.Development.json` in both `ECommerce.Catalog.API` and `ECommerce.Notifications.API` with the Azure Service Bus emulator connection string (`UseDevelopmentEmulator=true`). When Aspire is running, it overrides this value via environment variable injection. When running standalone in Development, MassTransit connects to the local emulator instead of receiving a null host argument that causes `ArgumentNullException`.

## Commits

| Task | Commit | Description |
|------|--------|-------------|
| Task 1 | dbe7057 | fix(02-08): fix Identity demo user seed and relax password complexity |
| Task 2 | bd32b1e | fix(02-08): add fallback ASB emulator connection string for standalone startup |

## Verification Results

All three services build with 0 errors:
- `ECommerce.Identity.API` — Build succeeded, 0 errors
- `ECommerce.Catalog.API` — Build succeeded, 0 errors
- `ECommerce.Notifications.API` — Build succeeded, 0 errors

Acceptance criteria confirmed:
- `DbInitializer.cs` contains `result.Succeeded` check and `throw new InvalidOperationException(`
- Seed calls use `Demo123!` and `Admin123!`
- `Program.cs` AddIdentity block contains `RequireUppercase = false` and `RequireNonAlphanumeric = false`
- Both `appsettings.Development.json` files contain `ConnectionStrings.messaging` with `UseDevelopmentEmulator=true`

## Deviations from Plan

None - plan executed exactly as written.

## Threat Model Compliance

| Threat ID | Disposition | Applied |
|-----------|-------------|---------|
| T-02-08-01 | accept | RequireUppercase/RequireNonAlphanumeric relaxed; passwords Demo123!/Admin123! meet remaining defaults |
| T-02-08-02 | accept | appsettings.Development.json uses standard emulator SAS key (public knowledge); no production secrets |
| T-02-08-03 | accept | Startup exception on seed failure is intentional — visible and actionable vs. silent discard |

## Known Stubs

None — both fixes wire real behavior (password policy enforcement, real emulator connection string).

## Self-Check: PASSED

- `src/services/identity/ECommerce.Identity.API/Data/DbInitializer.cs` — modified, confirmed committed in dbe7057
- `src/services/identity/ECommerce.Identity.API/Program.cs` — modified, confirmed committed in dbe7057
- `src/services/catalog/ECommerce.Catalog.API/appsettings.Development.json` — created, confirmed committed in bd32b1e
- `src/services/notifications/ECommerce.Notifications.API/appsettings.Development.json` — created, confirmed committed in bd32b1e
