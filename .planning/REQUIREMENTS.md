# Requirements: ECommerce Platform

**Defined:** 2026-05-30
**Core Value:** A working checkout saga that spans Catalog, Cart, Orders, Payments, Fulfillment, and Notifications — demonstrating event-driven coordination between microservices without direct coupling.

## v1 Requirements

### Catalog

- [ ] **CAT-01**: User can list products with pagination and category filtering
- [ ] **CAT-02**: User can view product detail by ID
- [ ] **CAT-03**: System provides seeded demo catalog of 20–50 SKUs with price and stock

### Cart

- [ ] **CART-01**: User can add, remove, and update cart item quantities
- [ ] **CART-02**: Cart captures product price at time of item addition (prevents price drift)
- [ ] **CART-03**: User can view cart summary with line totals and grand total
- [ ] **CART-04**: Cart is cleared after checkout completes

### Identity

- [ ] **IDN-01**: User can register with email and password
- [ ] **IDN-02**: User can log in and receive a JWT token (via OpenIddict)
- [ ] **IDN-03**: User can retrieve their current profile (GET /me)
- [ ] **IDN-04**: System provides seeded demo user accounts for demos

### Orders

- [ ] **ORD-01**: User can list their order history
- [ ] **ORD-02**: User can view order detail with line items and current status
- [ ] **ORD-03**: Order status follows state machine: Pending → Paid → Fulfilled / Cancelled / Failed
- [ ] **ORD-04**: Order queries are served via a CQRS read model (projection from domain events)

### Checkout & Saga

- [ ] **CHK-01**: User can initiate checkout and receive a checkoutId (202 Accepted)
- [ ] **CHK-02**: User can poll checkout / order status via GET /checkout/{id}
- [ ] **CHK-03**: Saga compensates on PaymentFailed — cancels the order
- [ ] **CHK-04**: Saga compensates on FulfillmentFailed — refunds payment and cancels order
- [ ] **CHK-05**: Saga times out after ~15 minutes if not completed (compensation triggered)

### Payments

- [ ] **PAY-01**: Simulated payment service processes AuthorisePayment commands
- [ ] **PAY-02**: Amounts ending in `.99` deterministically trigger PaymentFailed (demo trigger)
- [ ] **PAY-03**: Payment processing is idempotent by checkoutId

### Fulfillment

- [ ] **FUL-01**: Fulfillment service consumes OrderPaid events and starts processing
- [ ] **FUL-02**: Fulfillment publishes OrderShipped after timer-based processing simulation

### Notifications

- [ ] **NOT-01**: User can view their in-app notification inbox (GET /notifications)
- [ ] **NOT-02**: Notifications service consumes saga events (OrderPaid, OrderShipped, PaymentFailed) and persists inbox entries

### Contracts

- [x] **CON-01**: Shared Contracts library defines all ASB message types as pure C# records (no domain logic, no EF, no MediatR)
- [x] **CON-02**: Messages include envelope fields: MessageId, CorrelationId, CausationId, OccurredAt
- [x] **CON-03**: Messages are namespaced per producing service with `.V1` suffix (e.g. `ECommerce.Catalog.Events.V1`)

### Infrastructure & Cross-Cutting

- [ ] **INF-01**: Every publishing service uses MassTransit transactional outbox for guaranteed at-least-once delivery
- [ ] **INF-02**: Every consuming service uses idempotent inbox to deduplicate redelivered messages
- [x] **INF-03**: All services emit OpenTelemetry traces and structured logs with correlation ID across service boundaries
- [x] **INF-04**: All services expose GET /health endpoints for readiness and liveness probes

### Architecture Decision Records

- [x] **ADR-01**: ADRs follow MADR 4.0 format, stored in docs/adr/ with numbered kebab-case filenames
- [x] **ADR-02**: Minimum 8 ADRs written during Phase 1 (covering: ASB choice, DB-per-service, YARP gateway, saga orchestration, MassTransit + outbox, ASB topic design, MADR format, mono-repo structure); total ~26 by Phase 6

### Infrastructure as Code

- [ ] **IAC-01**: Terraform provisions all Azure resources: ASB Standard tier, Container Apps, Key Vault, PostgreSQL, Redis
- [ ] **IAC-02**: Terraform remote state stored in Azure Storage Account from day one
- [ ] **IAC-03**: Separate Terraform environments for dev and prod (infra/environments/dev | prod)

### Mono-Repo Structure

- [x] **REPO-01**: Each service has its own .sln file (independently openable in Visual Studio)
- [x] **REPO-02**: All service solutions reference the shared Contracts project via relative path (not NuGet)
- [x] **REPO-03**: Local orchestration via Docker Compose (generated from .NET Aspire AppHost via `aspire publish`)

### Angular Frontend

- [ ] **FE-01**: User can browse the product catalog and view product detail (/catalog, /product/:id)
- [ ] **FE-02**: User can manage their cart (/cart)
- [ ] **FE-03**: User can complete checkout and see order status updating in real-time via polling (/checkout, /orders/:id)
- [ ] **FE-04**: User can register and log in (/register, /login)

---

## v2 Requirements

### Payments
- **PAY-V2-01**: Real Stripe payment integration with test mode

### Notifications
- **NOT-V2-01**: Mark notifications as read (PATCH /notifications/{id}/read)
- **NOT-V2-02**: Real email delivery (SMTP / SendGrid)

### Frontend
- **FE-V2-01**: Order history list page (/orders)
- **FE-V2-02**: Real-time updates via WebSockets / SignalR (replace polling)

### Operations
- **OPS-V2-01**: demo.ps1 — self-driving demo script (seed data, place order, trigger failure, show compensation)
- **OPS-V2-02**: Dead-letter queue monitoring and replay tooling

### Admin
- **ADM-V2-01**: Admin dashboard for product CRUD and order management

---

## Out of Scope

| Feature | Reason |
|---------|--------|
| Real payment processor (Stripe) | Avoids credential management; pattern demonstrated with simulation |
| Admin product CRUD | Seed data sufficient for demo; admin UI adds scope without adding pattern value |
| Product search engine (Elasticsearch) | Basic category filtering sufficient for portfolio; search adds infra complexity |
| Product reviews, ratings, wishlists | Not core to the saga demo story |
| Real email / SMS | In-app inbox demonstrates the pattern; real SMTP is operational cost |
| WebSockets / SignalR | Polling is sufficient for v1 order status; real-time is v2 |
| Mobile app | Web-first; Angular covers the demo |
| Multi-currency, i18n, multi-tenancy | Out of scope for portfolio project |
| Inventory reservation | Complex distributed pattern; deferred beyond v1 |
| Event sourcing on Order aggregate | Possible differentiator but one strong feature per phase; scope discipline |

---

## Traceability

| Requirement | Phase | Status |
|-------------|-------|--------|
| REPO-01 | Phase 1 | Complete |
| REPO-02 | Phase 1 | Complete |
| REPO-03 | Phase 1 | Complete |
| CON-01 | Phase 1 | Complete |
| CON-02 | Phase 1 | Complete |
| CON-03 | Phase 1 | Complete |
| ADR-01 | Phase 1 | Complete |
| ADR-02 | Phase 1 | Complete |
| INF-03 | Phase 1 | Complete |
| INF-04 | Phase 1 | Complete |
| IDN-01 | Phase 2 | Pending |
| IDN-02 | Phase 2 | Pending |
| IDN-03 | Phase 2 | Pending |
| IDN-04 | Phase 2 | Pending |
| CAT-01 | Phase 2 | Pending |
| CAT-02 | Phase 2 | Pending |
| CAT-03 | Phase 2 | Pending |
| FE-01 | Phase 2 | Pending |
| FE-04 | Phase 2 | Pending |
| INF-01 | Phase 2 | Pending |
| INF-02 | Phase 2 | Pending |
| CART-01 | Phase 3 | Pending |
| CART-02 | Phase 3 | Pending |
| CART-03 | Phase 3 | Pending |
| CART-04 | Phase 3 | Pending |
| ORD-01 | Phase 3 | Pending |
| ORD-02 | Phase 3 | Pending |
| ORD-03 | Phase 3 | Pending |
| ORD-04 | Phase 3 | Pending |
| FE-02 | Phase 3 | Pending |
| CHK-01 | Phase 4 | Pending |
| CHK-02 | Phase 4 | Pending |
| CHK-03 | Phase 4 | Pending |
| CHK-04 | Phase 4 | Pending |
| CHK-05 | Phase 4 | Pending |
| PAY-01 | Phase 4 | Pending |
| PAY-02 | Phase 4 | Pending |
| PAY-03 | Phase 4 | Pending |
| FE-03 | Phase 4 | Pending |
| FUL-01 | Phase 5 | Pending |
| FUL-02 | Phase 5 | Pending |
| NOT-01 | Phase 5 | Pending |
| NOT-02 | Phase 5 | Pending |
| IAC-01 | Phase 6 | Pending |
| IAC-02 | Phase 6 | Pending |
| IAC-03 | Phase 6 | Pending |

**Coverage:**
- v1 requirements: 46 total
- Mapped to phases: 46
- Unmapped: 0

---
*Requirements defined: 2026-05-30*
*Last updated: 2026-05-30 after roadmap creation (traceability re-confirmed against ROADMAP.md)*
