# Architecture Patterns

**Domain:** Event-driven .NET microservices e-commerce platform
**Researched:** 2026-05-30
**Overall confidence:** HIGH (patterns are well established in Microsoft .NET microservices guidance, eShopOnContainers reference, MassTransit/NServiceBus saga literature, and Evans/Vernon DDD canon)

> **Sourcing note:** Web access was unavailable during this research session. All recommendations are grounded in established reference architectures (Microsoft .NET Microservices: Architecture for Containerized .NET Applications, eShopOnContainers, MassTransit saga patterns, Chris Richardson's microservices.io saga/CQRS catalogue, and DDD canon). Confidence flagged per section.

---

## 1. High-Level Architecture

```
                                                ┌─────────────────────────┐
                                                │     Angular SPA         │
                                                │  (browser, port 4200)   │
                                                └────────────┬────────────┘
                                                             │ HTTPS / JSON
                                                             ▼
                                                ┌─────────────────────────┐
                                                │     API Gateway         │
                                                │   (YARP reverse proxy)  │
                                                │  - routing              │
                                                │  - auth token forward   │
                                                │  - rate limiting        │
                                                └──┬──┬──┬──┬──┬──┬──┬──┬─┘
                                                   │  │  │  │  │  │  │  │  (north-south, sync HTTP)
                       ┌───────────────┬───────────┘  │  │  │  │  │  │  └─────────────┐
                       ▼               ▼              ▼  ▼  ▼  ▼  ▼  ▼                ▼
                 ┌──────────┐    ┌──────────┐    ┌──────────┐ ┌──────────┐ ┌──────────┐ ┌──────────────┐
                 │ Identity │    │ Catalog  │    │   Cart   │ │ Checkout │ │  Orders  │ │ Notifications│
                 │   API    │    │   API    │    │   API    │ │   API    │ │   API    │ │     API      │
                 └────┬─────┘    └────┬─────┘    └────┬─────┘ └────┬─────┘ └────┬─────┘ └──────┬───────┘
                      │               │               │            │            │              │
                      │               │               │            │            │              │
                      │               │               │  (publish/subscribe — east-west, async)│
                      │               │               │            │            │              │
                      ▼               ▼               ▼            ▼            ▼              ▼
              ┌─────────────────────────────────────────────────────────────────────────────────────┐
              │                            Azure Service Bus                                        │
              │  Topics: catalog-events, cart-events, order-events, payment-events,                 │
              │          fulfillment-events, notification-commands                                  │
              │  Subscriptions per service with SQL filters on MessageType                          │
              └─────────────────────────────────────────────────────────────────────────────────────┘
                                  ▲                              ▲
                                  │                              │
                          ┌───────┴─────────┐          ┌─────────┴────────┐
                          │   Payments      │          │   Fulfillment    │
                          │  (simulated)    │          │      API         │
                          └─────────────────┘          └──────────────────┘

  Per-service data stores (database-per-service):
  ┌──────────┐ ┌──────────┐ ┌──────────┐ ┌──────────┐ ┌──────────┐ ┌──────────┐ ┌──────────┐ ┌──────────┐
  │Identity  │ │Catalog   │ │Cart      │ │Checkout  │ │Orders    │ │Payments  │ │Fulfill.  │ │Notif.    │
  │PostgreSQL│ │PostgreSQL│ │Redis     │ │PostgreSQL│ │PostgreSQL│ │PostgreSQL│ │PostgreSQL│ │PostgreSQL│
  └──────────┘ └──────────┘ └──────────┘ └──────────┘ └──────────┘ └──────────┘ └──────────┘ └──────────┘
```

**Confidence:** HIGH — this layout mirrors eShopOnContainers and Microsoft's reference microservices architecture, adapted to ASB instead of RabbitMQ.

---

## 2. Bounded Context Map

Each service is a single bounded context with its own ubiquitous language, data store, and lifecycle. Services **never share databases** — integration happens through events on Azure Service Bus or, in narrow read-time cases, through HTTP calls via the API Gateway.

### 2.1 Context Table

| # | Bounded Context | Core Responsibility | Owns (Data) | Publishes (Events) | Subscribes To (Events) | Sync API (Inbound) |
|---|------|------|------|------|------|------|
| 1 | **Identity** | User registration, login, JWT issuance, refresh tokens | Users, Credentials, Roles, RefreshTokens | `UserRegistered`, `UserDeactivated` | — | `POST /register`, `POST /login`, `POST /refresh`, `GET /me` |
| 2 | **Catalog** | Product master data, categories, inventory snapshot, search | Products, Categories, Prices, StockLevels | `ProductCreated`, `ProductPriceChanged`, `ProductDiscontinued`, `StockLevelChanged` | — | `GET /products`, `GET /products/{id}`, `GET /categories`, `GET /search` |
| 3 | **Cart** | Per-user shopping cart, line items, totals (transient) | CartHeader, CartLineItem (Redis or relational) | `CartItemAdded`, `CartCheckedOut`, `CartAbandoned` (optional) | `ProductPriceChanged` (revalidate cart prices), `OrderCreated` (clear cart) | `GET /cart`, `POST /cart/items`, `DELETE /cart/items/{id}` |
| 4 | **Checkout** | Saga coordinator (process manager); validates cart, reserves stock, orchestrates payment + fulfillment | CheckoutSagaState (persisted process-manager state) | `CheckoutStarted`, `StockReservationRequested`, `PaymentRequested`, `CheckoutCompleted`, `CheckoutFailed` | `StockReserved`, `StockReservationFailed`, `PaymentProcessed`, `PaymentFailed`, `OrderCreated`, `FulfillmentStarted`, `OrderShipped` | `POST /checkout` (kicks off saga, returns checkoutId) |
| 5 | **Orders** | Authoritative order record, order history, status projection | Orders, OrderLines, OrderStatusHistory | `OrderCreated`, `OrderStatusChanged`, `OrderCancelled` | `PaymentProcessed` (mark paid), `FulfillmentStarted` (mark in fulfillment), `OrderShipped` (mark shipped) | `GET /orders`, `GET /orders/{id}` |
| 6 | **Payments** (simulated) | Process payment authorization/capture against simulated gateway | PaymentAttempts, PaymentResults | `PaymentProcessed`, `PaymentFailed`, `RefundIssued` | `PaymentRequested` | `GET /payments/{id}` (read-only for support/debug) |
| 7 | **Fulfillment** | Pick-pack-ship workflow, shipment tracking | Shipments, FulfillmentTasks, TrackingNumbers | `FulfillmentStarted`, `OrderShipped`, `FulfillmentFailed` | `OrderCreated`, `PaymentProcessed` (only ship if both received) | `GET /shipments/{orderId}` |
| 8 | **Notifications** | Send transactional email / in-app messages | NotificationLog, UserPreferences | `NotificationSent` (audit only) | `OrderCreated`, `PaymentProcessed`, `PaymentFailed`, `OrderShipped`, `UserRegistered`, `CheckoutFailed` | `GET /notifications` (user inbox) |

**Confidence:** HIGH on context boundaries; MEDIUM on exact event-name lists (you will discover/refine these during implementation; the shape and intent are correct).

### 2.2 Context Relationships (DDD)

| Upstream | Downstream | Relationship | Notes |
|---|---|---|---|
| Catalog | Cart, Orders, Checkout | **Published Language** (via events) + **Open Host Service** (HTTP read API) | Catalog is the source of truth for products; downstream services keep denormalized copies of `ProductId`, `Name`, `PriceAtPurchase` |
| Identity | All services | **Conformist** (JWT claims) | Services trust signed JWTs; no direct DB lookup of user table |
| Cart | Checkout | **Customer/Supplier** | Checkout reads cart contents at saga start, then takes ownership of the line snapshot |
| Checkout | Orders, Payments, Fulfillment, Notifications | **Process Manager / Orchestrator** | Checkout is the saga conductor; downstream contexts are participants |
| Orders | Notifications, Fulfillment | **Published Language** | Order events are first-class integration events |

**Confidence:** HIGH — these are canonical DDD context-mapping patterns (Evans, Vernon).

---

## 3. The Checkout Saga (Orchestration Process Manager)

This is the **core demonstration** of the platform. The Checkout service hosts a stateful saga that coordinates Orders, Payments, Fulfillment, and Notifications via events. Use **orchestration** (not choreography) — it is easier to reason about, debug, and explain in a portfolio context.

### 3.1 Happy-Path Event Flow

```
Actor: User clicks "Place Order"
   │
   ▼
[Frontend] ── POST /checkout ──▶ [API Gateway] ──▶ [Checkout API]
                                                       │
                                                       │ 1. Load Cart (HTTP → Cart API) — snapshot lines
                                                       │ 2. Persist CheckoutSaga (status = Started)
                                                       │ 3. Return 202 Accepted { checkoutId }
                                                       │
                                                       ▼
                                              Publish: CheckoutStarted
                                                       │
                  ┌────────────────────────────────────┼─────────────────────────────┐
                  ▼                                    ▼                             ▼
         [Notifications]                       [Orders]                     [Checkout — self]
         "checkout begun"                      Create Order                  (await OrderCreated)
                                               (status = Pending)
                                               Publish: OrderCreated
                                                       │
                                                       ▼
                                              [Checkout receives OrderCreated]
                                              Publish: PaymentRequested { orderId, amount, userId }
                                                       │
                                                       ▼
                                              [Payments]
                                              Simulate gateway call
                                              Publish: PaymentProcessed  (or PaymentFailed)
                                                       │
                  ┌────────────────────────────────────┼─────────────────────────────┐
                  ▼                                    ▼                             ▼
              [Orders]                          [Notifications]              [Checkout]
         status = Paid                         "payment confirmed"          Publish:
                                                                            FulfillmentStarted (cmd)
                                                                                    │
                                                                                    ▼
                                                                            [Fulfillment]
                                                                            Create Shipment
                                                                            Publish: FulfillmentStarted (evt)
                                                                                    │
                                                       ┌────────────────────────────┤
                                                       ▼                            ▼
                                                  [Orders]                    [Checkout]
                                                  status = InFulfillment      (await OrderShipped)
                                                                                    │
                                                       (simulated dispatch later)   │
                                                                                    ▼
                                                                            [Fulfillment]
                                                                            Publish: OrderShipped
                                                                                    │
                  ┌────────────────────────────────────────────────────────────────┤
                  ▼                                    ▼                            ▼
              [Orders]                          [Notifications]              [Checkout]
         status = Shipped                       "your order shipped"        Saga status = Completed
                                                                            Publish: CheckoutCompleted
```

### 3.2 Saga Steps Enumerated

| Step | Trigger | Saga Action | Outbound Message | Compensating Action |
|---|---|---|---|---|
| 1 | HTTP `POST /checkout` | Validate cart, snapshot lines, persist `CheckoutSaga { status: Started }` | `CheckoutStarted` (event) | n/a (nothing yet to undo) |
| 2 | `OrderCreated` received | Transition `status: AwaitingPayment` | `PaymentRequested` (command) | `CancelOrder` (if downstream timeout) |
| 3a | `PaymentProcessed` received | Transition `status: Paid` | `FulfillmentRequested` (command) | `RefundPayment` (if later step fails) |
| 3b | `PaymentFailed` received | Transition `status: Failed` | `CancelOrder` (command), `NotifyUser` (already auto via Notifications subscribing to PaymentFailed) | `CancelOrder` |
| 4 | `FulfillmentStarted` received | Transition `status: InFulfillment` | — | `CancelShipment` + `RefundPayment` |
| 5a | `OrderShipped` received | Transition `status: Completed` | `CheckoutCompleted` (event) | — (terminal) |
| 5b | `FulfillmentFailed` received | Transition `status: Failed` | `RefundPayment` (command), `CancelOrder` (command) | n/a (compensation path) |
| ∞ | Timeout (no response after N minutes) | Transition `status: TimedOut` | Compensations as appropriate | `CancelOrder`, `RefundPayment` if applicable |

**Implementation guidance:**
- Use **MassTransit** with **Azure Service Bus transport** for saga state machines (`MassTransitStateMachine<CheckoutSagaState>`) — out-of-the-box persistence, timeouts, and idempotency. (Confidence: HIGH — MassTransit is the de facto .NET saga library.)
- **Saga state persistence:** EF Core repository → PostgreSQL table `CheckoutSagas`. Avoid in-memory state for any production-shaped demo.
- **Idempotency:** every event handler must tolerate redelivery (ASB has at-least-once semantics). Use a per-service `ProcessedMessages` table keyed on `MessageId` or rely on MassTransit's in-box pattern.
- **Correlation:** all messages in a saga carry `CorrelationId = CheckoutId`. ASB SQL filters on subscriptions can route by correlation if needed.
- **Timeouts:** schedule a `CheckoutTimeout` message at saga start (15 min, configurable). On receipt, fire compensation chain.

**Confidence:** HIGH on saga structure; HIGH on MassTransit recommendation; MEDIUM on exact compensation policy (this is a business-rule decision the project should refine).

### 3.3 Command vs Event Discipline

Distinguish the two message shapes in the Contracts library:

- **Commands** (imperative, single recipient): `PaymentRequested`, `FulfillmentRequested`, `CancelOrder`, `RefundPayment`. Sent to a service-specific topic/queue, processed by exactly one consumer.
- **Events** (past tense, broadcast): `OrderCreated`, `PaymentProcessed`, `OrderShipped`. Published to an event topic; many subscribers may react.

Encode this in folder structure: `Contracts/Commands/`, `Contracts/Events/`, `Contracts/IntegrationEvents/`.

**Confidence:** HIGH — this command/event split is canonical in EDA literature (Hohpe's Enterprise Integration Patterns, MassTransit docs).

---

## 4. Data Ownership Rules

**Hard rule: one database per service. No shared schemas. No cross-service joins.**

| Service | Store | Why | What Lives Here |
|---|---|---|---|
| Identity | PostgreSQL | Relational, ACID for credentials | Users, Roles, RefreshTokens |
| Catalog | PostgreSQL (read-heavy; add Redis cache later) | Structured product data, joins on category | Products, Categories, Prices, StockLevels |
| Cart | Redis (primary), optional PostgreSQL fallback | Cart is transient, TTL-friendly, low-latency reads | `cart:{userId}` → JSON blob |
| Checkout | PostgreSQL | Saga state must be ACID-durable | CheckoutSagas (state machine instances) |
| Orders | PostgreSQL | System of record; queryable history | Orders, OrderLines, StatusHistory |
| Payments | PostgreSQL | Audit trail required | PaymentAttempts, PaymentResults |
| Fulfillment | PostgreSQL | Workflow state | Shipments, FulfillmentTasks |
| Notifications | PostgreSQL | Inbox + audit log | NotificationLog, Preferences |

**Reference-data denormalization is mandatory.** When Orders creates an order line, it stores `ProductId`, `ProductNameAtPurchase`, `PriceAtPurchase` — it does **not** call Catalog at read time. Catalog price changes do **not** retroactively update orders.

**Confidence:** HIGH on the rule; MEDIUM on Redis-for-Cart vs PostgreSQL-for-Cart (both are defensible — Redis demonstrates polyglot persistence which has portfolio value).

---

## 5. API Gateway

### 5.1 Verdict: YES, an API Gateway is required.

**Why:**
1. **Frontend simplification** — the Angular SPA hits one origin (avoids CORS hell with 8 backend origins).
2. **Cross-cutting concerns** — auth token validation, rate limiting, request logging in one place.
3. **Service relocation** — services can be re-hosted without frontend changes.
4. **TLS termination** — single ingress point.

### 5.2 Recommended Implementation: **YARP (Yet Another Reverse Proxy)**

- Microsoft-maintained .NET reverse proxy; configuration-driven; integrates with ASP.NET Core middleware (auth, rate limiting).
- Lighter than Ocelot, more actively maintained.
- In Azure Container Apps, you can alternatively use the platform's built-in ingress for path-based routing, but YARP gives you a portable, code-reviewable artifact for the portfolio.

**Confidence:** HIGH — YARP is Microsoft's current recommendation for in-process .NET API gateways (Ocelot is now in maintenance mode; this is well documented).

### 5.3 Anti-Pattern to Avoid: BFF Proliferation

Resist building a separate Backend-For-Frontend per channel. With only a web frontend in scope, **one** gateway is sufficient. Add a mobile BFF later if mobile ever ships (currently out of scope per PROJECT.md).

### 5.4 Routing Sketch

```
/api/identity/*       → identity-api
/api/catalog/*        → catalog-api
/api/cart/*           → cart-api
/api/checkout/*       → checkout-api
/api/orders/*         → orders-api
/api/payments/*       → payments-api    (admin/debug only)
/api/fulfillment/*    → fulfillment-api (admin/debug only)
/api/notifications/*  → notifications-api
```

---

## 6. Suggested Build Order

Build in waves. Each wave adds end-to-end value while keeping dependencies satisfied. **Do not** attempt to build all 8 services in parallel.

### Wave 0 — Foundations (no service yet)

1. **Contracts library** (already scaffolded) — define base `IntegrationEvent`, `Command`, `Event` marker types; folder structure for `Commands/` and `Events/`.
2. **Docker Compose baseline** — Postgres, Redis, Azurite or Service Bus emulator (or real ASB dev namespace), Seq/OTel collector for logs.
3. **Shared building blocks** — `MassTransit` config helper, `Result<T>`, problem-details middleware, OpenTelemetry registration.
4. **First ADRs** — ADR-0001 (record decisions), ADR-0002 (Azure Service Bus), ADR-0003 (database per service), ADR-0004 (API Gateway with YARP), ADR-0005 (saga orchestration over choreography).

### Wave 1 — Identity + Catalog (read-only browse path)

1. **Identity API** — register, login, JWT issuance. No events yet.
2. **Catalog API** — products + categories CRUD, seeded data. Publishes `ProductCreated` etc. (subscribers come later).
3. **API Gateway (YARP)** — minimal routes to Identity and Catalog.
4. **Angular shell** — login + browse products. End-to-end browse path works.

**Milestone:** A user can sign up, log in, and browse products. No purchase flow yet.

### Wave 2 — Cart + Orders skeleton

5. **Cart API** — add/remove/view cart (Redis). Frontend wired up.
6. **Orders API** — order entity, status enum, read endpoints. Subscribe to no events yet; just CRUD.

**Milestone:** Cart works. Orders can be created via direct API call (no saga yet).

### Wave 3 — Saga and the core event flow

7. **Payments API** (simulated) — accept `PaymentRequested`, publish `PaymentProcessed`/`PaymentFailed`. Trivial logic (random or deterministic-by-amount).
8. **Checkout API + Saga** — MassTransit state machine; first end-to-end orchestration: `CheckoutStarted` → `OrderCreated` → `PaymentRequested` → `PaymentProcessed` → `CheckoutCompleted`.
9. **Orders** subscribes to `PaymentProcessed` and updates status.

**Milestone:** Click "Place Order" in the UI; saga runs; order shows as Paid. **This is the headline portfolio demo.**

### Wave 4 — Fulfillment and Notifications (close the loop)

10. **Fulfillment API** — subscribe to `PaymentProcessed`; publish `FulfillmentStarted` then `OrderShipped` (timer-based simulation).
11. **Notifications API** — subscribe to `OrderCreated`, `PaymentProcessed`, `OrderShipped`; write to inbox table; expose `GET /notifications`.
12. **Frontend** — order history view; notification inbox.

**Milestone:** Full happy-path checkout end-to-end. Saga can be observed via logs/traces.

### Wave 5 — Hardening (cross-cutting, not a service)

13. Compensation paths (`PaymentFailed` → `CancelOrder`; `FulfillmentFailed` → `RefundPayment`).
14. Idempotency / outbox pattern in each publisher.
15. OpenTelemetry traces stitched across services (correlation IDs).
16. Terraform IaC: parameterize ASB, Container Apps, Postgres flexible servers, Azure Container Registry.
17. CI/CD per service (GitHub Actions matrix).

### Why this order

- **Foundational dependencies first:** auth and product data must exist before anything else makes sense.
- **End-to-end thin slices:** every wave produces a demoable result, not just plumbing.
- **Saga is wave 3, not wave 1:** you need real producers (Cart, Orders) and at least one async participant (Payments) before the saga has anything to orchestrate.
- **Fulfillment + Notifications last because they are pure subscribers:** they need upstream events to consume. Building them earlier means stubbing producers, which is wasted work.

**Confidence:** HIGH on the ordering principle (foundations → producers → orchestrator → subscribers → hardening); MEDIUM on the exact wave boundaries (you may merge waves 1 and 2 if you want a tighter milestone count).

---

## 7. Architecture Decision Records (ADRs)

### 7.1 Format: **MADR 4.0** (Markdown Any Decision Records)

MADR is the chosen format per PROJECT.md. Use the **MADR 4.0 full template** — it is the current canonical structure. (Confidence: HIGH — MADR is the most widely adopted markdown ADR convention; the project has already committed to it.)

**Template:**

```markdown
---
status: "proposed | accepted | rejected | deprecated | superseded by ADR-NNNN"
date: YYYY-MM-DD
deciders: "Fanji"
consulted: "(optional — people whose opinions were sought)"
informed: "(optional — people kept in the loop)"
---

# {short title of decision}

## Context and Problem Statement

What is the issue? Why do we need a decision?

## Decision Drivers

- Driver 1
- Driver 2

## Considered Options

- Option A
- Option B
- Option C

## Decision Outcome

Chosen option: "Option B", because {justification}.

### Consequences

- Good: {positive outcome}
- Bad: {tradeoff accepted}

### Confirmation

How will we know this decision is working? (metrics, review date)

## Pros and Cons of the Options

### Option A
- Good: ...
- Bad: ...

### Option B
- Good: ...
- Bad: ...

## More Information

(links, related ADRs)
```

### 7.2 Placement and Naming

```
docs/
└── adr/
    ├── README.md                                  # index of ADRs
    ├── template.md                                # copy of MADR template
    ├── 0001-record-architecture-decisions.md
    ├── 0002-use-azure-service-bus-for-messaging.md
    ├── 0003-database-per-service.md
    ├── 0004-api-gateway-with-yarp.md
    ├── 0005-saga-orchestration-over-choreography.md
    ├── 0006-masstransit-for-saga-state-machine.md
    ├── 0007-jwt-bearer-auth-via-identity-service.md
    ├── 0008-redis-for-cart-storage.md
    ├── 0009-postgresql-as-default-relational-store.md
    ├── 0010-monorepo-multi-solution-structure.md
    ├── 0011-contracts-library-via-project-reference.md
    └── ...
```

**Naming convention:** four-digit zero-padded sequence + kebab-case slug + `.md`.

**Status transitions:** `proposed` → `accepted` → (optionally) `deprecated` or `superseded by 00NN`. **Never delete or rewrite history** of an accepted ADR — supersede it with a new one.

### 7.3 Tooling

- **Optional CLI:** `adr-tools` (Node) or `log4brains` (npm) — they auto-generate the index and supersede links. Not required; manual works fine for this scale.
- **README.md in `docs/adr/`** — maintain a manual table of contents:

  ```markdown
  | # | Title | Status | Date |
  |---|---|---|---|
  | 0001 | Record architecture decisions | Accepted | 2026-05-30 |
  | 0002 | Use Azure Service Bus for messaging | Accepted | 2026-05-30 |
  ```

### 7.4 When to Write an ADR

Write one whenever you make a decision that:
- Is **hard to reverse** (database choice, messaging platform, language/framework).
- Has **non-obvious tradeoffs** that future-you or a reviewer will question.
- Resolves an **architectural conflict** (e.g., orchestration vs choreography).
- Closes a **research question** raised during roadmap planning.

Skip ADRs for routine implementation choices (which logger library, which test framework — unless those choices are themselves contentious).

**Confidence:** HIGH on MADR format, placement, and lifecycle conventions.

---

## 8. Patterns to Follow

### 8.1 Transactional Outbox

**What:** Persist outgoing integration events in the same database transaction as the domain state change. A separate dispatcher publishes them to ASB.

**Why:** Without the outbox, you can crash between "INSERT INTO Orders" and "publish OrderCreated", leaving the system in an inconsistent state forever. The outbox is the single most important pattern in event-driven systems.

**Implementation:** MassTransit has a built-in **EF Core outbox** — enable it on each service. (Confidence: HIGH.)

```csharp
services.AddMassTransit(x =>
{
    x.AddEntityFrameworkOutbox<OrdersDbContext>(o =>
    {
        o.UsePostgres();
        o.UseBusOutbox();
    });
    x.UsingAzureServiceBus((ctx, cfg) =>
    {
        cfg.Host("Endpoint=sb://...");
        cfg.ConfigureEndpoints(ctx);
    });
});
```

### 8.2 Idempotent Consumers

**What:** Every consumer tolerates the same message arriving twice (ASB is at-least-once).

**How:** Either MassTransit's **in-box** (persisted MessageId dedup table) or domain-level idempotency (`if (order.Status == Paid) return;`).

### 8.3 Correlation IDs Everywhere

**What:** Every message carries a `CorrelationId`; every HTTP request carries `X-Correlation-Id`; every log line includes it.

**Why:** You cannot debug a saga without it. OpenTelemetry handles this automatically when configured.

### 8.4 CQRS-Lite Read Models

**What:** Where read shapes diverge from write shapes (e.g., Orders read view that joins denormalized product names + payment status + shipment tracking), build a dedicated read projection updated by event handlers.

**Where to use it:** Orders read view, Notifications inbox, possibly an Order History query model. **Not** universally — over-application of CQRS is a documented anti-pattern (see §9).

### 8.5 Health Checks + Readiness Probes

Every service exposes `/health/live` and `/health/ready`. Container Apps and Compose both honor these. Include ASB connectivity and DB connectivity in readiness.

### 8.6 OpenTelemetry End-to-End

Configure OTel in each service for traces, metrics, logs. Export to Seq or Jaeger locally; to Azure Monitor in prod. **The saga is unobservable without this.**

---

## 9. Anti-Patterns to Avoid

### 9.1 Anti-Pattern: Shared Database
**What:** Two services pointing at the same Postgres schema.
**Why bad:** Couples deployments, blocks independent scaling, leaks domain concepts across boundaries. Defeats the entire point of microservices.
**Instead:** Database per service; integrate via events; denormalize freely.

### 9.2 Anti-Pattern: Synchronous Service-to-Service HTTP for Business Operations
**What:** Checkout makes a blocking HTTP call to Payments and waits.
**Why bad:** Cascading failures; latency stacking; tight coupling; defeats the async event-driven model the project is meant to demonstrate.
**Instead:** Publish a command on ASB. Use HTTP only for **read** queries from the gateway, not for cross-service business operations.

### 9.3 Anti-Pattern: Distributed Transactions / Two-Phase Commit
**What:** Attempting to span an ACID transaction across services.
**Why bad:** Not supported by ASB + Postgres; even where supported (MSDTC), it creates fragile, slow systems.
**Instead:** Saga + compensating actions. Embrace eventual consistency.

### 9.4 Anti-Pattern: Choreography for the Checkout Flow
**What:** No orchestrator; each service reacts to others' events in a chain.
**Why bad:** Implicit ordering, hard to reason about, hard to debug, hard to demo, no place to hang timeouts and compensation.
**Instead:** Orchestrated saga in the Checkout service. (Choreography is fine for one-off side effects like "send a welcome email when a user registers" — but the multi-step checkout deserves explicit orchestration.)

### 9.5 Anti-Pattern: Contracts Library That Imports Domain Code
**What:** The shared `Contracts` project depends on `Orders.Domain` so it can serialize `Order` directly.
**Why bad:** Every service now transitively depends on Orders. Coupling explosion.
**Instead:** `Contracts` contains **only DTOs and message types** — primitives + collections. Domain entities never leave their service.

### 9.6 Anti-Pattern: Per-Service NuGet Versioning of Contracts (at this scale)
**What:** Publishing `Contracts` to a private feed and pinning versions per service.
**Why bad:** For a 1-person portfolio project, this is overhead that produces no value. Project reference via relative path is correct here (and is already the chosen approach per PROJECT.md).
**Instead:** Project reference. Revisit if a second team ever joins.

### 9.7 Anti-Pattern: Catalog as the Inventory System of Record for Reservations
**What:** Checkout calls Catalog to "reserve" stock during checkout.
**Why bad:** Reservation is a workflow concern with TTLs and compensation; it does not belong in the product master.
**Instead:** Either (a) defer real inventory reservation to v2 and let Catalog only expose snapshot stock levels, or (b) introduce an Inventory bounded context. For this portfolio, **(a)** is correct — keep Catalog read-mostly.

### 9.8 Anti-Pattern: One Topic Per Event
**What:** Creating `order-created-topic`, `order-shipped-topic`, etc.
**Why bad:** ASB topic sprawl; subscription management nightmare.
**Instead:** One topic per **producing context** (`order-events`, `payment-events`), with subscribers using **SQL filters** on a `MessageType` user property. (Confidence: HIGH — this is Microsoft's documented ASB topology guidance.)

---

## 10. Scalability Considerations

| Concern | At demo scale (≤100 req/min) | At 10K users | At 1M users |
|---|---|---|---|
| Compute | Container Apps min=0, max=1 per service | Container Apps min=1, max=5, scale on HTTP concurrency / queue depth | KEDA-based autoscale on ASB queue length; per-region deployments |
| Database | Single Postgres flexible-server (Burstable B1ms) | Move to General Purpose; read replicas for Catalog | Sharding for Orders by userId hash; CQRS read stores in Cosmos |
| ASB tier | Standard (topics required; Basic does not support them) | Standard | Premium (dedicated capacity, geo-replication) |
| Cart store | Redis single node | Redis Cache Standard tier | Redis Enterprise / clustering |
| Observability | Seq locally, App Insights in Azure | App Insights with sampling | Dedicated Log Analytics workspace, sampling at 1-5% |

**Confidence:** HIGH on tiering principles; MEDIUM on exact thresholds (workload-dependent).

**Note:** ASB **Basic tier does not support topics**, only queues. Use **Standard or Premium** for this project. (Confidence: HIGH — explicit in Azure ASB pricing documentation.)

---

## 11. Build Order Implications for Roadmap Phases

Translating §6 into roadmap phase guidance:

| Roadmap Phase | Includes | Why this phase |
|---|---|---|
| **P1 — Foundations** | Contracts shapes, Docker Compose, shared building blocks, first 5 ADRs | Everything else assumes these exist |
| **P2 — Identity + Catalog + Gateway** | First two services, YARP, Angular shell with login + browse | Smallest end-to-end vertical slice |
| **P3 — Cart + Orders skeleton** | Cart (Redis), Orders CRUD, frontend wired | Required producers for the saga |
| **P4 — Checkout Saga + Payments** | MassTransit state machine, simulated Payments, happy-path orchestration | **Headline demo** — the central portfolio artifact |
| **P5 — Fulfillment + Notifications** | Subscribers close the loop; order history + inbox UI | Demonstrates pure event-consumer pattern |
| **P6 — Hardening** | Compensations, outbox/idempotency rigor, OTel traces, Terraform, CI/CD | Production-shape polish |

**Research flags for phases:**
- **P4 (Saga)** will need a focused research pass on **MassTransit + ASB state machine specifics** (saga repository config, scheduling/timeouts in ASB, in-box configuration). Flag this for a phase-research subagent.
- **P6 (Terraform + Container Apps)** will need a focused research pass on **Azure Container Apps + ASB managed identity wiring, KEDA scalers, Terraform azurerm provider current version**.
- Other phases should run on standard patterns and not need bespoke research.

---

## 12. Open Questions for Phase-Level Research

These are deliberately deferred to phase-research subagents rather than answered now:

1. **Cart store:** Redis vs PostgreSQL — defer to P3; both work; choice depends on whether polyglot persistence is a portfolio goal.
2. **Read-side projection technology for Orders history:** Materialized views in Postgres vs a dedicated CQRS store — defer to P5.
3. **Inventory reservation:** Out of scope for v1 per PROJECT.md; revisit when/if a real checkout failure scenario demands it.
4. **Schema evolution policy for Contracts:** Additive-only with deprecation? Versioned namespaces? Defer to P6 hardening.
5. **Dead-letter queue handling and replay tooling:** Defer to P6.

---

## 13. Sources

All recommendations in this document are grounded in:

- **Microsoft .NET Microservices: Architecture for Containerized .NET Applications** (Cesar de la Torre et al.) — canonical reference for the eShopOnContainers patterns; bounded contexts, integration events, API gateway, saga. **Confidence: HIGH.**
- **eShopOnContainers / eShop** reference implementation (github.com/dotnet-architecture/eShopOnContainers and the newer dotnet/eShop) — concrete implementation of all the above patterns in .NET. **Confidence: HIGH.**
- **Chris Richardson — microservices.io** — definitive catalogue of saga, outbox, CQRS, database-per-service patterns. **Confidence: HIGH.**
- **Vaughn Vernon — Implementing Domain-Driven Design** — bounded context mapping, aggregates, integration patterns. **Confidence: HIGH.**
- **Eric Evans — Domain-Driven Design** — strategic design, context maps. **Confidence: HIGH.**
- **MassTransit documentation** (masstransit.io) — saga state machine, outbox, idempotency, ASB transport. **Confidence: HIGH.**
- **Microsoft Azure Service Bus documentation** — topics/subscriptions, SQL filters, tier limits. **Confidence: HIGH.**
- **MADR (Markdown Any Decision Records)** (adr.github.io / madr/madr on GitHub) — the format committed to in PROJECT.md. **Confidence: HIGH.**
- **YARP documentation** (microsoft.github.io/reverse-proxy) — Microsoft's current recommended .NET reverse proxy. **Confidence: HIGH.**

**Caveat:** Web access was unavailable in this research session, so URLs above are not fetched live. All patterns and recommendations are based on long-established, stable references that have not materially changed in the past 5+ years. The roadmap-creation step should still validate version-specific details (current MassTransit major version, current ASB SDK version, current YARP release) at implementation time.
