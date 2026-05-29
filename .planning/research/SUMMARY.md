# Research Summary — ECommerce Platform

**Project:** Event-driven .NET microservices e-commerce platform (portfolio / learning)
**Synthesized:** 2026-05-30
**Source files:** STACK.md, FEATURES.md, ARCHITECTURE.md, PITFALLS.md
**Overall confidence:** MEDIUM-HIGH on architectural choices; MEDIUM on specific library versions (web tools unavailable — run Pre-Pin Verification Checklist before pinning)

---

## Executive Summary

This is a portfolio project whose entire reason for existing is the **checkout saga that spans eight services over Azure Service Bus**. Every other decision should be evaluated against whether it makes that saga clearer to demo, easier to reason about, and more credible as a senior-level .NET microservices artefact.

The well-trodden path is unambiguous: **.NET 10 + MassTransit 8.x + Azure Service Bus + EF Core 10 / PostgreSQL + MassTransit state machine for saga + transactional outbox + idempotent consumers + database-per-service + OpenIddict for OIDC + YARP gateway + Terraform AzureRM 4.x + Angular 20 (signals, zoneless, standalone) + .NET Aspire 10.x as local-dev launcher.**

Build order is forced by dependencies: foundations → Identity + Catalog + Gateway → Cart + Orders skeleton → Checkout saga + simulated Payments (the headline demo) → Fulfillment + Notifications → hardening.

---

## Stack

| Layer | Choice | Notes |
|-------|--------|-------|
| Runtime | .NET 10 (LTS) | Already pinned in `Contracts.csproj` |
| API | ASP.NET Core Minimal APIs + `Microsoft.AspNetCore.OpenApi` | |
| Messaging | **MassTransit 8.x** + `MassTransit.Azure.ServiceBus.Core` + `MassTransit.EntityFrameworkCore` (outbox) | Raw ASB SDK rejected; NServiceBus (commercial) rejected |
| Saga | `MassTransitStateMachine<T>` + `EntityFrameworkSagaRepository` | Orchestration, not choreography |
| Persistence | EF Core 10 + Npgsql 10 (PostgreSQL); Dapper optional for read-side | One DB per service — hard rule |
| Cart store | Redis (preferred for polyglot signal) | Postgres fallback acceptable |
| Mediator | MediatR 12.x (with license-risk ADR) or Mediator.SourceGenerator | Decide in Phase 2 |
| Mapping | **Mapperly** (not AutoMapper — license/reflection) | |
| Validation | FluentValidation 11.x | |
| Identity | ASP.NET Core Identity + **OpenIddict 6.x** | Not Duende (commercial) |
| Gateway | **YARP 2.x** | Ocelot is in maintenance — use YARP |
| Observability | Serilog + OpenTelemetry + `MassTransit.OpenTelemetry` + Azure Monitor | From Phase 1 — never retrofit |
| Local orchestration | **.NET Aspire 10.x** AppHost → `aspire publish` → `docker-compose.yml`; ASB emulator container | |
| Frontend | **Angular 20** zoneless + standalone + signals; `angular-auth-oidc-client`; Vitest + Playwright | |
| IaC | Terraform 1.10+ with `hashicorp/azurerm` 4.x | Remote state from day 1 |
| Testing | xUnit v3 + Testcontainers + WireMock.Net | |

**Critical:** ASB **Standard or Premium** tier — Basic has no topic support.

---

## Table Stakes Per Service

| Service | Minimum viable |
|---------|----------------|
| **Catalog** | `GET /products` (paged + category filter), `GET /products/{id}`, seeded 20–50 SKUs with price + stock |
| **Cart** | CRUD on cart items, per-user, price snapshot at line level |
| **Checkout** | `POST /checkout` (202 + checkoutId), `GET /checkout/{id}`; hosts `CheckoutSaga` |
| **Orders** | `GET /orders`, `GET /orders/{id}`; status state machine Pending→Paid→Fulfilled/Cancelled/Failed |
| **Identity** | Register, login, `GET /me`; OpenIddict JWT issuance; seeded demo users |
| **Payments (sim)** | `AuthorisePayment` consumer; deterministic failure trigger (amount ending in `.99` declines); idempotent by checkoutId |
| **Fulfillment** | `OrderPaid` consumer; timer-based status advance; publishes `OrderShipped` |
| **Notifications** | In-app inbox (`GET /notifications`); consumes domain events; no real SMTP |
| **Contracts** | Pure message records + envelope (`MessageId`, `CorrelationId`, `CausationId`, `OccurredAt`); namespace per producer + `.V1` |
| **Angular** | `/catalog`, `/cart`, `/checkout`, `/orders`, `/orders/:id`, `/login`, `/register`; order-detail page polls for saga status |
| **Cross-cutting** | `/health` per service, OpenAPI, structured logging with correlation ID, Aspire dashboard |

---

## Checkout Saga Event Flow

Hosted in **Checkout service** as `MassTransitStateMachine<CheckoutSagaState>` persisted to Checkout's own PostgreSQL DB via `EntityFrameworkSagaRepository`. CorrelationId = CheckoutId on every message. Timeout scheduled at saga start (~15 min).

| # | Trigger | State | Outbound | Compensation |
|---|---------|-------|----------|--------------|
| 1 | HTTP `POST /checkout` | `Started` | `CheckoutStarted` (event) | — |
| 2 | `OrderCreated` received | `AwaitingPayment` | `PaymentRequested` (cmd) | `CancelOrder` |
| 3a | `PaymentProcessed` received | `Paid` | `FulfillmentRequested` (cmd) | `RefundPayment` |
| 3b | `PaymentFailed` received | `Failed` (terminal) | `CancelOrder` (cmd) | (this is the compensation) |
| 4 | `FulfillmentStarted` received | `InFulfillment` | — | `CancelShipment` + `RefundPayment` |
| 5a | `OrderShipped` received | `Completed` (terminal) | `CheckoutCompleted` (event) | — |
| 5b | `FulfillmentFailed` received | `Failed` (terminal) | `RefundPayment` + `CancelOrder` | — |
| ∞ | `CheckoutTimeout` fires | `TimedOut` | Compensation cascade | — |

**Demo storytelling device:** amounts ending in `.99` trigger `PaymentFailed` on demand — shows compensation live.

---

## Suggested Build Order

| Phase | Delivers | Research needed? |
|-------|----------|-----------------|
| **P1 — Foundations** | Contracts shape locked, Compose baseline (Postgres + Redis + ASB emulator + OTel), Aspire AppHost, first 8 ADRs | Light (verification only) |
| **P2 — Identity + Catalog + Gateway** | User can register/login/browse; YARP routing; Angular shell; outbox + inbox from first MT config | Possibly (OpenIddict 6.x + .NET 10) |
| **P3 — Cart + Orders skeleton** | Cart (Redis); Orders domain with state machine; frontend pages wired | Light |
| **P4 — Checkout Saga + Payments** | **Headline demo.** Click "Place Order" → saga runs → Paid. Deterministic failure. | **YES** — MassTransit saga + ASB scheduling, 8.x→9.x API changes |
| **P5 — Fulfillment + Notifications** | Full E2E happy path; order inbox; compensation paths; CQRS read model | Light |
| **P6 — Hardening + Azure + Demo** | DLQ handling, Terraform + remote state, Key Vault + Managed Identity, CI/CD, `demo.ps1` | **YES** — ACA + KEDA + Terraform AzureRM 4.x |

---

## Top 5 Pitfalls

| # | Pitfall | Phase to prevent | Prevention |
|---|---------|------------------|------------|
| 1 | **Shared database across services** | P1 | One DB container per service in Compose; code-review rule blocks cross-schema refs |
| 2 | **Bloated Contracts library** | P1 | Contracts = message records only; no EF/MediatR/domain logic; Roslyn analyzer to enforce |
| 3 | **Choreographed checkout saga** | P4 ADR before code | Explicit `CheckoutSaga` state machine; choreography reserved for fire-and-forget side effects |
| 4 | **Missing transactional outbox** | P2 (day one) | `AddEntityFrameworkOutbox<TDbContext>()` from first MassTransit config; inbox on every consumer |
| 5 | **ASB topic-per-event explosion** | P2 ADR + P6 Terraform | Topic per producing context (`orders-events`, `catalog-events`); SQL filter on `MessageType` |

**Also lock early:** in-memory saga state (P4), OpenTelemetry added late (P1), Terraform state local/unlocked (P6).

---

## Key ADRs by Phase

| Phase | ADRs |
|-------|------|
| **P1** | 0001 record-decisions, 0002 ASB choice, 0003 DB-per-service, 0004 YARP gateway, 0005 saga orchestration, 0006 MassTransit + outbox/inbox, 0007 ASB topic-per-context, 0008 MADR format |
| **P2** | 0009 auth topology, 0010 DDD pragmatism per service, 0011 EF migration init job, 0012 Mapperly over AutoMapper, 0013 MediatR license stance |
| **P3** | 0014 Cart = Redis, 0015 Orders aggregate, 0016 Order state machine |
| **P4** | 0017 saga design, 0018 saga persistence store, 0019 eventual-consistency UX contract |
| **P5** | 0020 CQRS read model, 0021 compensation policy |
| **P6** | 0022 DLQ retry/redelivery, 0023 Terraform layout + remote state, 0024 ACA + Managed Identity + Key Vault, 0025 CI/CD structure, 0026 Contracts schema evolution |

**Rule:** ADR written before or during the implementing PR. Status: `proposed → accepted → deprecated / superseded`. Never delete or rewrite accepted ADRs.

---

## Open Questions (deferred to phase research)

1. Cart store: Redis vs PostgreSQL → **P3** (lean Redis for polyglot demo signal)
2. Auth topology: per-service JWT validation vs gateway-trusted internal claim → **P2**
3. MediatR vs Mediator.SourceGenerator vs no mediator → **P2** (license risk)
4. Read-side projection tech for Orders history → **P5**
5. MassTransit + ASB state machine specifics (saga repo config, scheduled-message API, 8.x→9.x API changes) → **P4 research**
6. Azure Container Apps + KEDA ASB scalers + Terraform AzureRM 4.x resource shapes → **P6 research**
7. ASB emulator capabilities vs production (sessions, transactions) → **P1 + per phase**
8. Angular major version (20 vs 21) → verify at P2 start
9. Optional second differentiator (event-sourced Order aggregate, inventory reservation) → **post-P6**

---

*Synthesized: 2026-05-30 from STACK.md, FEATURES.md, ARCHITECTURE.md, PITFALLS.md*
