---
status: accepted
date: 2026-06-03
decision-makers: [Fanji Ari Mukti]
consulted: []
informed: []
---

# Enforce Database-per-Service Isolation

## Context and Problem Statement

Eight services need persistence. If they share a database schema, a schema change in one service can break another, deployments become coupled, and service autonomy is lost. The goal is to preserve independent deployability.

## Decision Drivers

- Services must be independently deployable without coordinating schema migrations
- No service should query another service's tables directly
- Cross-service data access must go through published events or APIs, not SQL joins
- Local dev simplicity: one Postgres container is acceptable in Phase 1

## Considered Options

- Shared database (single schema for all services)
- Shared database (per-service schema within one server)
- Database per service (each service owns its connection string and schema)

## Decision Outcome

Chosen: **Database per service** — each service owns its schema and connection string. No service can reference another service's tables. Cross-service queries are prohibited at the architecture level.

### Consequences

- Good: Independent schema migrations per service — no cross-team coordination.
- Good: Each service can choose its storage technology independently (all use Postgres in this project, but the architecture doesn't require it).
- Bad: Cross-service reporting requires event-driven projections or a dedicated read model — no SQL JOINs across services.
- Neutral (Phase 1): A single shared Postgres container is provisioned by Aspire for simplicity. Isolation is enforced at connection-string level: each service gets its own database within the shared container. Separate containers enforced from Phase 2 onward.

## Pros and Cons of the Options

### Database per Service
- Pro: Full autonomy — services deploy independently
- Pro: Schema changes are local to the owning service
- Con: No cross-service SQL joins; requires event-driven projections for aggregated views

### Shared Database (Single Schema)
- Pro: Simple queries, easy cross-service reporting
- Con: Schema coupling — one team's migration can break another service
- Con: Violates service autonomy; defeats the purpose of microservices

### Shared Database (Per-Service Schema)
- Pro: Logical separation within one server
- Con: Still physically coupled — a database outage takes all services down together
- Con: Schema isolation is a convention, not enforced by the infrastructure

## More Information

- Related: ADR-0005 (saga orchestration requires cross-service coordination via events, not SQL)
- Cross-service queries are prohibited. Saga state is owned by the Orders service and replicated to read models via published events.
