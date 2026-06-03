---
status: accepted
date: 2026-06-03
decision-makers: [Fanji Ari Mukti]
consulted: []
informed: []
---

# Use Azure Service Bus as the Async Messaging Backbone

## Context and Problem Statement

Eight microservices need to coordinate without direct coupling. Orders, Payments, Fulfillment, Notifications, and Catalog must exchange events reliably — including across service restarts, with retries, dead-letter queues, and at-least-once delivery guarantees. The deployment target is Azure Container Apps, which has native integrations with Azure messaging services.

## Decision Drivers

- Must support topics/subscriptions for fan-out (one event, multiple subscribers)
- Must integrate with KEDA for auto-scaling Azure Container Apps based on queue depth
- Must have an emulator for local development without cloud credentials
- Must support sessions or ordering for saga state machine messages
- Must be operable via managed identity (no connection string secrets in production)

## Considered Options

- Azure Service Bus (Standard tier)
- RabbitMQ (self-hosted via Docker)
- Apache Kafka (Azure Event Hubs or self-hosted)
- In-process events via MediatR (no broker)

## Decision Outcome

Chosen: **Azure Service Bus (Standard tier)** — native Azure service that requires no self-hosting, supports topics/subscriptions for bounded context fan-out, integrates directly with KEDA for ACA auto-scaling, and has a Docker-based local emulator (`mcr.microsoft.com/azure-messaging/servicebus-emulator`).

### Consequences

- Good: KEDA ASB scaler enables services to scale from 0 based on queue depth — a key portfolio differentiator for Azure Container Apps deployment.
- Good: Managed identity auth eliminates connection string secrets in production.
- Good: `RunAsEmulator()` in Aspire provisions the emulator container automatically for local dev.
- Bad: Standard tier required — Basic tier has no topic support. Standard tier has a cost (though minimal at dev scale).
- Bad: ASB emulator does not persist messages across container restarts — local dev state is ephemeral.
- Bad: AMQP-based; no direct HTTP access without a relay (acceptable — MassTransit handles transport).

## Pros and Cons of the Options

### Azure Service Bus (Standard tier)
- Pro: Native Azure, no ops burden
- Pro: Topics/subscriptions align with bounded context design
- Pro: KEDA integration for ACA scaling
- Pro: Local emulator available
- Con: Standard tier cost (modest)
- Con: Emulator has feature gaps vs cloud (no sessions in some emulator versions)

### RabbitMQ
- Pro: Free, excellent local DX, MassTransit first-class support
- Con: Requires self-hosting in Azure (VM or AKS) — no managed service
- Con: No native KEDA scaler for ACA (custom scaler needed)
- Con: Portfolio deploys to Azure — using a non-Azure service adds operational complexity

### Apache Kafka / Azure Event Hubs
- Pro: Excellent for high-throughput event streaming
- Con: Overkill for an 8-service e-commerce MVP
- Con: Kafka's log-based model complicates saga compensation patterns

### In-Process Events (MediatR)
- Pro: Zero infrastructure, instant dev feedback
- Con: Cannot span service boundaries — defeats the entire microservices premise
- Con: No durability, no retry, no DLQ

## More Information

- Related: ADR-0007 (topic naming strategy), ADR-0006 (MassTransit transport over ASB)
- ASB Standard tier pricing: https://azure.microsoft.com/pricing/details/service-bus/
