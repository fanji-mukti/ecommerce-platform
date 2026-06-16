<!-- GSD:project-start source:PROJECT.md -->
## Project

**ECommerce Platform**

An event-driven e-commerce platform built as a portfolio and learning project. It demonstrates microservices architecture using .NET 10 and Azure Service Bus, with DDD, Saga/process manager patterns, and eventual consistency across eight independent services. Deployed locally via Docker Compose and to Azure via Terraform-managed infrastructure.

**Core Value:** A working checkout saga that spans Catalog, Cart, Orders, Payments, Fulfillment, and Notifications — demonstrating event-driven coordination between microservices without direct coupling.

### Constraints

- **Tech stack**: .NET 10, Azure Service Bus, Angular, Terraform, Docker Compose
- **Payments**: Simulated only — no real payment provider credentials needed
- **Deployment**: Local (Docker Compose) + Azure (Container Apps via Terraform)
- **ADR format**: MADR (Markdown Architectural Decision Records) — one file per decision in `docs/adr/`
<!-- GSD:project-end -->

<!-- GSD:stack-start source:research/STACK.md -->
## Technology Stack

## Verification Constraints
## Recommended Stack — At a Glance
| Layer | Choice | Confidence |
|-------|--------|------------|
| Runtime | .NET 10 (LTS) | HIGH (already pinned) |
| Web/API framework | ASP.NET Core Minimal APIs + OpenAPI (`Microsoft.AspNetCore.OpenApi`) | HIGH |
| Messaging abstraction | **MassTransit 8.x** with Azure Service Bus transport | HIGH |
| Saga / process manager | MassTransit Automatonymous-style state machines, ASB-persisted | HIGH |
| ORM (write side) | EF Core 10 + Npgsql provider (PostgreSQL) | HIGH |
| Read-model store (CQRS) | PostgreSQL projections via EF Core; optional Redis cache | MEDIUM |
| In-process mediator | **MediatR 12.x** (or built-in handlers — see notes) | MEDIUM |
| Validation | FluentValidation 11.x | HIGH |
| Mapping | Mapperly (source-generated) — **not** AutoMapper | MEDIUM |
| Logging | Serilog + Serilog.Sinks.OpenTelemetry | HIGH |
| Observability | OpenTelemetry SDK + Azure Monitor exporter | HIGH |
| Resilience | `Microsoft.Extensions.Resilience` / Polly v8 | HIGH |
| Local orchestration | **.NET Aspire 10.x** AppHost + Docker Compose export | MEDIUM |
| Container runtime | Distroless `mcr.microsoft.com/dotnet/aspnet:10.0-noble-chiseled` | HIGH |
| Identity | ASP.NET Core Identity + **OpenIddict 7.5.0** (self-hosted OIDC) | MEDIUM |
| API gateway (optional) | YARP 2.x reverse proxy | MEDIUM |
| Testing | xUnit v3 + Testcontainers-for-.NET + WireMock.Net | HIGH |
| Frontend | **Angular 20** (zoneless, standalone, signals) | MEDIUM |
| Angular state | Signals + `@angular/router` resolvers; NgRx Signal Store if needed | MEDIUM |
| IaC | Terraform 1.10+ with **AzureRM provider 4.x** | HIGH |
| CI | GitHub Actions | HIGH |
## Detailed Recommendations
### Core Framework
| Technology | Version | Purpose | Why |
|------------|---------|---------|-----|
| .NET | 10.0 (LTS) | Service runtime | Already pinned in `Contracts.csproj`. .NET 10 is the active LTS as of Nov 2025; supports native AOT for trim-able services, improved Minimal APIs, `RequestDelegateGenerator` source generation. |
| ASP.NET Core | 10.0 | HTTP host, Minimal APIs | Minimal APIs are the idiomatic choice for microservices in 2026 — less ceremony than MVC, source-generated routing, full OpenAPI support via `Microsoft.AspNetCore.OpenApi` (replaces Swashbuckle as default). |
| C# | 13 (matches .NET 10) | Language | Field-backed properties, params collections, escape sequences — keeps domain code expressive. |
### Messaging — The Critical Choice
| Library | Purpose | Notes |
|---------|---------|-------|
| `MassTransit` | Core abstraction, consumers, pipeline | v8 line, .NET 8+ compatible (works on .NET 10) |
| `MassTransit.Azure.ServiceBus.Core` | ASB transport | Wraps `Azure.Messaging.ServiceBus` SDK; handles topology (topics/subscriptions) from contract types |
| `MassTransit.EntityFrameworkCore` | Saga persistence + Outbox | Critical — provides transactional outbox |
| Option | Why not |
|--------|---------|
| **Raw `Azure.Messaging.ServiceBus` SDK** | No saga support, no outbox, manual topology, manual serialization, manual retry. Re-implements MassTransit poorly. Use only inside MassTransit's transport — never directly. |
| **NServiceBus + ASB transport** | Excellent product but commercial license (Particular Platform). For a portfolio project demonstrating breadth, license friction is a blocker; OSS MassTransit removes that. Technically equivalent for this scope. |
| **CAP (DotNetCore.CAP)** | Good outbox-first library, but smaller ecosystem, weaker saga story, ASB support less mature than RabbitMQ/Kafka. |
| **Brighter / Darker** | Niche; smaller community; saga story less polished than MassTransit. |
| **Wolverine** | Promising (Jeremy Miller, ex-MassTransit contributor), full-featured, but smaller community in 2026. Worth knowing about; MassTransit is the safer portfolio choice because it appears in more .NET job listings. |
### Persistence
| Technology | Version | Purpose | Why |
|------------|---------|---------|-----|
| EF Core | 10.0 | Write-side ORM | Ships with .NET 10; supports complex types, JSON columns, `ExecuteUpdate`/`ExecuteDelete` bulk ops, compiled models. |
| Npgsql.EntityFrameworkCore.PostgreSQL | 10.x | PostgreSQL provider | PostgreSQL > SQL Server for portfolio — free, cross-platform, runs in Docker Compose painlessly, Azure has managed Flexible Server. |
| `Microsoft.EntityFrameworkCore.Design` | 10.0 | `dotnet ef` migrations | Standard. |
| Dapper | 2.1.x | Read-side queries (CQRS) | Use for hand-tuned read projections; EF Core for writes. Demonstrates CQRS separation cleanly. |
| Option | Why not |
|--------|---------|
| SQL Server / Azure SQL | Higher cost in Azure, less common in portfolio-grade microservice writeups, heavier Docker image. |
| Cosmos DB everywhere | Cost trap for portfolio. Use only if a specific service (e.g., Catalog with global-read) justifies it — and call that out as an ADR. |
| Marten (event store on Postgres) | Tempting for event sourcing, but PROJECT.md scopes to "eventual consistency via ASB", not event sourcing. Adds learning surface without scope justification. |
| MongoDB | No clear advantage; relational fits ordering/cart/payments domain. |
### Mediator / In-Process Dispatch
- Use MediatR 12.x now (HIGH confidence on the architectural pattern, MEDIUM on the library specifically given license uncertainty).
- Document the licensing risk in an ADR.
- Acceptable alternatives if you want to avoid the risk entirely:
### Validation & Mapping
| Library | Version | Purpose | Why |
|---------|---------|---------|-----|
| FluentValidation | 11.x | Request / command validation | Industry standard. Use `AddValidatorsFromAssembly` + a Minimal API filter to run validators automatically. |
| Mapperly | 4.x | DTO ↔ domain mapping | Source-generated, AOT-friendly, **no reflection**. Compile-time errors when properties drift. |
### Identity & Auth
| Library | Purpose |
|---------|---------|
| `Microsoft.AspNetCore.Identity.EntityFrameworkCore` | User/role storage |
| `OpenIddict.AspNetCore` + `OpenIddict.EntityFrameworkCore` | OIDC token issuance (auth code + PKCE for Angular SPA, client credentials for service-to-service) |
| `Microsoft.AspNetCore.Authentication.JwtBearer` | Token validation on every protected API |
| Option | Verdict |
|--------|---------|
| **OpenIddict** | Recommended. OSS, MIT, mature, well-documented, fully self-hosted. Demonstrates OIDC understanding. |
| **Duende IdentityServer** | Commercial license required above a small revenue threshold. Adds friction for a portfolio. |
| **Azure AD B2C / Entra External ID** | Real-world choice but obscures the auth mechanics; portfolio loses the "I built it" narrative. Mention as production alternative in ADR. |
| **Keycloak** | Java-based, off-stack. Container adds bulk. |
### Observability
| Component | Library | Notes |
|-----------|---------|-------|
| Logs | Serilog + `Serilog.Sinks.OpenTelemetry` + `Serilog.Sinks.Console` | Structured logs; OTEL sink exports to Aspire dashboard locally and Azure Monitor in cloud. |
| Traces / Metrics | `OpenTelemetry.Extensions.Hosting` + `OpenTelemetry.Instrumentation.AspNetCore` + `OpenTelemetry.Instrumentation.Http` + `MassTransit.OpenTelemetry` | MassTransit auto-instruments consumer spans — critical for tracing the saga across services. |
| Exporter (cloud) | `Azure.Monitor.OpenTelemetry.AspNetCore` | One-line wiring for Application Insights. |
| Exporter (local) | OTLP → Aspire dashboard | Aspire AppHost wires this automatically. |
### Resilience
### Local Orchestration
- Use the **Aspire AppHost** as the local dev launcher (one `dotnet run` brings up all services, Postgres, ASB emulator, OTEL collector).
- Use `aspire publish` (or the Aspire Docker Compose publisher) to **generate** `docker-compose.yml` from the AppHost model.
- Commit both: `eshop.AppHost/` (developer experience) and `docker-compose.yml` (artefact required by PROJECT.md).
### Containers & Deployment
| Concern | Choice |
|---------|--------|
| Base image | `mcr.microsoft.com/dotnet/aspnet:10.0-noble-chiseled` (distroless-style, ~30 MB) |
| SDK image (build stage) | `mcr.microsoft.com/dotnet/sdk:10.0` |
| Multi-stage builds | Mandatory — restore, publish, then copy to runtime stage |
| Native AOT | Optional per service; payments/notifications are good candidates (small, no EF Core) |
| Azure runtime | Azure Container Apps (per PROJECT.md) — KEDA-based ASB scaler is the killer feature: services scale from 0 based on queue depth |
### Frontend (Angular)
| Technology | Version | Purpose | Why |
|------------|---------|---------|-----|
| Angular | **20.x** | SPA framework | Angular 20 (released ~mid-2025) is the current stable line; zoneless change detection is stable; standalone components / signals are the idiomatic default. Angular 21 may be available — verify before pinning. |
| TypeScript | 5.6+ | Language | Matches Angular 20 peer dep range. |
| State | Signals (built-in) | Reactive state | Use signals + computed for component state; only reach for NgRx Signal Store if a clear cross-cutting state need emerges. |
| HTTP | `provideHttpClient(withFetch())` | Backend calls | Fetch API mode is the modern default. |
| OIDC client | `angular-auth-oidc-client` 19.x | Auth | PKCE flow against OpenIddict. Maintained, ergonomic, supports silent renew. |
| UI components | **Angular Material 20** OR **PrimeNG 18** | Component library | Material if you want minimal effort; PrimeNG if you want richer data tables / dashboards. Either is fine for a portfolio. |
| Testing | Vitest (via `@analogjs/vitest-angular`) or Karma+Jasmine | Unit tests | Vitest is faster but newer in Angular; Karma is the default but being phased out. Pick Vitest for a modern signal. |
| E2E | Playwright | E2E | Cypress is also valid; Playwright is the trajectory winner. |
### Infrastructure as Code
| Tool | Version | Purpose |
|------|---------|---------|
| Terraform | 1.10+ | IaC engine |
| `hashicorp/azurerm` | **4.x** | Azure resource provider |
| `hashicorp/azuread` | 3.x | App registrations, service principals |
| `hashicorp/random` | 3.x | Suffixes for globally unique names |
| (Optional) `Azure/azapi` | 2.x | Preview / not-yet-in-AzureRM resources |
| Module | Resources |
|--------|-----------|
| `network` | VNet, subnets, NSGs, private endpoints |
| `service-bus` | Namespace, topics, subscriptions, SAS / managed identity auth |
| `container-apps` | Container Apps Environment, Log Analytics workspace |
| `container-app` | Single ACA app (used N times — one per microservice) |
| `postgres` | Flexible Server, databases, firewall rules, key vault secret |
| `identity` | User-assigned managed identities + role assignments |
| `frontdoor` | (Optional) Azure Front Door for the Angular SPA |
### Testing Stack
| Layer | Tooling |
|-------|---------|
| Unit | xUnit v3, FluentAssertions, NSubstitute |
| Integration (per service) | `Microsoft.AspNetCore.Mvc.Testing` + Testcontainers (Postgres) + MassTransit in-memory transport |
| Contract | Verify.Xunit snapshots of ASB message contracts (compile-time + snapshot guard against breaking changes) |
| End-to-end saga | Testcontainers (Postgres + ASB emulator) + Aspire `DistributedApplicationTestingBuilder` |
| Load | k6 |
## Installation Sketch
# infra/environments/dev/versions.tf
## What NOT to Use (and Why)
| Avoid | Reason | Use Instead |
|-------|--------|-------------|
| Raw `Azure.Messaging.ServiceBus` SDK as the messaging surface | No saga, no outbox, no test harness, manual topology | MassTransit |
| AutoMapper | License trajectory + reflection overhead | Mapperly (source-generated) |
| Swashbuckle/Swagger directly | Replaced as ASP.NET Core default by `Microsoft.AspNetCore.OpenApi` in .NET 9/10 | `Microsoft.AspNetCore.OpenApi` + Scalar or built-in UI |
| Newtonsoft.Json | `System.Text.Json` is faster, source-gen friendly, AOT-compatible | `System.Text.Json` |
| MVC Controllers for service APIs | Ceremony without payoff at this scale | Minimal APIs with endpoint groups |
| NgModules / Zone.js in Angular | Legacy patterns; signals + zoneless are the modern default | Standalone components, signals, `provideZonelessChangeDetection()` |
| Karma (Angular test runner) | Being phased out | Vitest via `@analogjs/vitest-angular` |
| Bicep for IaC | PROJECT.md chose Terraform | Terraform AzureRM provider 4.x |
| Cosmos DB by default | Cost-trap for a portfolio | PostgreSQL Flexible Server |
| SQL Server / Azure SQL | Cost + Docker bulk | PostgreSQL |
| Duende IdentityServer (commercial) | License friction for portfolio | OpenIddict |
| NServiceBus (commercial) | License friction for portfolio | MassTransit |
| Eventuous / EventStoreDB | Out of scope — event sourcing isn't a PROJECT.md requirement | EF Core + outbox pattern |
| In-process direct service calls between microservices | Defeats the event-driven premise | ASB messages, period (except gateway → service synchronous reads) |
## Alternatives Considered (Summary Matrix)
| Category | Recommended | Strong Alternative | Why Not the Alternative |
|----------|-------------|--------------------|-------------------------|
| Messaging | MassTransit | NServiceBus | Commercial license |
| Messaging | MassTransit | Wolverine | Smaller community; less job-market signal |
| ORM | EF Core 10 | Dapper (writes too) | Loses migrations + change-tracking; only worth it for ultra-perf paths |
| Mediator | MediatR 12 | Mediator.SourceGenerator | Smaller ecosystem; MediatR pattern dominates job listings — but document license risk |
| Mediator | MediatR 12 | No mediator (direct handler classes) | Equally valid; pick if you want zero license risk |
| Mapping | Mapperly | AutoMapper | License trajectory + slower at runtime |
| Identity | OpenIddict | Duende IdentityServer | Commercial above small revenue threshold |
| DB | PostgreSQL | SQL Server | Cost + Docker weight |
| Local orchestration | Aspire + Compose | Compose only | Aspire wins on DX; Compose alone still works |
| IaC | Terraform AzureRM | Bicep | PROJECT.md chose Terraform deliberately |
| Frontend state | Signals | NgRx Signal Store | Only adopt when cross-cutting state demands it |
| Frontend tests | Vitest | Karma+Jasmine | Karma is being phased out |
| Container base | `aspnet:10.0-noble-chiseled` | `aspnet:10.0-alpine` | Chiseled is smaller and Microsoft-supported |
## Pre-Pin Verification Checklist
- [ ] `MassTransit` and `MassTransit.Azure.ServiceBus.Core` — latest 8.x minor on nuget.org; check release notes for any breaking changes since v8.0
- [ ] EF Core 10 + Npgsql 10 — confirm Npgsql 10 GA is shipped (it usually trails EF Core GA by 1–4 weeks)
- [ ] `Microsoft.AspNetCore.OpenApi` — confirm it's still the default OpenAPI generator in .NET 10
- [ ] MediatR — check current license terms; if v13 has shipped with commercial terms, lock to v12.x or switch to Mediator.SourceGenerator and update this doc
- [ ] AutoMapper — confirm license stance before assuming anyone still wants it (same reason)
- [ ] OpenIddict 7.5.0 — confirmed on NuGet (verified in Phase 2 research); .NET 10 support verified
- [ ] Angular — confirm whether Angular 20 or 21 is current; both follow the same architectural recommendations here
- [ ] Terraform AzureRM 4.x — confirm latest 4.x minor; check changelog for Container Apps + Service Bus resource changes
- [ ] Azure Service Bus Emulator container — confirm latest tag and feature parity gaps with cloud ASB (sessions, transactions)
- [ ] `mcr.microsoft.com/dotnet/aspnet:10.0-noble-chiseled` tag exists (vs `8.0-jammy-chiseled` etc.)
## Sources
- The existing repo state (`src/building-blocks/Contracts/Contracts.csproj` pinning `net10.0`)
- PROJECT.md constraints (.NET 10, Azure Service Bus, Angular, Terraform, Docker Compose, Azure Container Apps)
- Well-established .NET / Azure ecosystem patterns through early 2026 (training data)
- Microsoft reference architectures (eShop, eShopOnContainers lineage) referenced in PROJECT.md
<!-- GSD:stack-end -->

<!-- GSD:conventions-start source:CONVENTIONS.md -->
## Conventions

Conventions not yet established. Will populate as patterns emerge during development.
<!-- GSD:conventions-end -->

<!-- GSD:architecture-start source:ARCHITECTURE.md -->
## Architecture

Architecture not yet mapped. Follow existing patterns found in the codebase.
<!-- GSD:architecture-end -->

<!-- GSD:skills-start source:skills/ -->
## Project Skills

No project skills found. Add skills to any of: `.claude/skills/`, `.agents/skills/`, `.cursor/skills/`, `.github/skills/`, or `.codex/skills/` with a `SKILL.md` index file.
<!-- GSD:skills-end -->

<!-- GSD:workflow-start source:GSD defaults -->
## GSD Workflow Enforcement

Before using Edit, Write, or other file-changing tools, start work through a GSD command so planning artifacts and execution context stay in sync.

Use these entry points:
- `/gsd-quick` for small fixes, doc updates, and ad-hoc tasks
- `/gsd-debug` for investigation and bug fixing
- `/gsd-execute-phase` for planned phase work

Do not make direct repo edits outside a GSD workflow unless the user explicitly asks to bypass it.
<!-- GSD:workflow-end -->



<!-- GSD:profile-start -->
## Developer Profile

> Profile not yet configured. Run `/gsd-profile-user` to generate your developer profile.
> This section is managed by `generate-claude-profile` -- do not edit manually.
<!-- GSD:profile-end -->
