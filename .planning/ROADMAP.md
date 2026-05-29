# Roadmap: ECommerce Platform

**Created:** 2026-05-30
**Mode:** mvp (vertical slices — each phase delivers a working, demo-able increment)
**Granularity:** coarse
**Core Value:** A working checkout saga that spans Catalog, Cart, Orders, Payments, Fulfillment, and Notifications — demonstrating event-driven coordination between microservices without direct coupling.

---

## Phases

- [ ] **Phase 1: Foundations** — Lock contracts shape, Compose baseline, Aspire AppHost, OpenTelemetry, and first 8 ADRs
- [ ] **Phase 2: Identity, Catalog & Gateway** — User can register/login and browse catalog through YARP gateway with outbox/inbox wired from day one
- [ ] **Phase 3: Cart & Orders Skeleton** — Per-user Redis cart and Orders aggregate with CQRS read model and state machine
- [ ] **Phase 4: Checkout Saga & Payments** — Headline demo: place order triggers saga, deterministic failure shows live compensation
- [ ] **Phase 5: Fulfillment & Notifications** — Complete happy-path and compensation flows end-to-end with in-app notification inbox
- [ ] **Phase 6: Hardening & Azure Deployment** — Terraform-provisioned Azure infrastructure with remote state, dev/prod environments, and production-grade resilience

---

## Phase Details

### Phase 1: Foundations
**Goal:** Lock the contracts shape, local orchestration baseline, observability, and core architectural decisions so every later phase builds on a stable, traceable foundation.
**Mode:** mvp
**Needs Research:** no
**Depends on:** Nothing (first phase)
**Requirements:** REPO-01, REPO-02, REPO-03, CON-01, CON-02, CON-03, ADR-01, ADR-02, INF-03, INF-04
**Success Criteria:**
1. Each service has its own `.sln` file openable independently in Visual Studio, with the shared `Contracts` project referenced by relative path (not NuGet) from every solution.
2. The Contracts library compiles as pure C# records with envelope fields (`MessageId`, `CorrelationId`, `CausationId`, `OccurredAt`) and `.V1` namespaces per producing service, enforced with no EF/MediatR/domain logic dependencies.
3. `docker compose up` (generated from .NET Aspire AppHost via `aspire publish`) brings up Postgres, Redis, ASB emulator, and a stub health-checked service that emits OpenTelemetry traces visible in the Aspire dashboard.
4. At least 8 MADR-format ADRs exist in `docs/adr/` (numbered kebab-case) covering ASB choice, DB-per-service, YARP gateway, saga orchestration, MassTransit + outbox/inbox, ASB topic-per-context, MADR format, and mono-repo structure.
5. Every scaffolded service exposes a `GET /health` endpoint returning 200 and structured logs carry a correlation ID across simulated service boundaries.
**Plans:** TBD

### Phase 2: Identity, Catalog & Gateway
**Goal:** A user can register, log in, and browse a seeded catalog through a YARP gateway, with MassTransit transactional outbox and idempotent inbox wired into the first services.
**Mode:** mvp
**Needs Research:** no
**Depends on:** Phase 1
**Requirements:** IDN-01, IDN-02, IDN-03, IDN-04, CAT-01, CAT-02, CAT-03, FE-01, FE-04, INF-01, INF-02
**Success Criteria:**
1. User can POST to `/register` to create an account, POST to `/login` to receive a JWT (issued by OpenIddict), and GET `/me` to retrieve their profile.
2. User can list products via `GET /products` with pagination and category filtering, and view a single product via `GET /products/{id}`, against a seeded catalog of 20–50 SKUs with price and stock.
3. Angular shell at `/catalog`, `/product/:id`, `/login`, `/register` renders products and authenticates users end-to-end through the YARP gateway.
4. Catalog service publishes a domain event (e.g. `ProductViewed` or `CatalogSeeded`) through MassTransit transactional outbox and the consuming side deduplicates redelivered messages via idempotent inbox — verified with a forced redelivery test.
5. Seeded demo user accounts allow the demo to run without manual registration.
**Plans:** TBD
**UI hint:** yes

### Phase 3: Cart & Orders Skeleton
**Goal:** A logged-in user can build a per-user cart with price snapshots and view their orders history through a CQRS read model backed by an Orders state machine — no checkout yet.
**Mode:** mvp
**Needs Research:** no
**Depends on:** Phase 2
**Requirements:** CART-01, CART-02, CART-03, CART-04, ORD-01, ORD-02, ORD-03, ORD-04, FE-02
**Success Criteria:**
1. User can add, remove, and update cart item quantities at `/cart`, with each line item capturing the product price at the time of addition (preventing price drift on later catalog changes).
2. User can view a cart summary with line totals and grand total, and the cart is cleared after a (seeded or test-triggered) checkout-complete event.
3. User can list their order history via `GET /orders` and view a single order detail via `GET /orders/{id}` showing line items and current status.
4. Order status transitions strictly follow the state machine Pending → Paid → Fulfilled / Cancelled / Failed, enforced by the Orders aggregate.
5. Order queries are served from a CQRS read-model projection built from domain events (not from the write-side aggregate), verified by inspecting the projection table populated by event handlers.
**Plans:** TBD
**UI hint:** yes

### Phase 4: Checkout Saga & Payments
**Goal:** A user clicks "Place Order" and the checkout saga orchestrates Order creation, simulated payment, and compensation paths end-to-end — the headline demo.
**Mode:** mvp
**Needs Research:** yes
**Depends on:** Phase 3
**Requirements:** CHK-01, CHK-02, CHK-03, CHK-04, CHK-05, PAY-01, PAY-02, PAY-03, FE-03
**Success Criteria:**
1. User can POST `/checkout` and receive a `202 Accepted` with a `checkoutId`, then GET `/checkout/{id}` to poll current saga / order status updated in real time via Angular `/checkout` and `/orders/:id` pages.
2. Happy path: a normal-priced cart drives the saga from `Started` → `AwaitingPayment` → `Paid` and produces an order in `Paid` status with idempotent payment processing keyed by `checkoutId`.
3. Demo-trigger failure: a cart total ending in `.99` deterministically triggers `PaymentFailed`, the saga compensates by cancelling the order, and the order status transitions to `Cancelled` / `Failed`.
4. Fulfillment-failure compensation (simulated by a test trigger or seeded condition) causes the saga to publish `RefundPayment` and `CancelOrder`, leaving the system in a consistent terminal state.
5. A checkout left incomplete for ~15 minutes triggers a saga timeout that cascades the same compensation path as an explicit failure, leaving no orphaned orders or payments.
**Plans:** TBD
**UI hint:** yes

### Phase 5: Fulfillment & Notifications
**Goal:** The saga reaches a fully-shipped terminal state and the user sees the entire lifecycle reflected in an in-app notification inbox.
**Mode:** mvp
**Needs Research:** no
**Depends on:** Phase 4
**Requirements:** FUL-01, FUL-02, NOT-01, NOT-02
**Success Criteria:**
1. Fulfillment service consumes `OrderPaid` events, advances status through a timer-based simulation, and publishes `OrderShipped` — visible in the saga state and the order detail view.
2. After `OrderShipped`, the saga reaches `Completed` and the order detail page reflects `Fulfilled` status without manual intervention.
3. User can GET `/notifications` to view an in-app inbox containing entries for the saga lifecycle events they participated in (`OrderPaid`, `OrderShipped`, `PaymentFailed`).
4. Notifications service idempotently consumes saga events from the producing-context topics and persists inbox entries, verified by a forced-redelivery test producing no duplicate inbox rows.
**Plans:** TBD

### Phase 6: Hardening & Azure Deployment
**Goal:** The platform deploys to Azure via Terraform with remote state and separate dev/prod environments, demonstrating production-grade IaC for a microservices system.
**Mode:** mvp
**Needs Research:** yes
**Depends on:** Phase 5
**Requirements:** IAC-01, IAC-02, IAC-03
**Success Criteria:**
1. `terraform apply` in `infra/environments/dev` provisions all required Azure resources end-to-end: Azure Service Bus (Standard tier with topics/subscriptions), Azure Container Apps for every service, Key Vault, PostgreSQL flexible server, and Redis.
2. Terraform remote state is stored in an Azure Storage Account from the first apply (no local state files committed), with state locking verified by a concurrent-apply test.
3. Separate Terraform configurations exist for `infra/environments/dev` and `infra/environments/prod`, sharing reusable modules under `infra/modules/`, with environment-specific variables isolated per environment.
4. The deployed dev environment successfully runs the headline checkout-saga demo end-to-end against real Azure Service Bus (not the emulator), with Managed Identity used for secret retrieval from Key Vault.
**Plans:** TBD

---

## Progress

| Phase | Plans Complete | Status | Completed |
|-------|----------------|--------|-----------|
| 1. Foundations | 0/? | Not started | - |
| 2. Identity, Catalog & Gateway | 0/? | Not started | - |
| 3. Cart & Orders Skeleton | 0/? | Not started | - |
| 4. Checkout Saga & Payments | 0/? | Not started | - |
| 5. Fulfillment & Notifications | 0/? | Not started | - |
| 6. Hardening & Azure Deployment | 0/? | Not started | - |

---

## Coverage

**v1 requirements:** 46 total
**Mapped:** 46
**Unmapped:** 0

Every v1 requirement maps to exactly one phase. See `REQUIREMENTS.md` ## Traceability for the per-requirement mapping.

---

*Roadmap created: 2026-05-30*
