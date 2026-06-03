---
status: accepted
date: 2026-06-03
decision-makers: [Fanji Ari Mukti]
consulted: []
informed: []
---

# Use Saga Orchestration over Choreography for Checkout

## Context and Problem Statement

The checkout flow spans Orders, Payments, Fulfillment, Notifications, and Catalog — five services that must coordinate a distributed transaction with compensation paths when steps fail. If a payment fails, the reserved inventory must be released. If fulfillment fails, the payment must be refunded. These compensation paths need to be explicit, testable, and observable.

## Decision Drivers

- Compensation paths must be explicit and auditable — not implied by event chains
- The saga state must be inspectable at any point (what step are we on, what failed)
- Failure in one step must not silently leave the system in an inconsistent state
- MassTransit (chosen in ADR-0006) has first-class saga state machine support

## Considered Options

- Orchestration (central process manager / state machine owns the saga flow)
- Choreography (each service reacts to events and emits next events; no central coordinator)

## Decision Outcome

Chosen: **Orchestration** — a MassTransit state machine in the Orders service drives the checkout saga. All state transitions, compensation triggers, and timeout handling live in one place, making the flow inspectable and testable without tracing event chains across 5 services.

### Consequences

- Good: Single place to observe saga progress — the state machine's current state is the source of truth.
- Good: Compensation paths are explicit state transitions, not implicit event reactions.
- Good: MassTransit state machine integrates with the EF Core outbox for durable state persistence.
- Bad: The saga state machine is a coupling point — if it goes down, checkout coordination stops.
- Bad: Adding a new step to the saga requires modifying the central state machine.

## Pros and Cons of the Options

### Orchestration (Central State Machine)
- Pro: Explicit state and transitions — easy to reason about, test, and debug
- Pro: Single location to add timeout/compensation logic
- Pro: MassTransit Automatonymous-style state machines are purpose-built for this
- Con: Single point of failure for checkout coordination

### Choreography (Reactive Event Chain)
- Pro: Fully decentralised — no single point of failure
- Pro: Services are loosely coupled; each just reacts to events it cares about
- Con: Compensation requires each service to listen for "undo" events — implicit, hard to trace
- Con: The overall saga flow is invisible unless you trace all 5 services' event logs simultaneously
- Con: Testing the end-to-end flow requires running all 5 services

## More Information

- ADR-0006 provides the MassTransit version and outbox configuration the saga depends on.
- The checkout saga is implemented in Phase 4.
- Saga state is persisted via MassTransit EF Core saga repository to the Orders service database.
