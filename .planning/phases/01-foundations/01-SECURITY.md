---
phase: 01
slug: foundations
status: verified
threats_open: 0
asvs_level: 1
created: 2026-06-15
---

# Phase 01 — Security

> Per-phase security contract: threat register, accepted risks, and audit trail.

---

## Trust Boundaries

| Boundary | Description | Data Crossing |
|----------|-------------|---------------|
| Developer workstation → GitHub | Source code pushed via git | .csproj files, Program.cs, ci.yml — no secrets |
| GitHub Actions runner → NuGet | Package restore at CI time | Pinned NuGet packages (exact versions); no floating references |
| Service container → OTLP collector | Telemetry export at runtime | Endpoint injected via `OTEL_EXPORTER_OTLP_ENDPOINT` env var; never hardcoded |
| Developer workstation → Docker | Local orchestration via Aspire | No secrets in committed files; `.env` excluded by `.gitignore` |

---

## Threat Register

| Threat ID | Category | Component | Disposition | Mitigation | Status |
|-----------|----------|-----------|-------------|------------|--------|
| T-01-01 | Tampering | Contracts.csproj | mitigate | Zero `PackageReference` elements confirmed — file has only `PropertyGroup` with `TargetFramework`, `ImplicitUsings`, `Nullable` | CLOSED |
| T-01-02 | Information Disclosure | Class1.cs deletion | accept | Class1.cs contained no sensitive data; accepted risk documented in Accepted Risks Log | CLOSED |
| T-01-SC | Tampering | npm/pip/cargo installs | accept | Phase 1 Contracts task performed zero package manager installs; accepted risk documented | CLOSED |
| T-02-01 | Information Disclosure | Program.cs OTLP endpoint | mitigate | All 8 services use `AddOtlpExporter()` with no explicit endpoint argument — delegates to `OTEL_EXPORTER_OTLP_ENDPOINT` env var per OTEL SDK spec. No hardcoded URL, `localhost`, or IP found in any `src/services/**/Program.cs` | CLOSED |
| T-02-02 | Information Disclosure | appsettings.json secrets | mitigate | Zero `appsettings*.json` files found under `src/services/` (glob returned no matches). No `ConnectionString`, `Password`, `AccountKey`, or `SharedAccessKey` present in any service source file | CLOSED |
| T-02-03 | Tampering | NuGet package versions | mitigate | All 8 service `.csproj` files use exact version strings (no `*` wildcards). Versions verified: `Microsoft.AspNetCore.OpenApi@10.0.8`, `Serilog.AspNetCore@10.0.0`, `Serilog.Sinks.OpenTelemetry@4.2.0`, `OpenTelemetry.Extensions.Hosting@1.15.3`, `OpenTelemetry.Instrumentation.AspNetCore@1.15.2`, `OpenTelemetry.Exporter.OpenTelemetryProtocol@1.15.3` | CLOSED |
| T-02-SC | Tampering | NuGet package installs | accept | All packages are Microsoft and Serilog org publishers (ASSUMED-APPROVED per RESEARCH.md); accepted risk documented | CLOSED |
| T-03-01 | Information Disclosure | docker-compose.yml + .env | mitigate | `.gitignore` line 7 contains `.env` entry, confirmed present at repo root | CLOSED |
| T-03-02 | Information Disclosure | AppHost/Program.cs | accept | AppHost Program.cs contains no secrets — all connection strings are Aspire-managed; accepted risk documented | CLOSED |
| T-03-03 | Denial of Service | ASB emulator :latest tag | accept | Tag is "latest" by design for Phase 1 dev; pinning deferred to Phase 2+; accepted risk documented | CLOSED |
| T-03-SC | Tampering | Aspire.Hosting.* packages | accept | Microsoft first-party packages (ASSUMED-APPROVED); accepted risk documented | CLOSED |
| T-04-01 | Information Disclosure | ci.yml secrets | mitigate | Grep for `secrets.` in `.github/workflows/ci.yml` returned zero matches; no secret references present | CLOSED |
| T-04-02 | Tampering | Third-party GitHub Actions | mitigate | Only `actions/checkout@v4` and `actions/setup-dotnet@v4` present in `ci.yml` (lines 28, 30); both are GitHub first-party actions; no community actions found | CLOSED |
| T-04-03 | Elevation of Privilege | dotnet test in CI | accept | Phase 1 has no test projects; tests run in GitHub-hosted runner sandbox; accepted risk documented | CLOSED |
| T-04-SC | Tampering | NuGet restore in CI | accept | NuGet packages restored with pinned versions from .csproj files (T-02-03 verified exact versions); accepted risk documented | CLOSED |
| T-05-01 | Information Disclosure | ADR content | accept | ADRs are public architectural documentation with no sensitive data; accepted risk documented | CLOSED |
| T-05-02 | Tampering | ADR-0006 MassTransit version pin | mitigate | `docs/adr/0006-masstransit-outbox-inbox.md` explicitly documents MassTransit 8.3.6 pin in decision title, Decision Outcome section, and Consequences with critical warning against installing without `--version 8.3.6` | CLOSED |
| T-05-SC | Tampering | npm/pip/cargo installs in ADR task | accept | ADR files are markdown — no package installs; accepted risk documented | CLOSED |

---

## Accepted Risks Log

| Risk ID | Threat Ref | Rationale | Accepted By | Date |
|---------|------------|-----------|-------------|------|
| AR-01 | T-01-02 | Class1.cs contained no sensitive data; file was scaffolding boilerplate with no secrets, credentials, or PII | Fanji Ari Mukti | 2026-06-15 |
| AR-02 | T-01-SC | Phase 1 Contracts task performed zero package manager installs (npm/pip/cargo) — no third-party supply chain exposure | Fanji Ari Mukti | 2026-06-15 |
| AR-03 | T-02-SC | NuGet packages are from Microsoft and Serilog publishers; all are well-known OSS packages with no known malicious history; ASSUMED-APPROVED per RESEARCH.md | Fanji Ari Mukti | 2026-06-15 |
| AR-04 | T-03-02 | AppHost/Program.cs uses Aspire resource APIs only; no secrets, connection strings, or credentials are hardcoded in committed source | Fanji Ari Mukti | 2026-06-15 |
| AR-05 | T-03-03 | ASB emulator uses `:latest` tag by design for Phase 1 local development; a floating tag introduces a DoS risk via breaking changes but is acceptable at this stage; tag pinning is scheduled for Phase 2 | Fanji Ari Mukti | 2026-06-15 |
| AR-06 | T-03-SC | Aspire.Hosting.* packages are Microsoft first-party; ASSUMED-APPROVED; no community packages used in AppHost | Fanji Ari Mukti | 2026-06-15 |
| AR-07 | T-04-03 | Phase 1 has no test projects; `dotnet test` runs in GitHub-hosted ephemeral Ubuntu runner sandbox with no persistent state or elevated privileges | Fanji Ari Mukti | 2026-06-15 |
| AR-08 | T-04-SC | CI NuGet restore uses exact version strings from .csproj files (verified under T-02-03); no floating version resolution occurs at restore time | Fanji Ari Mukti | 2026-06-15 |
| AR-09 | T-05-01 | ADR files contain only architectural decisions with no secrets, credentials, environment-specific values, or PII | Fanji Ari Mukti | 2026-06-15 |
| AR-10 | T-05-SC | ADR task produced only markdown files; no package manager was invoked; zero supply chain exposure | Fanji Ari Mukti | 2026-06-15 |

---

## Unregistered Threat Flags

| Flag | Source | Assessment |
|------|--------|------------|
| /health endpoint unauthenticated by design | Plan 01-02 SUMMARY | Maps to T-02-01 (accepted scope: Phase 1 stubs; no PII exposed on health endpoint). No new threat ID required. |

---

## Security Audit Trail

| Audit Date | Threats Total | Closed | Open | Run By |
|------------|---------------|--------|------|--------|
| 2026-06-15 | 18 | 18 | 0 | gsd-security-auditor |

---

## Sign-Off

- [x] All threats have a disposition (mitigate / accept / transfer)
- [x] Accepted risks documented in Accepted Risks Log
- [x] `threats_open: 0` confirmed
- [x] `status: verified` set in frontmatter

**Approval:** verified 2026-06-15
