---
status: accepted
date: 2026-06-03
decision-makers: [Fanji Ari Mukti]
consulted: []
informed: []
---

# Pin MassTransit to 8.3.6 (Apache-2.0) with Transactional Outbox and Idempotent Inbox

## Context and Problem Statement

The platform needs a messaging abstraction over Azure Service Bus that provides: saga state machine support (for the checkout saga), a transactional outbox (to avoid dual-write between database and broker), idempotent inbox (to handle at-least-once delivery safely), and a test harness that works without a live broker. MassTransit is the leading OSS solution in the .NET ecosystem, but its licensing changed between major versions.

## Decision Drivers

- Must be Apache-2.0 or equivalent permissive OSS license (no commercial license for a portfolio project)
- Must support MassTransit saga state machines (required by ADR-0005)
- Must have EF Core outbox integration (transactional outbox is non-negotiable — see pitfall below)
- Must work with Azure Service Bus transport
- Must provide a test harness for in-memory integration tests

## Considered Options

- MassTransit 8.3.6 (Apache-2.0 — last permissive release)
- MassTransit 9.x (Massient commercial license)
- NServiceBus (Particular Platform commercial license)
- Raw `Azure.Messaging.ServiceBus` SDK

## Decision Outcome

Chosen: **MassTransit 8.3.6 (Apache-2.0)** — the last release under the Apache-2.0 license, with full saga state machine support, EF Core outbox/inbox, ASB transport, and an in-memory test harness. Receives security patches through end of 2026.

### Consequences

- Good: Apache-2.0 license — no commercial friction for an OSS portfolio project.
- Good: `MassTransit.EntityFrameworkCore` provides the transactional outbox; cannot be retrofitted later without major rework.
- Good: In-memory test harness enables saga integration tests without a running ASB instance.
- Bad (v9 license): MassTransit 9.x requires a **Massient commercial license**. Installing v9 would violate the portfolio's OSS-only constraint. Pin 8.3.6 explicitly in every `.csproj` file — do not use floating versions.
- Bad (EmulatorHost() absent): The `EmulatorHost()` API does not exist in v8.3.6 — it was added in v9. Phase 2 must configure ASB connectivity via Aspire-injected `ConnectionString` and may require `ServiceBusClientOptions` workarounds for the local emulator. This will be addressed during Phase 2 implementation.
- Bad (v8 EOL): MassTransit v8 receives security patches through end of 2026. A license re-evaluation is required in Q4 2026: either a commercial Massient license, migration to an alternative (Wolverine, NServiceBus), or continued use of v8 with community patches.

## Pros and Cons of the Options

### MassTransit 8.3.6 (Apache-2.0)
- Pro: Full-featured — saga, outbox, inbox, test harness, ASB transport
- Pro: Largest .NET messaging library community; appears in most job listings
- Con: EOL security patches only through 2026; needs re-evaluation in Q4 2026

### MassTransit 9.x (Massient Commercial)
- Pro: Latest features including EmulatorHost() for ASB emulator
- Con: Commercial license — unacceptable for an OSS portfolio project

### NServiceBus (Particular Platform)
- Pro: Mature, battle-tested, excellent saga support
- Con: Commercial Particular Platform license above a low revenue threshold
- Con: License friction is a blocker for a public portfolio project

### Raw Azure.Messaging.ServiceBus SDK
- Pro: No abstraction layer; direct Azure SDK control
- Con: No saga support, no outbox, no test harness, manual topology management, manual serialization
- Con: Re-implements what MassTransit provides, poorly

## More Information

- Massient licensing details: https://massient.io
- ADR-0005 (saga orchestration) depends on MassTransit's state machine implementation.
- ADR-0007 (topic naming) describes the `[EntityName]` override required because MassTransit's default topic-per-message behaviour is overridden at the bounded context level.
- **CRITICAL for Phase 2**: Do not `dotnet add package MassTransit` without specifying `--version 8.3.6`. The default resolves to 9.x which has a commercial license.
