---
status: accepted
date: 2026-06-03
decision-makers: [Fanji Ari Mukti]
consulted: []
informed: []
---

# Use One ASB Topic per Producing Bounded Context

## Context and Problem Statement

Each service publishes multiple message types. Azure Service Bus Standard tier has a limit of 1,000 entities per namespace (topics + queues + subscriptions). MassTransit's default behaviour creates one topic per message type, which at 40+ message types across 8 services would consume a large portion of that limit and make the ASB topology hard to reason about.

## Decision Drivers

- ASB entity count must stay well within the Standard tier limit (1,000 max; ASB emulator max 50)
- Topic naming must align with bounded context boundaries, not internal implementation details
- Subscription topology must remain understandable without reading source code
- MassTransit must be configured to honour this naming convention

## Considered Options

- One topic per message type (MassTransit default — N topics per service)
- One topic per producing bounded context (1 topic per service — chosen)
- One shared topic for all services (single topic, all messages routed by filter)

## Decision Outcome

Chosen: **One topic per producing bounded context** — each service publishes all its events to a single topic named after the service (e.g., `catalog-events`). Consumers subscribe to that topic with a filter for the specific message types they care about.

### Consequences

- Good: Maximum 8 topics (one per service) vs potentially 40+ with per-message topics. ASB emulator's 50-entity limit is respected.
- Good: Topic names reflect bounded context ownership, not internal class names.
- Bad: MassTransit's default does NOT apply this convention — every service must override via `[EntityName("catalog-events")]` attribute on message records, or via `cfg.Message<T>(x => x.SetEntityName(...))` in bus configuration. This override is implemented in Phase 2 when MassTransit is first configured.
- Neutral: Consumers still filter by message type via subscription filter expressions — fan-out semantics are preserved.

## Pros and Cons of the Options

### One Topic per Bounded Context
- Pro: Minimal entity count; bounded context alignment
- Pro: Clear ownership — one team owns one topic
- Con: Requires MassTransit EntityName override; not the default

### One Topic per Message Type (MassTransit Default)
- Pro: Zero configuration — works out of the box
- Con: Entity count explosion at scale; ASB emulator limit easily exceeded in development
- Con: Topics named after C# class names — leaks implementation details

### One Shared Topic for All Services
- Pro: Minimum entity count (1 topic)
- Con: All messages compete on one topic; filter complexity grows unboundedly
- Con: No bounded context alignment — impossible to understand message ownership

## More Information

- MassTransit EntityName override syntax (to be applied in Phase 2):
  ```csharp
  [EntityName("catalog-events")]
  public record CatalogItemPublished(...) : IMessageEnvelope;
  ```
  Or via bus configuration: `cfg.Message<CatalogItemPublished>(x => x.SetEntityName("catalog-events"));`
- ADR-0006 documents the MassTransit version and licensing constraints this decision builds on.
