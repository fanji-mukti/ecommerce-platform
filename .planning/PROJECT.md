# ECommerce Platform

## What This Is

An event-driven e-commerce platform built as a portfolio and learning project. It demonstrates microservices architecture using .NET 10 and Azure Service Bus, with DDD, Saga/process manager patterns, and eventual consistency across eight independent services. Deployed locally via Docker Compose and to Azure via Terraform-managed infrastructure.

## Core Value

A working checkout saga that spans Catalog, Cart, Orders, Payments, Fulfillment, and Notifications — demonstrating event-driven coordination between microservices without direct coupling.

## Requirements

### Validated

(None yet — ship to validate)

### Active

- [ ] Product Catalog service — browse and retrieve products (CRUD, search)
- [ ] Cart service — add/remove items, persistent cart per user
- [ ] Checkout service — initiate checkout, publish events to trigger saga
- [ ] Orders service — create and track orders, expose order history
- [ ] Identity service — user sign-up, login, session management
- [ ] Payments service — simulated payment gateway, publish payment result events
- [ ] Fulfillment service — consume order events, update fulfillment status
- [ ] Notifications service — consume domain events, send email/in-app notifications
- [ ] Shared Contracts library — ASB message type definitions referenced by all services
- [ ] Checkout saga — orchestrate multi-service checkout flow via process manager
- [ ] Angular frontend — browse catalog, manage cart, complete checkout, view orders
- [ ] Terraform IaC — provision Azure Service Bus, Container Apps, and supporting resources
- [ ] Architecture Decision Records — numbered markdown ADRs for all key architectural choices
- [ ] Docker Compose — local orchestration for all services and dependencies

### Out of Scope

- Real payment processing — using simulated gateway; Stripe integration is v2
- Mobile app — web-first
- Real-time features (WebSockets, SignalR) — not needed for portfolio demo
- Admin dashboard — v2

## Context

- **Mono-repo, multi-solution**: each service has its own `.sln` file so it can be opened independently in Visual Studio; all solutions reference the shared `Contracts` project via relative path
- **Repo structure**:
  ```
  src/
    services/{service-name}/   ← Service.API/ + Service.sln
    building-blocks/Contracts/ ← shared ASB message contracts (existing)
    frontend/ecommerce-app/    ← Angular app
  infra/
    modules/                   ← reusable Terraform modules
    environments/dev | prod/
  docs/adr/                    ← numbered ADR markdown files
  docker-compose.yml
  ```
- **Existing code**: `src/building-blocks/Contracts/` — a .NET 10 class library for shared event/message contracts, already scaffolded
- **Key patterns to demonstrate**: DDD aggregates and value objects, bounded contexts, Saga/process manager (checkout), CQRS-style read models where appropriate, eventual consistency via ASB
- **Reference inspiration**: Microsoft eShopOnContainers

## Constraints

- **Tech stack**: .NET 10, Azure Service Bus, Angular, Terraform, Docker Compose
- **Payments**: Simulated only — no real payment provider credentials needed
- **Deployment**: Local (Docker Compose) + Azure (Container Apps via Terraform)
- **ADR format**: MADR (Markdown Architectural Decision Records) — one file per decision in `docs/adr/`

## Key Decisions

| Decision | Rationale | Outcome |
|----------|-----------|---------|
| Azure Service Bus for async messaging | Native Azure, enterprise features (topics/subscriptions, dead-letter), fits Azure deployment target | — Pending |
| Multi-solution mono-repo | Each service openable independently in VS; Contracts shared via project reference, not NuGet | — Pending |
| Simulated payment gateway | Avoids credential management; demonstrates the pattern without production complexity | — Pending |
| Terraform for IaC | Most widely recognised IaC tool; strong Azure provider; job market visibility | — Pending |
| Angular for frontend | Demonstrates full-stack breadth; portfolio value beyond .NET alone | — Pending |
| Saga pattern for checkout | Showcases event-driven coordination across Payment, Fulfillment, Notification services | — Pending |

## Evolution

This document evolves at phase transitions and milestone boundaries.

**After each phase transition** (via `/gsd-transition`):
1. Requirements invalidated? → Move to Out of Scope with reason
2. Requirements validated? → Move to Validated with phase reference
3. New requirements emerged? → Add to Active
4. Decisions to log? → Add to Key Decisions
5. "What This Is" still accurate? → Update if drifted

**After each milestone** (via `/gsd-complete-milestone`):
1. Full review of all sections
2. Core Value check — still the right priority?
3. Audit Out of Scope — reasons still valid?
4. Update Context with current state

---
*Last updated: 2026-05-29 after initialization*
