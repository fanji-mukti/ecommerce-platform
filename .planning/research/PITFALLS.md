# Domain Pitfalls

**Domain:** Event-driven .NET microservices e-commerce (8 services, Azure Service Bus, DDD, Saga)
**Researched:** 2026-05-30
**Confidence:** MEDIUM (training-data-only — external research tools unavailable in this run; recommend validating each item during the phase that introduces it)

---

## Research Notes

External research tools (WebSearch, Brave, ctx7, MCP doc tools) were unavailable for this run. The pitfalls below are sourced from accumulated knowledge of MassTransit, Azure Service Bus, MS eShopOnContainers post-mortems, and well-documented microservices anti-patterns (Newman, Vernon, Richardson). Each pitfall is flagged with a confidence level and a recommended verification step. Treat MEDIUM/LOW confidence items as hypotheses to validate during the corresponding phase.

---

## Critical Pitfalls

Mistakes that cause rewrites, lost data, or unfixable architecture debt.

### Pitfall 1: Shared Database Across Services
**Confidence:** HIGH
**What goes wrong:** One PostgreSQL/SQL Server instance with separate schemas (Catalog, Orders, Cart) accessed by every service. Looks pragmatic at first; collapses into a distributed monolith because schemas reference each other and migrations require coordinating all teams.
**Why it happens:** Saves Docker Compose lines, avoids "duplicating" product data, feels DRY.
**Consequences:**
- Cross-service joins creep in (Orders service queries Catalog.Products table directly)
- Independent deployability is destroyed — every schema change requires a multi-service release
- Bounded contexts collapse; DDD becomes cosmetic
- Reverse migration is a multi-month rewrite
**Prevention:**
- One database **container/instance** per service in Docker Compose (yes, 8 DB containers; use lightweight images like postgres:16-alpine)
- No service references another service's schema, even read-only
- If service B needs data from service A: subscribe to A's events and build a local read model
- Code-review rule: any connection string in a service that points to another service's DB is rejected
**Detection (warning signs):** PRs adding `[Table("OtherService.X")]`, shared EF DbContexts across projects, "let's just join across schemas for the report"
**Phase mapping:** Phase 0/1 — bake this into the Docker Compose foundation. Far cheaper to enforce than to undo.

---

### Pitfall 2: Bloated `Contracts` Library Becomes a Distributed Monolith
**Confidence:** HIGH
**What goes wrong:** The shared `src/building-blocks/Contracts/` project starts as message DTOs, then accumulates: validation logic, enums shared with domain code, base entities, common interfaces, repository contracts, mapping profiles. Every service references it, every contract change forces every service to rebuild/redeploy in lockstep.
**Why it happens:** Genuine DRY instinct + the project literally exists already + adding "one more useful thing" is always trivial in isolation.
**Consequences:**
- Lockstep deployments (the thing event-driven architecture is supposed to prevent)
- Versioning becomes impossible — a v2 contract requires either breaking all consumers or maintaining two namespaces
- Circular conceptual dependencies (Orders contract references Cart enum which references Catalog ID type)
- Refactoring one service requires touching the shared lib, which ripples
**Prevention:**
- **Contracts contains only**: message/event record types (immutable POCOs), primitive value types used in messages (e.g., `Money(decimal Amount, string Currency)`), and the bare minimum constants for topic/subscription names
- **Contracts forbids**: behaviour, EF attributes, MediatR handlers, base classes other than marker interfaces, business validation, mapping logic
- One namespace per publishing service: `Contracts.Catalog.IntegrationEvents.V1`, `Contracts.Orders.IntegrationEvents.V1`
- Adopt a versioning convention from day one: new fields are additive only; breaking changes get a new `.V2` namespace and run side-by-side
- Add a `.editorconfig` / Roslyn analyzer rule: no `using System.Data.*`, no `Microsoft.EntityFrameworkCore.*` in the Contracts assembly
- Consider replacing the project reference with a NuGet package published to a local feed once stable — the friction of versioning helps enforce discipline
**Detection (warning signs):** Contracts project has more than ~3 dependencies; any class in Contracts has methods other than equality/ctor; a service can't be built without rebuilding Contracts; PRs that add helpers to Contracts "just for now"
**Phase mapping:** Phase 1 (foundation) — write an ADR locking the Contracts scope. Phase 2+ — enforce in PR review.

---

### Pitfall 3: Implementing the Checkout Saga as Choreography
**Confidence:** HIGH
**What goes wrong:** Each service listens for events and decides what to do next. Cart publishes `CheckoutInitiated`, Orders reacts and publishes `OrderCreated`, Payments reacts and publishes `PaymentAuthorized`, Fulfillment reacts, Notifications reacts. No central place describes the flow. To answer "why didn't this order ship?" requires correlating logs across 5 services.
**Why it happens:** Choreography sounds more "pure event-driven" and is celebrated in conference talks. It is genuinely the right call for some flows. Checkout is not one of them.
**Consequences:**
- Business flow is implicit, scattered across 5+ services and inferrable only from event subscriptions
- Compensation logic is duplicated (if Payment fails, who reverses Inventory reservation? Both Cart and Orders think it's their job, or neither does)
- Adding a new step (e.g., fraud check) requires modifying every downstream consumer's filters
- Saga state, timeouts, retries, and compensation become impossible to reason about
**Prevention:**
- **Use orchestration for checkout**: implement an explicit `CheckoutSaga` (MassTransit `MassTransitStateMachine<CheckoutState>`) that owns the flow, sends commands to services, and reacts to their reply events
- The saga lives in the Checkout (or a dedicated Saga) service and persists state (saga repository) — typically EF or Azure Cosmos
- Other one-off reactions (e.g., Notifications reacting to `OrderShipped` to send an email) can stay choreographed — they're not part of the business transaction
- Document the rule in an ADR: "Multi-step business transactions = orchestration. Side-effects/observers = choreography."
**Detection (warning signs):** Asking "where does the checkout happen?" gets 4 different answers; compensation logic appears in more than one service; timeouts on the saga can't be configured centrally
**Phase mapping:** Phase 3 (when the saga is introduced) — make the orchestration choice in an ADR before writing the first handler. This decision is very expensive to reverse.

---

### Pitfall 4: Two-Phase Commit via the Outbox Skipped
**Confidence:** HIGH
**What goes wrong:** A handler writes to the database, then publishes a message to Azure Service Bus. Crash between the two = data is saved but the event is lost (or vice versa). System state silently diverges. The bug shows up weeks later as "orders that never shipped" or "double-charged customers."
**Why it happens:** The naive code is one line shorter; "it works in dev"; the failure mode is rare enough to not surface in tests.
**Consequences:**
- Eventually consistent system becomes eventually *inconsistent* — the worst outcome
- Bugs are non-deterministic and almost impossible to reproduce locally
- Data integrity loss compounds: a missed `OrderCreated` event means Fulfillment never starts, Notifications never fires
**Prevention:**
- **Use the Outbox pattern** for every event published from a transactional context. MassTransit has built-in EF Core outbox support — enable it on day one
  ```csharp
  busConfigurator.AddEntityFrameworkOutbox<AppDbContext>(o => {
      o.UseSqlServer(); // or Postgres
      o.UseBusOutbox();
  });
  ```
- The outbox writes the message to a DB table inside the same transaction as the business data; a background dispatcher publishes to ASB at-least-once
- Inbox pattern on consumers for idempotency (also MassTransit-supported)
- Idempotency keys (`MessageId`) on every consumer
**Detection (warning signs):** Any handler that calls `_publishEndpoint.Publish(...)` outside of MassTransit's `ConsumeContext` and isn't wrapped in an outbox; "we'll add the outbox later" tickets
**Phase mapping:** Phase 2 (first service publishing events) — outbox is foundational. Adding it later requires backfilling every handler.

---

### Pitfall 5: ASB Topic-per-Event Explosion
**Confidence:** MEDIUM
**What goes wrong:** Following the naive MassTransit default, every event type gets its own topic: `Contracts.Orders.IntegrationEvents.V1:OrderCreated`, `:OrderShipped`, `:OrderCancelled`, etc. With 8 services and ~5 events each, you end up with 40+ topics. Each subscription is also separate. ARM templates / Terraform get unwieldy; ASB quotas (1000 topics on Standard, but subscription rules are limited) become a planning concern.
**Why it happens:** MassTransit's default convention is topic-per-type; it's the path of least resistance.
**Consequences:**
- Terraform/Bicep modules become hundreds of lines
- Per-event-type ACLs are unmanageable
- Adding a new event = new topic = infrastructure PR
- Wildcard subscriptions ("all Orders events") aren't possible without re-design
**Prevention:**
- **Topic-per-aggregate** (or per-bounded-context): one topic `orders-events`, one `catalog-events`, etc. Use ASB **subscription filters** (CorrelationFilter on a `MessageType` system property) to route to consumers
- Configure MassTransit with `endpoint.ConfigureConsumeTopology = false` and define topics explicitly, OR use `SubscriptionEndpoint` with filters
- Document the topic taxonomy in an ADR before provisioning anything in Azure
- Use ASB Standard tier; only upgrade to Premium when you have a real reason (cost is ~10x)
**Detection (warning signs):** Terraform topic count > 20; can't sketch the topic/subscription map on one page; new event types require Terraform changes
**Phase mapping:** Phase 1/2 — ADR on topic strategy before first deployment. Phase 4 (Terraform) — module design.

---

### Pitfall 6: Saga State Not Persisted (or Persisted in the Wrong Place)
**Confidence:** HIGH
**What goes wrong:** The Checkout saga keeps state in-memory. Pod restart, scale event, or container redeploy = in-flight checkouts are lost. Customers see stuck "processing" orders. Or: saga state is persisted in the Cart service's database, coupling the saga's lifecycle to a service that should be stateless.
**Why it happens:** MassTransit's in-memory saga repository "just works" in dev. The production switch is forgotten or deprioritised.
**Consequences:**
- Lost transactions on every deploy (Azure Container Apps will recycle pods frequently)
- Saga state coupled to wrong service = can't redeploy/refactor independently
- No audit trail of in-flight workflows
**Prevention:**
- Use `EntityFrameworkSagaRepository` (or Cosmos / Redis if appropriate) from day one, even in dev
- Saga state lives in the **saga's own database**, not a domain service's DB
- Saga DB has its own EF migrations, its own connection string, its own container in Compose
- Add an admin/debug endpoint to query in-flight sagas — invaluable for debugging
**Detection (warning signs):** `InMemorySagaRepository` anywhere outside a unit test; saga state in a domain service's DbContext; "where do we see stuck checkouts?" has no answer
**Phase mapping:** Phase 3 (saga introduction) — persistence configured in the same PR as the saga itself.

---

## Moderate Pitfalls

### Pitfall 7: JWT Validation Logic Duplicated in Every Service
**Confidence:** MEDIUM
**What goes wrong:** Each of 8 services implements `AddJwtBearer` independently with slightly different settings (issuer, audience, clock skew). Token rotation requires 8 PRs. Misconfigured services accept stale tokens.
**Prevention:**
- Extract `AddPlatformAuthentication()` extension into a tiny shared **infrastructure** library (separate from Contracts — auth is cross-cutting, not contract data)
- Or: handle auth at an **API Gateway / BFF** (YARP, Azure API Management, or Angular's BFF pattern) and forward a trusted internal claim — services don't validate JWTs at all, they trust the gateway
- For service-to-service auth (Notifications calling Orders), use Managed Identity + Azure AD when in Azure, and a stub in local dev
- Pick **one** approach in an ADR — mixing both is the worst outcome
**Phase mapping:** Phase 2 (Identity service) — decide gateway vs per-service auth before implementing the second service.

---

### Pitfall 8: Docker Compose File Becomes Unmaintainable
**Confidence:** HIGH
**What goes wrong:** Single `docker-compose.yml` with 8 services + 8 databases + ASB emulator + Identity + frontend + Seq/Jaeger = 20+ services in one 800-line YAML. Editing it is painful, partial startups are impossible, onboarding is slow.
**Prevention:**
- Split into **compose profiles**: `--profile catalog`, `--profile checkout-flow`, `--profile infra-only`
- Use `compose.override.yml` for local dev settings; commit a `compose.azurite.yml` for the ASB emulator
- Provide convenience scripts: `make up-catalog`, `make up-saga` (or PowerShell equivalents)
- Keep a `docker-compose.infra.yml` for shared infrastructure (DBs, ASB emulator, telemetry) separate from services
- Use the **Azure Service Bus emulator** (`mcr.microsoft.com/azure-messaging/servicebus-emulator`) — it exists as of 2024 and is far better than mocking or RabbitMQ-as-substitute
- Healthchecks on every service so `depends_on: condition: service_healthy` actually works
**Detection (warning signs):** Compose file > 400 lines; `docker compose up` takes > 2 min; "I only need Catalog running" has no easy answer
**Phase mapping:** Phase 1 (compose foundation) — design profiles from the start. Phase 2-3 — refactor before adding the 4th service.

---

### Pitfall 9: DDD Over-Engineering on a Portfolio Project
**Confidence:** MEDIUM
**What goes wrong:** Every primitive becomes a value object (`CustomerId`, `OrderId`, `ProductId`, `Quantity`, `Money`, `Sku`, `EmailAddress`...). Every aggregate gets a repository interface + EF implementation + specification pattern + factory + 4-layer onion. The Catalog service has 80 files to expose `GET /products`. Reviewers can't tell what the code *does*.
**Why it happens:** "Doing DDD properly." Conflating tactical patterns (the blue book) with strategic DDD (the actual value).
**Consequences:**
- Velocity collapses; portfolio project never ships
- The DDD signal is lost — reviewers see ceremony, not domain insight
- Real domain logic (pricing, discount eligibility, fulfillment splits) gets squeezed by infrastructure scaffolding
**Prevention:**
- **Pragmatic rule**: value objects only where they encode an invariant (`Money` validates currency; `Email` validates format; `Sku` enforces pattern). `OrderId` as a strongly-typed `record struct OrderId(Guid Value)` is fine but optional — primitive `Guid` is acceptable.
- Aggregates only where there's a **real invariant boundary** (Order: line items + total + status transitions). The Catalog `Product` is probably a CRUD entity, not an aggregate — admitting that is more honest than pretending.
- Skip the repository abstraction for services where EF Core IS the repository. `DbContext` is already a Unit of Work + Repository.
- Write one or two services with **rich** domain models (Orders, Cart) and the rest as **anaemic** CRUD (Catalog, Notifications). Show you know when to apply which.
- Add an ADR: "DDD scope per service" — explicitly classify each service as anaemic/rich.
**Detection (warning signs):** More files in `Domain/` than in `Application/`; value objects with no behaviour; "I'm refactoring the repository pattern again"
**Phase mapping:** Phase 2 — establish the pragmatic rule before building service #2. Re-check at each milestone.

---

### Pitfall 10: Terraform State Stored Locally or Without Locking
**Confidence:** HIGH
**What goes wrong:** `terraform.tfstate` ends up in the repo, in `.gitignore` but on one machine, or in a storage account without a lock. Two `apply` runs race and corrupt state. Recovery requires manually editing state — high-stress, error-prone.
**Prevention:**
- **Remote backend from day one**: Azure Storage Account + container, with state locking via blob lease (built-in for the azurerm backend as of Terraform 1.x)
- Backend config in a separate `backend.tf` per environment; use partial config + `-backend-config` flags for env-specific values (subscription, RG, container)
- Bootstrap problem: create the state storage account via a one-off script (or `terraform init -migrate-state` from local), document this in `infra/README.md`
- One state file per environment (dev/prod), never share
- Use workspaces sparingly — environment-per-directory is clearer for portfolio code
- Enable storage account soft-delete + versioning on the state container
- **Never** check in `*.tfstate`, `*.tfstate.backup`, `*.tfvars` with secrets; use `.gitignore` + a `.tfvars.example` template
**Detection (warning signs):** `terraform.tfstate` in `git status`; "I'll set up remote state later"; two devs running apply simultaneously
**Phase mapping:** Phase 4 (Terraform introduction) — remote backend is the **first** thing provisioned, before any resources.

---

### Pitfall 11: Eventual Consistency UX Not Designed For
**Confidence:** MEDIUM
**What goes wrong:** User clicks "Checkout", frontend POSTs, gets 202 Accepted, then immediately navigates to "/orders" — but the Orders read model hasn't received the event yet. User sees no order, refreshes, panics, double-submits.
**Why it happens:** Frontend devs reason in CRUD/HTTP terms; eventual consistency is invisible until the demo.
**Prevention:**
- Design the UX to embrace async: "Order received — we'll email you when confirmed" rather than "Here's your order #1234"
- Return a **client-generated correlation ID** from POST, let the frontend poll `/orders/by-correlation/{id}` or subscribe via SignalR (out of scope here — so poll)
- Or: have the API gateway/BFF block briefly (`async`/`await` on a saga completion event with timeout) before returning — gives a synchronous feel, but with the saga underneath. Useful for portfolio demo storytelling.
- Read models updated by event handlers — accept that read-after-write is not guaranteed in < ~100ms
**Detection (warning signs):** Frontend assumes synchronous semantics; demo script says "click checkout, then click orders" without a delay
**Phase mapping:** Phase 3 (saga) and Phase 5 (frontend integration) — UX contract must account for async.

---

### Pitfall 12: No Correlation IDs / Distributed Tracing Setup Late
**Confidence:** HIGH
**What goes wrong:** First production-like bug spans 5 services; no way to follow the request. Adding OpenTelemetry across 8 services + ASB is a multi-day yak shave done under pressure.
**Prevention:**
- OpenTelemetry from Phase 1 — even with just a local Jaeger/Seq exporter in Compose
- MassTransit auto-instruments ASB activity propagation when OTel is configured
- Every log line includes `TraceId`, `SpanId`, and a business `CorrelationId` (typically the saga ID or order ID)
- Standardise via a shared `AddPlatformObservability()` extension in a small infrastructure NuGet (NOT in Contracts)
- Aspire dashboard is a strong option for local dev (works with OTel out of the box, .NET 10-friendly)
**Phase mapping:** Phase 1 (foundation) — Seq or Aspire dashboard in Compose. Phase 2+ — every service emits structured logs + traces from its first commit.

---

### Pitfall 13: Dead-Letter Queues Ignored
**Confidence:** HIGH
**What goes wrong:** ASB automatically dead-letters messages after `MaxDeliveryCount` (default 10) failures. No one watches the DLQ. Failed orders pile up silently. Discovered weeks later when the DLQ is full or a customer complains.
**Prevention:**
- Every subscription has a documented DLQ-handling strategy (alert + manual replay tool, or a redrive consumer that re-publishes with backoff)
- MassTransit's `Retry` and `Redelivery` configured with intent (e.g., immediate retry 3x, then scheduled redelivery at 1m, 5m, 30m, then DLQ)
- Build a tiny ops endpoint or CLI to peek/replay DLQ messages per subscription
- Azure Monitor alert on `DeadletteredMessages` metric > 0
- Distinguish **transient** failures (retry) from **poison** messages (DLQ immediately) — wrap permanent failures in a non-retryable exception type
**Phase mapping:** Phase 2 (first event consumers) — retry/DLQ policy in an ADR. Phase 4 (Azure deploy) — DLQ monitoring alerts.

---

### Pitfall 14: Cart Service Treated as a Microservice Instead of a Cache
**Confidence:** MEDIUM
**What goes wrong:** Cart gets its own database, EF migrations, repository, integration events, the works — for what is effectively a session-scoped key-value store that lives for ~20 minutes. Over-engineered relative to its value.
**Prevention:**
- Consider Cart as a thin service backed by **Redis** (or Cosmos with TTL) — no EF, no migrations
- Cart publishes one event: `CheckoutInitiated(cartId, userId, lineItems, totals)`. After that, Cart is irrelevant to the saga.
- Don't model `Cart` as a DDD aggregate — it's a transient projection
- Skip Cart event-sourcing temptations
**Phase mapping:** Phase 2 (Cart service design) — pick storage in an ADR, prefer Redis.

---

### Pitfall 15: Idempotency Not Enforced on Consumers
**Confidence:** HIGH
**What goes wrong:** ASB guarantees at-least-once delivery. A retried `OrderCreated` event creates two orders. A retried `PaymentAuthorized` charges twice (well, in the simulated case, marks paid twice — but the bug is the same).
**Prevention:**
- Use MassTransit's **Inbox** pattern (sibling to Outbox) — deduplicates by `MessageId`
- For handlers that can't use the inbox: store `(MessageId, CorrelationId)` in an `IdempotencyKeys` table, check on consume
- Make domain operations naturally idempotent where possible: `SetOrderStatus(id, status)` instead of `IncrementOrderVersion(id)`
- Test by deliberately republishing every event type in integration tests; assert no duplicate side effects
**Phase mapping:** Phase 2 (first consumer) — inbox configured alongside outbox.

---

## Minor Pitfalls

### Pitfall 16: ADRs Written Retroactively (or Not at All)
**Confidence:** HIGH
**What goes wrong:** Decisions are made in PRs / Slack / heads, never written down. Reviewer of the portfolio sees only the code — not the reasoning. The portfolio value drops sharply.
**Prevention:** ADR-per-decision, written **before or during** the implementing PR. Template (MADR 4.0): Context, Decision, Consequences, Alternatives Considered. Number sequentially, never renumber.
**Phase mapping:** Every phase.

---

### Pitfall 17: EF Migrations Not Reproducible / Auto-Apply in Production
**Confidence:** MEDIUM
**What goes wrong:** `Database.Migrate()` called on startup is convenient locally and catastrophic in Azure (two pod replicas race the migration; partial schema; rollback impossible).
**Prevention:**
- Migrations applied by a **dedicated init job** (Azure Container Apps Jobs, or a `migrate` Compose service that runs to completion before others start)
- `Database.EnsureCreated()` is for tests only — never production
- Each service has its own migrations, in its own assembly
- Backwards-compatible migrations: add columns nullable first, deploy code, then make non-nullable in a second migration (expand-contract)
**Phase mapping:** Phase 2 (first EF service). Phase 4 (Azure deploy) — migration strategy locked in ADR.

---

### Pitfall 18: Secrets in `appsettings.json` or Compose `.env` Committed
**Confidence:** HIGH
**What goes wrong:** ASB connection string, JWT signing key, DB passwords end up in source control. GitHub secret scanning may catch it; portfolio reviewer definitely will.
**Prevention:**
- User Secrets in dev (`dotnet user-secrets`)
- `.env.example` committed, `.env` git-ignored
- Azure Key Vault + Managed Identity in production; Terraform provisions Key Vault references for Container Apps
- Pre-commit hook scanning for common secret patterns (gitleaks)
**Phase mapping:** Phase 1 (foundation) — gitignore + .env.example. Phase 4 (Azure) — Key Vault.

---

### Pitfall 19: Frontend Coupled to Service URLs / No BFF
**Confidence:** MEDIUM
**What goes wrong:** Angular app holds URLs for 6 services, deals with 6 different CORS configs, has to compose responses from multiple services in the browser.
**Prevention:**
- BFF (Backend for Frontend) pattern: a single API the Angular app talks to, which fans out to services. Implement as YARP reverse proxy, an ASP.NET aggregator, or per-page composition in a small `bff-web` service.
- Centralises auth (the BFF holds the JWT, the browser holds only a session cookie — BFF pattern proper)
- Simplifies CORS to one origin
**Phase mapping:** Phase 5 (frontend integration).

---

### Pitfall 20: MassTransit Coupled in Domain Layer
**Confidence:** MEDIUM
**What goes wrong:** Domain code takes `IPublishEndpoint`, references `MassTransit` namespaces. Replacing the bus (or testing the domain) becomes hard.
**Prevention:**
- Domain raises domain events as POCOs collected on the aggregate (`Order.DomainEvents`)
- Application layer (handler/saga) translates domain events to integration events (Contracts) and publishes via MassTransit
- This also enforces the public contract / private domain distinction
**Phase mapping:** Phase 2 (first rich aggregate).

---

## Phase-Specific Warnings

| Phase Topic | Likely Pitfall | Mitigation |
|-------------|---------------|------------|
| Phase 1: Foundation (Compose, observability, Contracts scope) | #2 Bloated Contracts, #8 Compose creep, #12 No tracing | ADR for Contracts scope; Compose profiles from day 1; OTel + Seq in initial Compose |
| Phase 2: First services (Catalog, Cart, Identity) | #1 Shared DB, #4 No outbox, #7 Auth duplication, #9 DDD over-eng, #15 No idempotency, #17 Auto-migrate | One DB per service container; outbox + inbox enabled in first MT config; auth ADR (gateway vs per-service); pragmatic DDD ADR |
| Phase 3: Checkout saga | #3 Choreography, #6 In-memory saga, #11 Eventual-consistency UX | Orchestration ADR; EF saga repository from first commit; UX contract for async |
| Phase 4: Azure deploy via Terraform | #5 Topic explosion, #10 TF state, #13 DLQ ignored, #17 Migration strategy, #18 Secrets | Topic-per-context ADR; remote state day 1; DLQ alerts; migration init job; Key Vault |
| Phase 5: Frontend integration | #11 Async UX, #19 No BFF | BFF service; correlation ID polling pattern |
| Phase 6+: Hardening | #13 DLQ replay tooling, #20 Domain-MT coupling | Build DLQ replay CLI; refactor any MT usage in domain |

---

## Cross-Cutting Recommendations

1. **ADR for every "Critical" pitfall above** — writing the decision forces clarity and produces portfolio artifacts. Suggested ADRs:
   - ADR-XXXX: Contracts library scope and versioning policy
   - ADR-XXXX: Database-per-service policy
   - ADR-XXXX: Choreography vs orchestration policy (and why checkout is orchestrated)
   - ADR-XXXX: Outbox/Inbox pattern as default
   - ADR-XXXX: ASB topic taxonomy
   - ADR-XXXX: Saga persistence strategy
   - ADR-XXXX: Authentication architecture (gateway vs per-service)
   - ADR-XXXX: Terraform remote state + environment layout
   - ADR-XXXX: DDD pragmatism per service classification

2. **Reference implementation to study (with caution)**: Microsoft eShopOnContainers and the newer **eShop** reference app (Aspire-based). Both have publicly documented post-mortems showing exactly the pitfalls above — the eShopOnContainers shared `BuildingBlocks` is a textbook example of pitfall #2 evolving over time.

3. **Confidence-raising next step before/during Phase 1**: validate ASB emulator behaviour, MassTransit outbox + EF Core 10 compatibility, and the Aspire dashboard's OTel integration — these are the lowest-risk-to-test, highest-blast-radius-if-wrong assumptions.

---

## Sources

- **MEDIUM confidence (training data, established community patterns)**:
  - MassTransit documentation patterns (outbox, inbox, saga state machines, retry/redelivery)
  - Microsoft eShopOnContainers documented evolution (BuildingBlocks pain points, eventual consistency UX)
  - Microsoft Learn Azure Service Bus patterns and quotas
  - Sam Newman "Building Microservices" 2nd ed — shared database anti-pattern, BFF
  - Chris Richardson microservices.io — saga patterns, outbox
  - Vaughn Vernon "Implementing DDD" — pragmatic aggregate boundaries
- **HIGH confidence (well-established platform behaviour)**:
  - ASB at-least-once delivery, dead-lettering, MaxDeliveryCount default
  - Terraform azurerm backend state locking via blob lease
  - EF Core migration patterns and `Database.Migrate()` race conditions

**Recommended verification during phase research**: re-validate MassTransit version-specific APIs (the 8.x → 9.x line has changed outbox/transactional APIs), Azure Service Bus emulator current capabilities and limitations vs production, Terraform azurerm provider current state-locking guidance, and the Aspire dashboard's role in .NET 10 (was experimental in earlier versions).
