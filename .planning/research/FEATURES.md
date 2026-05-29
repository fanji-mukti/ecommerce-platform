# Feature Landscape

**Domain:** Event-driven e-commerce platform (microservices portfolio / learning project)
**Researched:** 2026-05-30
**Confidence:** MEDIUM overall — external research tools (WebSearch / WebFetch / Context7 / Brave) were unavailable for this run, so findings draw on training-data knowledge of the .NET reference applications (eShopOnContainers, dotnet/eShop), DDD literature (Vernon, Evans), and common e-commerce domain patterns. Items derived from those well-known references are MEDIUM; novel claims about market expectations are LOW.

---

## Framing: What Is This Project Optimised For?

This is a **portfolio / learning** project, not a commercial product. That changes the feature calculus:

- **"Table stakes" = what a reviewer (interviewer, tech lead, hiring manager) needs to see to believe the demo is a real working system end-to-end.** Missing these makes the demo look broken or trivial.
- **"Differentiators" = what makes the codebase memorable and signals seniority.** These are *pattern* features (saga, outbox, DDD aggregates done well), not *product* features (wishlists, reviews).
- **"Anti-features" = features that consume time without proving anything new about the architecture.** Build to demonstrate the patterns, not to ship a Shopify clone.

Optimisation order: **patterns first, breadth second, polish third.**

---

## Table Stakes

Features required for the demo to be coherent end-to-end. Missing these breaks the core value (working checkout saga across all eight services).

### Product Catalog
| Feature | Why Expected | Complexity | Notes |
|---------|--------------|------------|-------|
| List products with paging | Reviewer needs something to put in a cart | Low | Read-only API, simple EF Core query |
| Get product by ID | Required by Cart and Checkout to validate items | Low | — |
| Browse by category / basic filter | Demonstrates query model; without it the catalog looks like a flat list | Low | One category dimension is enough |
| Seed data (20-50 realistic SKUs with images) | A demo with 3 products looks unfinished | Low | Static seed file or migration |
| Price + currency on product | Required by Cart total calc and Payment amount | Low | Single currency for v1 |
| Stock level (read-only display) | Required so Checkout can fail realistically when out of stock | Low | Updated by inventory events — see Differentiators |

### Cart
| Feature | Why Expected | Complexity | Notes |
|---------|--------------|------------|-------|
| Add item to cart | Core flow | Low | — |
| Remove item / update quantity | Core flow | Low | — |
| Get current cart | Required by Checkout | Low | — |
| Persist cart per user (authenticated) | Otherwise a refresh blows away state and the demo feels fake | Low | Keyed by user id; Redis or DB |
| Cart line snapshot (price captured at add time) | DDD-correct: cart is its own aggregate, not a join to Catalog | Low-Med | Demonstrates aggregate boundary thinking |

### Checkout
| Feature | Why Expected | Complexity | Notes |
|---------|--------------|------------|-------|
| Initiate checkout from cart | Entry point to the saga — the whole project's centrepiece | Med | Publishes `CheckoutInitiated` |
| Collect shipping address | Without an address the saga has nothing to hand to Fulfillment | Low | Single address; no address book needed |
| Show order summary before confirm | Standard UX; lets the demo "pause" so the reviewer can see state | Low | — |
| Confirm + trigger saga | Core flow | Med | Hand-off to process manager |

### Orders
| Feature | Why Expected | Complexity | Notes |
|---------|--------------|------------|-------|
| Create order from successful checkout | Core flow | Low | Driven by saga, not direct API call |
| Order state machine (Pending → Paid → Fulfilled / Cancelled / Failed) | The saga is invisible without an order whose status visibly progresses | Med | This *is* the demo's storytelling device |
| Get order by ID | Reviewer needs to see the order they just placed | Low | — |
| List orders for current user | "Order history" page — table-stakes for any e-commerce UX | Low | — |

### Identity / Auth
| Feature | Why Expected | Complexity | Notes |
|---------|--------------|------------|-------|
| Register account (email + password) | Cart and Orders are per-user; anonymous demo is incoherent | Low | — |
| Login / logout, issue JWT or cookie | All authenticated APIs need this | Low-Med | ASP.NET Core Identity + JWT bearer is the path of least resistance |
| Get current user profile | Frontend needs `whoami` | Low | — |
| Demo / seeded test users | So a reviewer can log in instantly | Low | Print credentials in README |

### Payments (Simulated)
| Feature | Why Expected | Complexity | Notes |
|---------|--------------|------------|-------|
| Authorise + capture (single step is fine for v1) | Saga needs a payment step | Low | Simulated success/failure |
| Deterministic failure trigger (e.g. amount `$99.99` always declines) | Reviewer must be able to *demonstrate* the compensation path on demand | Low | Critical for showing saga compensation |
| Publish `PaymentSucceeded` / `PaymentFailed` events | Drives the saga | Low | — |
| Idempotent payment request (by checkout/order id) | Avoid double-charge on saga retry — demonstrates correct event-driven thinking | Med | Important pattern signal |

### Fulfillment
| Feature | Why Expected | Complexity | Notes |
|---------|--------------|------------|-------|
| Consume `OrderPaid` event, create fulfillment record | Saga step | Low | — |
| Status progression (Allocated → Picked → Shipped) — even if simulated via timer | Without movement, "fulfillment" looks like a stub | Low-Med | Use a hosted service / timer to advance status |
| Publish `OrderShipped` event | Closes the saga, triggers Notifications | Low | — |

### Notifications
| Feature | Why Expected | Complexity | Notes |
|---------|--------------|------------|-------|
| Consume domain events (`OrderPlaced`, `PaymentFailed`, `OrderShipped`) | The "across-services" reach is what proves event-driven architecture | Low | — |
| Render notification (log to console + persist to "inbox" table) | Don't need real email — but need *visible* output the reviewer can inspect | Low | A per-user inbox endpoint is enough; SMTP optional |
| Notification preferences read model | Demonstrates eventual consistency without complexity | Low | Optional but cheap |

### Cross-cutting (table stakes)
| Feature | Why Expected | Complexity | Notes |
|---------|--------------|------------|-------|
| Health check endpoints on every service | Docker Compose / Container Apps probes; reviewer expects `/health` | Low | `Microsoft.Extensions.Diagnostics.HealthChecks` |
| OpenAPI / Swagger per service | Without it the APIs are undiscoverable | Low | Built-in to ASP.NET Core |
| Structured logging with correlation id propagated across services | If you can't trace one checkout across 6 services, the demo is unfalsifiable | Med | Serilog + W3C trace context + ASB property propagation |
| README per service + root README with run instructions | "Clone and `docker compose up`" must work | Low | Non-negotiable for portfolio |

### Frontend (Angular — minimal)
| Feature | Why Expected | Complexity | Notes |
|---------|--------------|------------|-------|
| Catalog browse page | Entry point | Low | — |
| Product detail page | Add-to-cart action surface | Low | — |
| Cart page | Review before checkout | Low | — |
| Checkout flow (address + confirm) | Triggers the saga | Med | — |
| Login / register pages | Auth gate | Low | — |
| Order history + order detail (with live-ish status) | This is *the page that sells the architecture*: status changes as events fire | Med | Poll every 2–3s, or push later as differentiator |

---

## Differentiators

What makes this codebase stand out in a portfolio. These signal seniority. Most are *pattern* features, not product features — that is the point.

| Feature | Value Proposition | Complexity | Notes |
|---------|-------------------|------------|-------|
| **Checkout saga as an explicit process manager / state machine** | The headline. Use a named class (e.g. `CheckoutSaga`) with explicit states, not implicit event-chain spaghetti | High | Consider MassTransit Saga State Machine or hand-rolled with persisted state. Document choice in ADR. |
| **Transactional outbox pattern in services that publish events** | Solves the dual-write problem — the single most-asked DDD/event-driven interview question | High | One outbox implementation, reused across services. Big signal of seniority. |
| **Idempotent message consumers (inbox / message-id dedupe)** | Pairs with outbox; shows you understand at-least-once delivery | Med | Required for honest event-driven, not optional |
| **Compensating actions on saga failure (release inventory, refund, cancel order)** | Demonstrates the *reason* sagas exist, not just the mechanism | High | Build at least one compensation path end-to-end |
| **DDD aggregates with invariants enforced inside the aggregate (not in services)** | Distinguishes "DDD in name" from "DDD in practice" | Med | Order aggregate is the obvious one. Document invariants in code comments. |
| **Bounded context map / context diagram in `docs/`** | Lets a reviewer understand the architecture in 60 seconds | Low | One PNG / mermaid diagram. Cheap, huge impact. |
| **Value objects for Money, Address, Email, ProductId** | Visible, repeatable DDD signal throughout the codebase | Low-Med | Cheapest "looks senior" win in the project |
| **CQRS-style read model in Orders or Notifications** | "Write model publishes events, read model projects them" — classic eventual-consistency demo | Med | Don't go full event sourcing; just a projection. |
| **Dead-letter queue handling with a visible UI or endpoint** | Shows you've thought about failure modes, not just happy path | Med | Even a "DLQ inspector" endpoint is enough |
| **Correlation id surfaced in the order-detail page** | Reviewer can copy it, grep logs, and see the whole saga trace — incredibly compelling | Low | Tiny effort, large payoff |
| **OpenTelemetry traces across all services (export to Jaeger / Aspire dashboard locally)** | A single trace spanning 6 services on checkout is a "wow" moment | Med-High | .NET 10 + OpenTelemetry is well-supported; .NET Aspire dashboard is the easy path |
| **Per-service database (real polyglot persistence) — even just Postgres + Redis + Cosmos emulator** | Demonstrates "shared-nothing" between services properly | Med | Don't share a DB across services; that's the #1 microservices smell |
| **ADRs for every meaningful decision (saga choice, outbox impl, ASB topic strategy, DB-per-service)** | Already in the plan — make sure each one is genuinely a *decision*, not a description | Low | Reviewers read ADRs first |
| **Smoke-test / contract test suite that runs against `docker compose up`** | Proves "it works" beyond a README claim | Med | Even 5 Playwright / xUnit integration tests covering happy path + one compensation path |
| **A scripted "demo run" (`make demo` or `pwsh ./demo.ps1`) that seeds data, places an order, triggers a payment failure, and shows the compensation** | This is the single highest-leverage feature for a portfolio | Low-Med | A 90-second self-driving demo beats any README. |

### Differentiators worth strongly considering (one-pick)

Pick *one* of these; doing all three dilutes focus:

| Feature | Why | Complexity |
|---------|-----|------------|
| **Event sourcing on the Order aggregate** | Showcases the "lite" event sourcing — full event log per order | High |
| **Inventory reservation service with TTL holds** | Adds a second non-trivial saga step (reserve → confirm or release) | Med-High |
| **A second saga (e.g. refund flow) to prove the pattern generalises** | Shows the architecture isn't a one-trick pony | High |

---

## Anti-Features (Deliberately NOT Building for v1)

Each of these is tempting and each one is a trap for a portfolio scoped at one person's time.

| Anti-Feature | Why Avoid | What to Do Instead |
|--------------|-----------|--------------------|
| Real payment processor (Stripe, Adyen) | Credential management, webhook handling, PCI scope — adds zero architectural insight | Already out of scope per PROJECT.md. Keep it simulated. Add to "v2" list in README. |
| Admin dashboard / back-office | Product management UI, CRUD over CRUD, no architecture value | Seed data via migration; expose a /seed endpoint locally if needed |
| Real email / SMS delivery | SMTP / SendGrid setup is yak-shaving | Persist notifications to a per-user "inbox" table + endpoint; render in frontend |
| Product reviews, ratings, Q&A | Whole separate bounded context; out of scope for "checkout saga" demo | Mention in README as obvious next bounded context |
| Wishlists / favourites / saved carts | Cart variants — repetitive, no new pattern | Skip |
| Search (Elasticsearch / OpenSearch / Azure AI Search) | Operational burden, infra complexity, doesn't showcase event-driven patterns | Basic SQL `LIKE` on name/description is fine for the demo |
| Recommendations / personalisation | ML / data pipeline scope creep | Skip |
| Multi-currency / FX | Real complexity, no pattern showcase | Single currency (probably GBP or USD), Money value object is hard-coded |
| Multi-tenancy | Tenant isolation is a whole project on its own | Single-tenant |
| Coupons / promotions / discount engine | Rules-engine yak-shaving | Skip; mention in README as obvious extension |
| Tax calculation (real rates by jurisdiction) | Tax-engine integration is a project unto itself | Flat 20% or "tax not applied" line item |
| Returns / RMA flow | Whole new saga; cool but doubles scope | Tempting differentiator — consider as v2 |
| Mobile app | Already out of scope per PROJECT.md | — |
| WebSockets / SignalR for live order updates | Already out of scope per PROJECT.md; HTTP polling is sufficient for demo | Poll order-detail every 2–3s |
| GraphQL gateway / BFF aggregation layer | More moving parts, doesn't add saga clarity | Frontend calls services directly (or one thin YARP gateway, optional) |
| Internationalisation (i18n) of the frontend | Pure UI work, no backend signal | English only |
| Recommendation: avoid building a "product variants" model (size/colour matrices) | Adds catalog complexity that doesn't touch the saga | Single-SKU products, mention variants as v2 |

---

## Per-Service Minimum Viable Feature Set

Cross-reference for roadmap building. Anything outside this list is either differentiator or anti-feature.

### Catalog Service (MVP)
- `GET /products` (paged, optional category filter)
- `GET /products/{id}`
- `GET /categories`
- Seeded product data (20-50 SKUs)
- Publishes: `ProductPriceChanged` (only if implementing the price-snapshot scenario; otherwise none)
- Subscribes to: none
- DB: relational (Postgres or SQL Server)

### Cart Service (MVP)
- `GET /cart` (current user)
- `POST /cart/items` (add)
- `PUT /cart/items/{productId}` (update qty)
- `DELETE /cart/items/{productId}`
- `DELETE /cart` (clear — called by Checkout on success)
- Publishes: none required for MVP (or `CartCheckedOut` if Checkout is downstream)
- Subscribes to: `OrderConfirmed` (to clear cart) — optional, can be done synchronously by Checkout
- DB: Redis (showcases polyglot) or relational

### Checkout Service (MVP)
- `POST /checkout` (body: shipping address, payment method placeholder) — kicks off saga
- `GET /checkout/{id}` (saga status)
- Hosts the `CheckoutSaga` process manager
- Publishes: `CheckoutInitiated`, saga commands (`AuthorisePayment`, `CreateOrder`, etc. — choreography vs orchestration is an ADR call)
- Subscribes to: all saga step events
- DB: relational (saga state persistence is non-negotiable)

### Orders Service (MVP)
- `GET /orders` (current user, paged)
- `GET /orders/{id}` (with line items + status + correlation id)
- Internal command handler: `CreateOrder` (from saga)
- Order state machine: Pending → Paid → Fulfilled / Cancelled / Failed
- Publishes: `OrderCreated`, `OrderStatusChanged`, `OrderCancelled`
- Subscribes to: `PaymentSucceeded`, `PaymentFailed`, `OrderShipped`
- DB: relational; this is the aggregate-rich service

### Identity Service (MVP)
- `POST /auth/register`
- `POST /auth/login` → JWT
- `POST /auth/logout` (optional if JWT-only)
- `GET /me`
- Seeded demo users
- Publishes: `UserRegistered` (Notifications subscribes)
- Subscribes to: none
- DB: relational (ASP.NET Core Identity schema)

### Payments Service (MVP — simulated)
- Internal command handler: `AuthorisePayment` (from saga)
- Deterministic failure rule (e.g. amount ending in `.99` declines)
- Publishes: `PaymentSucceeded`, `PaymentFailed`
- Subscribes to: payment commands from saga
- Idempotency: dedupe by checkout/order id
- DB: relational (payment attempt log)

### Fulfillment Service (MVP)
- Internal handler: `OrderPaid` consumer
- Background worker advances status on a timer (or manual `POST /fulfillment/{id}/ship` for demo control)
- Publishes: `OrderShipped`
- Subscribes to: `OrderPaid` (or `OrderConfirmed`)
- DB: relational (fulfillment records)

### Notifications Service (MVP)
- `GET /notifications` (current user inbox)
- Background consumer subscribed to: `UserRegistered`, `OrderCreated`, `PaymentFailed`, `OrderShipped`
- Persists notification + (optionally) logs to console
- Publishes: none
- DB: relational or document; small, append-only

### Shared Contracts library (MVP)
- Event/message DTOs: `CheckoutInitiated`, `PaymentSucceeded`, `PaymentFailed`, `OrderCreated`, `OrderPaid`, `OrderShipped`, `OrderCancelled`, `UserRegistered`
- Saga command messages (if orchestration): `AuthorisePayment`, `CreateOrder`, `ReleaseInventory`, etc.
- Common envelope: `MessageId`, `CorrelationId`, `CausationId`, `OccurredAt`
- No behaviour — pure contracts; versioning strategy documented in ADR

### Angular Frontend (MVP)
- Routes: `/catalog`, `/product/:id`, `/cart`, `/checkout`, `/orders`, `/orders/:id`, `/login`, `/register`
- Auth interceptor for JWT
- Order detail page polls for status updates (the "watch the saga play out" page)
- Minimal styling — Angular Material or Tailwind, no custom design system

---

## Feature Dependencies

Builds bottom-up so each phase has runnable value. Arrow = "depends on".

```
Identity ───────────────────────────────────────┐
                                                ▼
Catalog ──► Cart ──► Checkout ──► Saga ──► Orders ──► Fulfillment ──► Notifications
                          │           │                                     ▲
                          │           ▼                                     │
                          └────► Payments ──────────────────────────────────┘
```

Detailed dependency map:

| Feature | Depends On |
|---------|------------|
| Cart (add item) | Catalog (price + stock lookup), Identity (per-user cart) |
| Checkout (initiate) | Cart (read), Identity, Shared Contracts |
| Saga | Checkout, Payments, Orders, Fulfillment, Notifications, Shared Contracts, ASB topics |
| Orders (create) | Saga (command), Shared Contracts |
| Payments | Saga (command), Shared Contracts |
| Fulfillment | Orders (events), Shared Contracts |
| Notifications | Identity, Orders, Payments, Fulfillment (all as event sources) |
| Frontend checkout flow | Catalog API, Cart API, Checkout API, Auth |
| Frontend order detail | Orders API |
| OpenTelemetry tracing | All services, ASB property propagation, collector / Aspire dashboard |
| Outbox pattern | Per-service DB schema, message dispatcher background worker |
| ADRs | Decisions actually made in code — write *after* the decision, not before |

**Critical-path observation:** the Catalog → Cart → Checkout → Saga → (Payments, Orders, Fulfillment, Notifications) chain is the demo's spine. Identity is a prerequisite (cart-per-user). Everything else (search, admin, reviews) is optional. Build along the spine first, vertically slicing — not horizontally service-by-service.

---

## MVP Phase Recommendation (Roadmap Hints)

Suggested phase ordering for the roadmap consumer. Each phase ends in a runnable, demo-able increment.

1. **Phase 1 — Walking skeleton:** Identity + Catalog (read-only) + Cart + minimal frontend (browse, login, add to cart). No messaging yet. Proves the dev loop, Docker Compose, contracts library shape.
2. **Phase 2 — First event:** Introduce ASB, publish + consume one event (e.g. `UserRegistered` → Notifications inbox). Proves the messaging spine before the saga.
3. **Phase 3 — Orders + Payments (synchronous-ish):** Checkout creates an Order, calls Payments. Still no saga — direct flow. Proves the domain model.
4. **Phase 4 — The saga:** Refactor checkout flow into the explicit process manager. Add Fulfillment. Add deterministic payment failure + compensation path. **This is the project's centrepiece — most ADRs land here.**
5. **Phase 5 — Cross-cutting:** Outbox, idempotent consumers, correlation ids, OpenTelemetry, health checks. The "production-readiness" pass.
6. **Phase 6 — Polish + demo script:** Seeded data, `demo.ps1`, README screenshots, context diagram, ADR audit.
7. **Phase 7 — Infrastructure as code:** Terraform modules for ASB + Container Apps + supporting resources, dev environment deploy.

Defer indefinitely (move to "v2" in README): real payments, admin UI, search engine, reviews, wishlists, multi-currency, real email.

---

## Sources

- `.planning/PROJECT.md` (this repo) — scope, services, patterns, constraints
- Training-data knowledge of **Microsoft eShopOnContainers** (dotnet-architecture/eShopOnContainers) — reference for service decomposition, saga in checkout, integration events, identity setup. Confidence: MEDIUM.
- Training-data knowledge of **dotnet/eShop** (.NET 8/9 reference app) — successor to eShopOnContainers; informs current patterns (Aspire, OpenTelemetry, EventBus abstractions). Confidence: MEDIUM.
- Training-data knowledge of DDD literature — Eric Evans *Domain-Driven Design*, Vaughn Vernon *Implementing Domain-Driven Design* — for aggregate / value object / bounded context guidance. Confidence: HIGH.
- Training-data knowledge of saga / process manager patterns — Hohpe & Woolf *Enterprise Integration Patterns*, Chris Richardson *Microservices Patterns*. Confidence: HIGH.
- General e-commerce domain knowledge — confidence MEDIUM for "what users expect", LOW for specific market data.

### Research gaps to revisit when external tools are available
- Verify current state of `dotnet/eShop` repo (features added since training cutoff) to keep this aligned with the current canonical .NET reference.
- Verify MassTransit vs NServiceBus vs hand-rolled saga support on .NET 10 + Azure Service Bus — relevant when the saga ADR is being written.
- Confirm OpenTelemetry / .NET Aspire dashboard story for multi-service local trace visualisation in current .NET 10 GA tooling.
