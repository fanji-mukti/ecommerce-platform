# Technology Stack

**Project:** ECommerce Platform (event-driven .NET microservices on Azure)
**Researched:** 2026-05-30
**Overall confidence:** MEDIUM (see "Verification Constraints" below)

## Verification Constraints

Web verification tools (WebSearch, WebFetch, Context7 CLI, package CLIs) were unavailable in this research session, so version numbers below are stated from training data and ecosystem patterns rather than freshly verified against nuget.org / npm / registry.terraform.io. **Before pinning versions in `.csproj` / `package.json` / `versions.tf`, run a quick verification pass** (see "Pre-Pin Verification Checklist" at the bottom of this file). Architectural choices and rationale do not depend on patch-level versions and are stated with higher confidence.

The existing `src/building-blocks/Contracts/Contracts.csproj` already targets `net10.0`, which anchors the runtime.

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
| Identity | ASP.NET Core Identity + **OpenIddict 6.x** (self-hosted OIDC) | MEDIUM |
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

**Confidence: HIGH** (architectural choice); MEDIUM on the specific recommendation to default to Minimal APIs over Controllers — both are supported; Minimal APIs win on ceremony for service boundaries this small.

### Messaging — The Critical Choice

**Choose MassTransit 8.x with `MassTransit.Azure.ServiceBus.Core`.**

| Library | Purpose | Notes |
|---------|---------|-------|
| `MassTransit` | Core abstraction, consumers, pipeline | v8 line, .NET 8+ compatible (works on .NET 10) |
| `MassTransit.Azure.ServiceBus.Core` | ASB transport | Wraps `Azure.Messaging.ServiceBus` SDK; handles topology (topics/subscriptions) from contract types |
| `MassTransit.EntityFrameworkCore` | Saga persistence + Outbox | Critical — provides transactional outbox |

**Rationale (HIGH confidence):**

1. **Topology automation** — MassTransit creates ASB topics and subscriptions from your message contract types automatically. Without it you write hundreds of lines of `ServiceBusAdministrationClient` topology code per service.
2. **Saga state machines** — first-class `MassTransitStateMachine<TState>` with ASB-backed persistence. Maps directly to the "Saga/process manager" requirement in PROJECT.md.
3. **Transactional outbox** — `AddEntityFrameworkOutbox<TDbContext>()` solves the dual-write problem (DB commit + ASB publish in one logical transaction) which is the #1 pitfall of event-driven systems. The raw ASB SDK gives you nothing here.
4. **Test harness** — `MassTransit.Testing` provides in-memory transport for unit tests; production code is transport-agnostic.
5. **Retry / redelivery / scheduled messages** — uniform API across consumers; ASB-specific quirks (lock duration, dead-letter, scheduled enqueue) abstracted behind familiar middleware.

**Alternatives rejected:**

| Option | Why not |
|--------|---------|
| **Raw `Azure.Messaging.ServiceBus` SDK** | No saga support, no outbox, manual topology, manual serialization, manual retry. Re-implements MassTransit poorly. Use only inside MassTransit's transport — never directly. |
| **NServiceBus + ASB transport** | Excellent product but commercial license (Particular Platform). For a portfolio project demonstrating breadth, license friction is a blocker; OSS MassTransit removes that. Technically equivalent for this scope. |
| **CAP (DotNetCore.CAP)** | Good outbox-first library, but smaller ecosystem, weaker saga story, ASB support less mature than RabbitMQ/Kafka. |
| **Brighter / Darker** | Niche; smaller community; saga story less polished than MassTransit. |
| **Wolverine** | Promising (Jeremy Miller, ex-MassTransit contributor), full-featured, but smaller community in 2026. Worth knowing about; MassTransit is the safer portfolio choice because it appears in more .NET job listings. |

**Confidence: HIGH.** MassTransit is the de-facto OSS choice for ASB-backed .NET microservices; this aligns with eShopOnContainers (which used MassTransit-style abstractions) and most Azure reference architectures.

### Persistence

| Technology | Version | Purpose | Why |
|------------|---------|---------|-----|
| EF Core | 10.0 | Write-side ORM | Ships with .NET 10; supports complex types, JSON columns, `ExecuteUpdate`/`ExecuteDelete` bulk ops, compiled models. |
| Npgsql.EntityFrameworkCore.PostgreSQL | 10.x | PostgreSQL provider | PostgreSQL > SQL Server for portfolio — free, cross-platform, runs in Docker Compose painlessly, Azure has managed Flexible Server. |
| `Microsoft.EntityFrameworkCore.Design` | 10.0 | `dotnet ef` migrations | Standard. |
| Dapper | 2.1.x | Read-side queries (CQRS) | Use for hand-tuned read projections; EF Core for writes. Demonstrates CQRS separation cleanly. |

**Database-per-service.** Each service owns its schema in a logically (preferably physically) separate database. In Docker Compose: one Postgres container with `POSTGRES_MULTIPLE_DATABASES` init script. In Azure: separate databases on a shared Flexible Server (cost-aware) or per-service servers (production-true).

**Alternatives rejected:**

| Option | Why not |
|--------|---------|
| SQL Server / Azure SQL | Higher cost in Azure, less common in portfolio-grade microservice writeups, heavier Docker image. |
| Cosmos DB everywhere | Cost trap for portfolio. Use only if a specific service (e.g., Catalog with global-read) justifies it — and call that out as an ADR. |
| Marten (event store on Postgres) | Tempting for event sourcing, but PROJECT.md scopes to "eventual consistency via ASB", not event sourcing. Adds learning surface without scope justification. |
| MongoDB | No clear advantage; relational fits ordering/cart/payments domain. |

**Confidence: HIGH** on EF Core + PostgreSQL; MEDIUM on Dapper for reads (also valid: separate EF Core read DbContext with `AsNoTracking()`).

### Mediator / In-Process Dispatch

**Recommend: MediatR 12.x** for request handlers and in-process domain event dispatch within a service.

**Caveat worth noting in an ADR:** MediatR's maintainer (Jimmy Bogard) announced in 2024-2025 that future major versions of MediatR (and AutoMapper) would move to a commercial license model. As of early 2026 the v12 line remains under its existing license. For this portfolio project:

- Use MediatR 12.x now (HIGH confidence on the architectural pattern, MEDIUM on the library specifically given license uncertainty).
- Document the licensing risk in an ADR.
- Acceptable alternatives if you want to avoid the risk entirely:
  - **No mediator** — Minimal API endpoints call handler classes directly via DI. For services with <30 endpoints this is honestly cleaner.
  - **Mediator.SourceGenerator** (martinothamar/Mediator) — source-generated, MIT, faster than MediatR.

**Confidence: MEDIUM.**

### Validation & Mapping

| Library | Version | Purpose | Why |
|---------|---------|---------|-----|
| FluentValidation | 11.x | Request / command validation | Industry standard. Use `AddValidatorsFromAssembly` + a Minimal API filter to run validators automatically. |
| Mapperly | 4.x | DTO ↔ domain mapping | Source-generated, AOT-friendly, **no reflection**. Compile-time errors when properties drift. |

**Do NOT use AutoMapper** for new code in 2026 — same licensing trajectory as MediatR, and Mapperly is strictly better technically (no runtime overhead, compile-time safety, AOT-compatible).

**Confidence: HIGH** on FluentValidation; **MEDIUM-HIGH** on Mapperly (clear technical win, but it's a newer choice than AutoMapper for many devs — call it out in an ADR).

### Identity & Auth

**Recommend: ASP.NET Core Identity (user store) + OpenIddict 6.x (OIDC/OAuth2 server) hosted inside the Identity service.**

| Library | Purpose |
|---------|---------|
| `Microsoft.AspNetCore.Identity.EntityFrameworkCore` | User/role storage |
| `OpenIddict.AspNetCore` + `OpenIddict.EntityFrameworkCore` | OIDC token issuance (auth code + PKCE for Angular SPA, client credentials for service-to-service) |
| `Microsoft.AspNetCore.Authentication.JwtBearer` | Token validation on every protected API |

**Why OpenIddict over alternatives:**

| Option | Verdict |
|--------|---------|
| **OpenIddict** | Recommended. OSS, MIT, mature, well-documented, fully self-hosted. Demonstrates OIDC understanding. |
| **Duende IdentityServer** | Commercial license required above a small revenue threshold. Adds friction for a portfolio. |
| **Azure AD B2C / Entra External ID** | Real-world choice but obscures the auth mechanics; portfolio loses the "I built it" narrative. Mention as production alternative in ADR. |
| **Keycloak** | Java-based, off-stack. Container adds bulk. |

**Confidence: MEDIUM.** The architectural pattern (OIDC + JWT validation in each service) is HIGH confidence; the specific choice of OpenIddict is opinionated — Duende is technically excellent and the dev license is free.

### Observability

| Component | Library | Notes |
|-----------|---------|-------|
| Logs | Serilog + `Serilog.Sinks.OpenTelemetry` + `Serilog.Sinks.Console` | Structured logs; OTEL sink exports to Aspire dashboard locally and Azure Monitor in cloud. |
| Traces / Metrics | `OpenTelemetry.Extensions.Hosting` + `OpenTelemetry.Instrumentation.AspNetCore` + `OpenTelemetry.Instrumentation.Http` + `MassTransit.OpenTelemetry` | MassTransit auto-instruments consumer spans — critical for tracing the saga across services. |
| Exporter (cloud) | `Azure.Monitor.OpenTelemetry.AspNetCore` | One-line wiring for Application Insights. |
| Exporter (local) | OTLP → Aspire dashboard | Aspire AppHost wires this automatically. |

**Trace context propagation across ASB is non-negotiable** for a saga-based system — without it you can't see the end-to-end checkout flow. MassTransit's OpenTelemetry package handles this.

**Confidence: HIGH.**

### Resilience

`Microsoft.Extensions.Http.Resilience` (built on Polly v8) for HTTP clients. MassTransit handles ASB-side retry. For circuit breakers on outbound HTTP (e.g., calling Payments simulator), use the standard `AddStandardResilienceHandler()` extension on `HttpClient`.

**Confidence: HIGH.**

### Local Orchestration

**Recommend: .NET Aspire 10.x for local dev orchestration, with Docker Compose as the deliverable artefact.**

The PROJECT.md requirement is Docker Compose, but Aspire and Compose are complementary:

- Use the **Aspire AppHost** as the local dev launcher (one `dotnet run` brings up all services, Postgres, ASB emulator, OTEL collector).
- Use `aspire publish` (or the Aspire Docker Compose publisher) to **generate** `docker-compose.yml` from the AppHost model.
- Commit both: `eshop.AppHost/` (developer experience) and `docker-compose.yml` (artefact required by PROJECT.md).

**Azure Service Bus Emulator** (`mcr.microsoft.com/azure-messaging/servicebus-emulator`) is the right local ASB substitute as of 2026 — it's the official Microsoft-shipped container and supports topics/subscriptions/dead-letter (sessions/transactions limited; document any gaps).

**Confidence: MEDIUM** on Aspire (architectural recommendation, optional — Docker Compose alone is also valid); **HIGH** on the ASB emulator as the local ASB substitute.

### Containers & Deployment

| Concern | Choice |
|---------|--------|
| Base image | `mcr.microsoft.com/dotnet/aspnet:10.0-noble-chiseled` (distroless-style, ~30 MB) |
| SDK image (build stage) | `mcr.microsoft.com/dotnet/sdk:10.0` |
| Multi-stage builds | Mandatory — restore, publish, then copy to runtime stage |
| Native AOT | Optional per service; payments/notifications are good candidates (small, no EF Core) |
| Azure runtime | Azure Container Apps (per PROJECT.md) — KEDA-based ASB scaler is the killer feature: services scale from 0 based on queue depth |

**Confidence: HIGH.**

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

**Anti-recommendation:** Do NOT use NgModules-based architecture, RxJS-heavy state pyramids, or `@Input()`/`@Output()` decorators when signals/`input()`/`output()` functions are available. The portfolio signal you want to send is "modern Angular."

**Confidence: MEDIUM** on Angular 20 specifically (Angular 21 may be current by 2026-05; verify); HIGH on the zoneless + standalone + signals direction regardless of major version.

### Infrastructure as Code

| Tool | Version | Purpose |
|------|---------|---------|
| Terraform | 1.10+ | IaC engine |
| `hashicorp/azurerm` | **4.x** | Azure resource provider |
| `hashicorp/azuread` | 3.x | App registrations, service principals |
| `hashicorp/random` | 3.x | Suffixes for globally unique names |
| (Optional) `Azure/azapi` | 2.x | Preview / not-yet-in-AzureRM resources |

**Module structure (per PROJECT.md `infra/modules/` + `environments/{dev,prod}/`):**

| Module | Resources |
|--------|-----------|
| `network` | VNet, subnets, NSGs, private endpoints |
| `service-bus` | Namespace, topics, subscriptions, SAS / managed identity auth |
| `container-apps` | Container Apps Environment, Log Analytics workspace |
| `container-app` | Single ACA app (used N times — one per microservice) |
| `postgres` | Flexible Server, databases, firewall rules, key vault secret |
| `identity` | User-assigned managed identities + role assignments |
| `frontdoor` | (Optional) Azure Front Door for the Angular SPA |

**State backend:** Azure Storage Account with state locking via blob lease. Configure per-environment.

**Why AzureRM over Bicep:** PROJECT.md explicitly chose Terraform for job-market visibility (Key Decisions row). Stick with it.

**Confidence: HIGH** on AzureRM provider 4.x architectural choice; verify exact minor version at pin time (provider 4.x has had several minor releases through 2025).

### Testing Stack

| Layer | Tooling |
|-------|---------|
| Unit | xUnit v3, FluentAssertions, NSubstitute |
| Integration (per service) | `Microsoft.AspNetCore.Mvc.Testing` + Testcontainers (Postgres) + MassTransit in-memory transport |
| Contract | Verify.Xunit snapshots of ASB message contracts (compile-time + snapshot guard against breaking changes) |
| End-to-end saga | Testcontainers (Postgres + ASB emulator) + Aspire `DistributedApplicationTestingBuilder` |
| Load | k6 |

**Confidence: HIGH.**

## Installation Sketch

```xml
<!-- A typical service .csproj -->
<ItemGroup>
  <PackageReference Include="Microsoft.AspNetCore.OpenApi" Version="10.0.*" />
  <PackageReference Include="Microsoft.AspNetCore.Authentication.JwtBearer" Version="10.0.*" />
  <PackageReference Include="Microsoft.EntityFrameworkCore" Version="10.0.*" />
  <PackageReference Include="Microsoft.EntityFrameworkCore.Design" Version="10.0.*" />
  <PackageReference Include="Npgsql.EntityFrameworkCore.PostgreSQL" Version="10.0.*" />
  <PackageReference Include="MassTransit" Version="8.*" />
  <PackageReference Include="MassTransit.Azure.ServiceBus.Core" Version="8.*" />
  <PackageReference Include="MassTransit.EntityFrameworkCore" Version="8.*" />
  <PackageReference Include="MediatR" Version="12.*" />
  <PackageReference Include="FluentValidation.AspNetCore" Version="11.*" />
  <PackageReference Include="Riok.Mapperly" Version="4.*" />
  <PackageReference Include="Serilog.AspNetCore" Version="8.*" />
  <PackageReference Include="Serilog.Sinks.OpenTelemetry" Version="*" />
  <PackageReference Include="OpenTelemetry.Extensions.Hosting" Version="1.*" />
  <PackageReference Include="OpenTelemetry.Instrumentation.AspNetCore" Version="1.*" />
  <PackageReference Include="OpenTelemetry.Instrumentation.Http" Version="1.*" />
  <PackageReference Include="Azure.Monitor.OpenTelemetry.AspNetCore" Version="1.*" />
  <PackageReference Include="Microsoft.Extensions.Http.Resilience" Version="9.*" />
</ItemGroup>
```

```json
// Angular service frontend/ecommerce-app/package.json (key deps)
{
  "dependencies": {
    "@angular/core": "^20.0.0",
    "@angular/router": "^20.0.0",
    "@angular/common": "^20.0.0",
    "@angular/forms": "^20.0.0",
    "@angular/material": "^20.0.0",
    "angular-auth-oidc-client": "^19.0.0",
    "rxjs": "^7.8.0"
  },
  "devDependencies": {
    "@angular/cli": "^20.0.0",
    "@analogjs/vitest-angular": "^1.0.0",
    "@playwright/test": "^1.49.0",
    "typescript": "~5.6.0"
  }
}
```

```hcl
# infra/environments/dev/versions.tf
terraform {
  required_version = ">= 1.10.0"
  required_providers {
    azurerm = { source = "hashicorp/azurerm", version = "~> 4.0" }
    azuread = { source = "hashicorp/azuread", version = "~> 3.0" }
    random  = { source = "hashicorp/random",  version = "~> 3.6" }
  }
  backend "azurerm" {}
}
```

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

Before writing exact versions into `.csproj` / `package.json` / `versions.tf`, verify each below (web tools were unavailable in this research session):

- [ ] `MassTransit` and `MassTransit.Azure.ServiceBus.Core` — latest 8.x minor on nuget.org; check release notes for any breaking changes since v8.0
- [ ] EF Core 10 + Npgsql 10 — confirm Npgsql 10 GA is shipped (it usually trails EF Core GA by 1–4 weeks)
- [ ] `Microsoft.AspNetCore.OpenApi` — confirm it's still the default OpenAPI generator in .NET 10
- [ ] MediatR — check current license terms; if v13 has shipped with commercial terms, lock to v12.x or switch to Mediator.SourceGenerator and update this doc
- [ ] AutoMapper — confirm license stance before assuming anyone still wants it (same reason)
- [ ] OpenIddict 6.x — confirm latest minor; verify .NET 10 support
- [ ] Angular — confirm whether Angular 20 or 21 is current; both follow the same architectural recommendations here
- [ ] Terraform AzureRM 4.x — confirm latest 4.x minor; check changelog for Container Apps + Service Bus resource changes
- [ ] Azure Service Bus Emulator container — confirm latest tag and feature parity gaps with cloud ASB (sessions, transactions)
- [ ] `mcr.microsoft.com/dotnet/aspnet:10.0-noble-chiseled` tag exists (vs `8.0-jammy-chiseled` etc.)

## Sources

Web verification was unavailable for this research session (WebSearch, WebFetch, and Context7 CLI all denied). Recommendations are grounded in:

- The existing repo state (`src/building-blocks/Contracts/Contracts.csproj` pinning `net10.0`)
- PROJECT.md constraints (.NET 10, Azure Service Bus, Angular, Terraform, Docker Compose, Azure Container Apps)
- Well-established .NET / Azure ecosystem patterns through early 2026 (training data)
- Microsoft reference architectures (eShop, eShopOnContainers lineage) referenced in PROJECT.md

**All version numbers should be treated as MEDIUM confidence until verified against the live registries** using the checklist above. Architectural choices (MassTransit, EF Core, OpenIddict, Terraform AzureRM, Aspire, signals-first Angular) are HIGH confidence because they are well-established by 2025 and rest on durable ecosystem trends rather than transient version details.
