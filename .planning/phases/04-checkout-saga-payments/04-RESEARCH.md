# Phase 4: Checkout Saga & Payments - Research

**Researched:** 2026-08-08
**Domain:** MassTransit saga orchestration, Azure Service Bus scheduled delivery, simulated payment idempotency, Angular polling UX
**Confidence:** MEDIUM (state machine architecture and contracts: HIGH — grounded in existing code; ASB scheduling mechanics: MEDIUM-LOW — flagged as highest technical risk in STATE.md, needs a Wave 0 spike)

## Summary

Phase 4 extends the **existing** `OrderStateMachine` in `ECommerce.Orders.API` (per ADR-0005, one orchestrator, no new saga) rather than building a second saga. The roadmap's checkout vocabulary (`Started → AwaitingPayment → Paid`) and the already-locked order-status vocabulary (`Pending → Paid → Fulfilled/Cancelled/Failed`, ORD-03) are **two different views of the same state machine**, not two different state machines: add one new leading state (`Started`), keep `Pending` as the internal name for the "awaiting payment" state (it already exists, is tested, and ORD-03 requires this exact literal string in the persisted read model), and expose a small server-side name mapping (`Started`→`Started`, `Pending`→`AwaitingPayment`, `Paid`→`Paid`, `Cancelled`/`Failed`→ as-is) only on the `GET /checkout/{id}` DTO. `GET /orders/{id}` keeps returning the raw ORD-03 vocabulary unchanged.

The saga gains two typed payment events (`PaymentAuthorised`, `PaymentFailed`) and one typed fulfillment event (`FulfillmentFailed`), all consumed directly by `OrderStateMachine` (no choreography), plus a scheduled timeout event using MassTransit's `Schedule`/`Unschedule` activities. The single largest technical risk — confirmed by cross-referencing the MassTransit documentation, a live GitHub issue, and the fact that this project already toggles between an in-memory bus (tests) and Azure Service Bus (dev/prod) — is that **Azure Service Bus native message scheduling is transport-specific and does not "just work" against the in-memory bus the test suite already relies on**. This document recommends Azure Service Bus native scheduling for the real transport (no new production package) plus a **test-only** `MassTransit.Quartz` dependency pinned to exactly `8.3.6` (matching the existing MassTransit pin) for the in-memory harness, and flags the ASB emulator's scheduled-message fidelity as unverified — recommend a short Wave 0 spike before committing task plans to it.

**Primary recommendation:** Extend `OrderStateMachine` in place with a new `Started` state and typed payment/fulfillment events; keep `checkoutId == orderId` as the single correlation id end-to-end; use ASB-native `Schedule`/`Unschedule` for the production timeout and a Quartz in-memory scheduler for the xUnit proof (D-05); make Payments idempotent via a unique-constrained `CheckoutId` column in its own new Postgres database, not via MassTransit's transport-level inbox alone.


<user_constraints>
## User Constraints (from CONTEXT.md)

### Locked Decisions

**Fulfillment-Failure Demo Trigger (CHK-04)**
- D-01: Since Fulfillment doesn't exist until Phase 5, the fulfillment-failure compensation path is triggered by a demo-only test endpoint (e.g. `POST /checkout/{id}/simulate-fulfillment-failure`), mirroring Phase 3's `POST /orders/test-create-from-cart` pattern: clearly marked as non-production/demo-only, easy for Phase 5 to retire once the real Fulfillment service exists.
- D-02: This trigger is API/test-only — no Angular UI control for it in Phase 4. It's called via Swagger/curl during live demo narration or exercised by integration tests.
- D-03: The endpoint is callable any time the order is in `Paid` status — no additional timing/window restriction.

**Saga Timeout (CHK-05)**
- D-04: The ~15-minute timeout is configurable, not hardcoded — e.g. `Checkout:TimeoutMinutes` (or similar) in appsettings/env var, defaulting to 15 for the "real" value but overridable to a short interval for local/live demos.
- D-05: The timeout path is proven via an xUnit integration test that injects a short test-only timeout value and asserts the compensation path (same as explicit failure) fires correctly — this validates the mechanism without requiring a live 15-minute wait, and works regardless of what the configured production default is.

**Checkout/Orders UX During the Saga (FE-03)**
- D-06: `/checkout` shows a step indicator (e.g. `mat-stepper` or an ordered list) that lights up each saga stage as it's reached (`Started → AwaitingPayment → Paid`, or the failure/cancellation equivalent).
- D-07: `/checkout` polls status on a fixed short interval (~1–2s) until a terminal state is reached, then stops polling.
- D-08: On reaching a terminal state, the page auto-redirects to `/orders/:id`.
- D-09: `/orders/:id` shows status + a human-readable failure reason on failure (e.g. "Payment declined", "Fulfillment failed — order cancelled and refunded"), not just the bare terminal status. Requires threading a failure reason through the saga into the Orders read model.

**Payment-Failure Demo Affordance (PAY-02)**
- D-10: `/checkout` shows hint text near the cart total / place-order button ("Tip: cart totals ending in .99 simulate a payment failure").
- D-11: In addition to the natural `.99`-ending-total trigger, add an explicit "Simulate payment failure" demo toggle/checkbox on `/checkout` that force-triggers the same failure path regardless of the actual cart total.

### Claude's Discretion
- Exact reconciliation between the roadmap's `Started`/`AwaitingPayment`/`Paid` state names (SC2) and the existing `OrderStateMachine`'s `Pending`/`Paid`/`Fulfilled`/`Cancelled`/`Failed` states — whether to rename/extend the existing state machine in place, add new states, or introduce a distinct pre-order "checkout" phase before `OrderCreated` fires. ADR-0005 already locks the saga as a single orchestrating state machine in the Orders service — the exact state shape is a research/planning call, not a user-vision question. **→ Addressed below in Architecture Patterns.**
- Whether `Checkout.API` is a thin façade that publishes a command and proxies `GET /checkout/{id}` to the Orders read model, or holds any state of its own. **→ Addressed below: thin façade, no own DB.**
- Exact `AuthorisePayment`/`RefundPayment` command/event shapes, Payments service's own idempotency-key storage. **→ Addressed below in Code Examples.**
- Visual styling of the step indicator, hint text placement, and demo-toggle placement — deferred to `/gsd-ui-phase 4`.

### Deferred Ideas (OUT OF SCOPE)
- Real Fulfillment service logic (timer-based shipping simulation, `OrderShipped`) — Phase 5.
- Real Notifications inbox surfacing saga lifecycle events — Phase 5.
- Retiring the Phase 3 `POST /orders/test-create-from-cart` test-trigger endpoint — happens as part of this phase since `/checkout` replaces it, but exact removal mechanics are a planning detail.
- Real payment provider integration (Stripe) — PAY-V2-01, out of scope for v1.
</user_constraints>

<phase_requirements>
## Phase Requirements

| ID | Description | Research Support |
|----|-------------|------------------|
| CHK-01 | User can initiate checkout and receive a checkoutId (202 Accepted) | Architecture Patterns → Saga Reconciliation; Code Examples → `POST /checkout` |
| CHK-02 | User can poll checkout / order status via GET /checkout/{id} | Architecture Patterns → Checkout.API façade; Code Examples → status DTO mapping |
| CHK-03 | Saga compensates on PaymentFailed — cancels the order | Architecture Patterns → state diagram; Code Examples → `PaymentFailed` handler |
| CHK-04 | Saga compensates on FulfillmentFailed — refunds payment and cancels order | Architecture Patterns → `Paid`→`Refunding`→`Cancelled`; Common Pitfalls → refund ordering |
| CHK-05 | Saga times out after ~15 minutes if not completed (compensation triggered) | Architecture Patterns → Timeout Scheduling (highest-risk section) |
| PAY-01 | Simulated payment service processes AuthorisePayment commands | Code Examples → Payments consumer |
| PAY-02 | Amounts ending in `.99` deterministically trigger PaymentFailed | Code Examples → deterministic failure rule |
| PAY-03 | Payment processing is idempotent by checkoutId | Don't Hand-Roll → idempotency table; Code Examples |
| FE-03 | User can complete checkout and see order status updating in real-time via polling | Architecture Patterns → Angular polling/stepper pattern; Common Pitfalls → no `/orders` feature folder exists yet |
</phase_requirements>

## Architectural Responsibility Map

| Capability | Primary Tier | Secondary Tier | Rationale |
|------------|-------------|----------------|-----------|
| Checkout initiation (`POST /checkout`) | API / Backend (Checkout.API) | API / Backend (Orders.API, internal) | Checkout.API is the public-facing thin façade; Orders.API owns the actual order-creation + cart-fetch logic already built in Phase 3 |
| Saga orchestration (state transitions, compensation) | API / Backend (Orders.API) | — | ADR-0005 locks a single state machine in Orders; no choreography |
| Payment authorization / idempotency | API / Backend (Payments.API) | Database / Storage (new PaymentsDb) | PAY-03 requires durable dedup by checkoutId — cannot live only in-memory or only in the message bus's transport-level inbox |
| Saga timeout scheduling | API / Backend (Orders.API, via MassTransit scheduler) | CDN/Static — n/a | Scheduling is a saga-internal concern; ASB-native scheduler for prod, Quartz in-memory scheduler for tests only |
| Failure-reason surfacing | API / Backend (Orders read model) | Browser / Client (Angular render) | Reason is computed once in the saga/Payments/Fulfillment domain and persisted; Angular only displays it |
| Checkout step indicator + polling | Browser / Client (Angular) | API / Backend (GET /checkout/{id}) | Polling and UI state belong client-side; server only exposes current status |
| Demo fulfillment-failure trigger | API / Backend (Checkout.API, demo-only) | — | Publishes directly onto the bus; no real Fulfillment service exists yet to own this |

## Project Constraints (from CLAUDE.md)

- MassTransit **pinned to exactly 8.3.6** (Apache-2.0) across every `.csproj` — never floating, never resolve to 9.x (commercial "Massient" license). This applies to any *new* MassTransit-family package added in this phase (see Package Legitimacy Audit).
- EF Core 10 + Npgsql for write-side persistence; Dapper reserved for read-side if hand-tuned queries are needed (not required for this phase's scope).
- Minimal APIs only — no MVC controllers.
- `System.Text.Json`, not Newtonsoft.
- Mapperly (source-generated) for DTO mapping — not AutoMapper. Existing `OrderMapper` pattern (registered as DI singleton) must be followed for any new Payments/Checkout DTOs.
- Angular 20 zoneless/standalone/signals; `provideHttpClient(withFetch())`; no NgModules/Zone.js.
- Distroless `aspnet:10.0-noble-chiseled` base image — no change needed this phase (no new Dockerfiles required, Aspire AppHost already declares all services).
- ADR format MADR, one file per decision in `docs/adr/` — if the saga reconciliation approach below is adopted, it is significant enough to warrant a new ADR (e.g. `0009-checkout-saga-state-reconciliation.md`), though writing it is an execution-time task, not a research deliverable.

## Standard Stack

### Core (already present, reused as-is)

| Library | Version | Purpose | Why Standard |
|---------|---------|---------|---------------|
| MassTransit | 8.3.6 (pinned) | Saga state machine, outbox/inbox, ASB transport | Already the project's locked messaging abstraction (ADR-0006) |
| MassTransit.Azure.ServiceBus.Core | 8.3.6 (pinned) | ASB transport + **native message scheduler** (`AddServiceBusMessageScheduler`/`UseServiceBusMessageScheduler`) | Already referenced by Orders.API; the scheduler extension methods ship in this same package — **no new production package needed for CHK-05** [CITED: masstransit.massient.com/configuration/schedulers — see Sources] |
| MassTransit.EntityFrameworkCore | 8.3.6 (pinned) | Saga persistence (existing `Order`/`OrderStateMachine`), new Payments outbox/inbox | Same pattern Orders already uses |
| Riok.Mapperly | 4.3.1 | DTO mapping | Matches `OrderMapper` pattern; reuse for `PaymentDto`/`CheckoutStatusDto` if needed |
| Npgsql.EntityFrameworkCore.PostgreSQL / Aspire.Npgsql.EntityFrameworkCore.PostgreSQL | 13.4.4 (Aspire integration, matches Orders.csproj) | Payments' new idempotency-key store | Payments currently has **no** database reference in `ecommerce.AppHost/Program.cs` — must be added this phase |

### Supporting (new for this phase)

| Library | Version | Purpose | When to Use |
|---------|---------|---------|-------------|
| `MassTransit.Quartz` | **8.3.6** (pin exactly — see Package Legitimacy Audit) | In-memory Quartz scheduler for the xUnit saga-timeout test (D-05) | Test project only (`ECommerce.Orders.Tests`); NOT referenced by `ECommerce.Orders.API` production code |

### Alternatives Considered

| Instead of | Could Use | Tradeoff |
|------------|-----------|----------|
| ASB-native scheduler (prod) | Quartz.NET with a persistent (Postgres) job store, used for both prod and test | More consistent across environments and sidesteps the documented ASB unschedule race condition (GitHub #3753), but adds a new subsystem/package to production and a new Quartz schema — not justified for a single 15-minute one-shot timeout. Revisit only if the ASB emulator spike (see Common Pitfalls) fails. |
| Typed `PaymentAuthorised`/`PaymentFailed` saga events | Reusing the generic `OrderStatusChanged` string-matched event (existing pattern) for payment outcomes too | Reuse is less code, but conflates "read-model status change" with "domain outcome," and loses the `FailureReason`/`Amount` fields naturally — typed events are recommended for CHK-03/CHK-04's compensation paths, `OrderStatusChanged` still used as the sole event the read-model projector consumes |
| Checkout.API calling Orders.API synchronously over HTTP for `POST /checkout` | Checkout.API publishing a `StartCheckout` **command** consumed asynchronously by Orders' saga | HTTP call keeps the 202-Accepted contract trivially satisfiable and avoids a bus round-trip before `checkoutId` can be returned; an async command would force `POST /checkout` to either block waiting for saga creation or return an unconfirmed id. HTTP call chosen — see Architecture Patterns. |

**Installation:**
```bash
# Test project only:
dotnet add src/services/orders/ECommerce.Orders.Tests package MassTransit.Quartz --version 8.3.6

# Payments needs a database for the first time — mirrors Orders' existing package set:
dotnet add src/services/payments/ECommerce.Payments.API package Aspire.Npgsql.EntityFrameworkCore.PostgreSQL --version 13.4.4
dotnet add src/services/payments/ECommerce.Payments.API package MassTransit --version 8.3.6
dotnet add src/services/payments/ECommerce.Payments.API package MassTransit.Azure.ServiceBus.Core --version 8.3.6
dotnet add src/services/payments/ECommerce.Payments.API package MassTransit.EntityFrameworkCore --version 8.3.6
dotnet add src/services/payments/ECommerce.Payments.API package Microsoft.EntityFrameworkCore.Design --version 10.0.9
```

**Version verification:** `MassTransit`/`MassTransit.Azure.ServiceBus.Core`/`MassTransit.EntityFrameworkCore` are already pinned at 8.3.6 in `ECommerce.Orders.API.csproj` — reuse that exact pin for Payments, do not re-resolve. `MassTransit.Quartz` 8.3.6 was directly confirmed to exist on nuget.org with a dependency on `MassTransit >= 8.3.6` (not the 9.x commercial line) — see Package Legitimacy Audit.

## Package Legitimacy Audit

> This is a .NET/NuGet project — `slopcheck` (built for npm/PyPI) does not apply. Verified directly against nuget.org via WebFetch instead. Confidence tagged accordingly.

| Package | Registry | Age | Downloads | Source Repo | Verification | Disposition |
|---------|----------|-----|-----------|--------------|---------------|-------------|
| `MassTransit.Quartz` @ **8.3.6** | nuget.org | Published 2025-01-31 (same day as `MassTransit` 8.3.6) | Part of the MassTransit family (234M+ total across `MassTransit` package; per-version count for `MassTransit.Quartz` not separately confirmed) | github.com/MassTransit/MassTransit (mono-repo) | [CITED: nuget.org/packages/MassTransit.Quartz/8.3.6 — confirmed exists, depends on `MassTransit >= 8.3.6`, `Quartz >= 3.13.1`, `Quartz.Extensions.Hosting >= 3.13.1`] | **Approved for test project only — MUST pin the exact version string `8.3.6`, never `*` or omitted** |
| `MassTransit.Quartz` @ **9.2.0 (latest)** | nuget.org | 2026-07-27 | 17.3M | github.com/**Massient**/MassTransit (renamed org) | [VERIFIED: nuget.org] Depends on `MassTransit >= 9.2.0` — the **commercial** license line per ADR-0006 | **REJECTED — do not `dotnet add` without `--version 8.3.6`, the default/latest resolves into the same license trap ADR-0006 already warns about for core MassTransit** |
| `Quartz` (transitive, via `MassTransit.Quartz` 8.3.6) | nuget.org | mature, Apache-2.0, long-standing | very high | github.com/quartznet/quartznet | [CITED: MassTransit.Quartz 8.3.6 dependency manifest] | Approved (transitive, Apache-2.0, no floating version — resolved by the pinned parent package) |

**Packages removed due to a legitimacy verdict:** none.
**Packages flagged as suspicious:** none — but flag the **version-pin discipline** itself as a required checkpoint: adding `MassTransit.Quartz` without `--version 8.3.6` silently pulls MassTransit 9.x transitively, exactly the trap ADR-0006 already documents for the core package. Planner should insert a `checkpoint:human-verify` (or at minimum a lint/CI check on `.csproj` version pins) before this package is installed.

## Architecture Patterns

### Saga State Reconciliation (the foundational discretionary call)

**The tension:** ORD-03 (locked, Phase 3, already implemented and tested) requires the persisted `Order`/`OrderReadModel.Status` vocabulary to be exactly `Pending → Paid → Fulfilled / Cancelled / Failed`. SC2 (Phase 4 roadmap) describes the checkout-facing vocabulary as `Started → AwaitingPayment → Paid`. These are not in conflict if treated as **two views of one state machine**, not two vocabularies fighting for the same field.

**Recommendation:** Extend `OrderStateMachine` in place:

1. Add one new leading state: `Started`. The saga instance is created in this state, **before** the `Order` aggregate/read-model row exists.
2. Keep the existing `Pending` state exactly as built in Phase 3 (do not rename it in code — ORD-03 and the existing `OrderReadModelProjector`/`OrderStateMachineTests` depend on the literal string `"Pending"`). `Pending` is what "AwaitingPayment" *means* from the checkout's point of view.
3. Keep `Paid`, `Cancelled`, `Failed` as-is. Add one optional new intermediate state `Refunding` for CHK-04 (see below) — this is a **discretionary addition beyond SC2's literal three-state list**; flag for planner/UI-phase confirmation since it adds a fourth visible step to the demo stepper (D-06 says "or the failure/cancellation equivalent," which leaves room for this).
4. `GET /checkout/{id}` (Checkout.API) exposes a **mapped** name, never the raw `CurrentState`:

   | Saga `CurrentState` | Checkout-facing status (SC2 vocabulary) | Order-facing status (ORD-03, unchanged) |
   |---|---|---|
   | `Started` | `Started` | *(no read-model row yet — see Common Pitfalls)* |
   | `Pending` | `AwaitingPayment` | `Pending` |
   | `Paid` | `Paid` | `Paid` |
   | `Refunding` (if adopted) | `Cancelling` (or fold into `Cancelled`) | `Paid` (unchanged until refund confirms) |
   | `Cancelled` | `Cancelled` | `Cancelled` |
   | `Failed` | `Failed` | `Failed` |

This mapping is a small `static` dictionary or `switch` expression in Checkout.API — not a new persisted field, and it changes zero existing Orders code paths.

**Why not a distinct pre-order "checkout" saga (CONTEXT.md's third option)?** ADR-0005 explicitly locks *one* orchestrating state machine. A second saga would either duplicate `OrderId`/`CorrelationId` handling or require a saga-to-saga handoff message, both of which reintroduce the choreography-style indirection ADR-0005 rejected. Extending in place keeps `CorrelationId == OrderId == CheckoutId` as a single id end-to-end (see below).

### `checkoutId == orderId` — single correlation id

`Order.CorrelationId` already *is* the `OrderId` (see `Order.cs` comment: "there is no separate Id property"). Recommend Checkout.API **mints the id** (a `Guid`) at `POST /checkout` time and treats it as the `OrderId` from the very first message. This means:
- No mapping table needed in Checkout.API between "checkoutId" and "orderId" — they are the same GUID.
- `GET /checkout/{id}` and `GET /orders/{id}` can both be satisfied by looking up the same saga/read-model row by the same id.

### Checkout.API as a thin façade (confirms CONTEXT.md discretion point)

Checkout.API holds **no database of its own**. Its two endpoints:

- `POST /checkout` — validates the caller is authenticated, mints `checkoutId = Guid.NewGuid()`, makes a **synchronous internal HTTP call** to a new Orders.API endpoint (e.g. `POST /orders/checkout` — the real successor to Phase 3's `POST /orders/test-create-from-cart`) forwarding the bearer token (same pattern Orders→Cart already uses via `ICartClient`), passing the minted id. Orders.API fetches the cart, validates non-empty, and publishes `StartCheckout` (or directly initiates the saga's `Initially()` transition) through its own outbox. Returns `202 Accepted` with `{ checkoutId }` per CHK-01. **Rationale for HTTP-not-bus:** publishing a fire-and-forget command and returning 202 immediately risks a race where `GET /checkout/{id}` 404s before the saga instance exists (see Common Pitfalls); a synchronous hop that waits for the saga's `Started` transition to be durably persisted (i.e. the internal call returns only after Orders' `SaveChangesAsync` flushes the outbox) closes that window, mirroring the existing test-trigger endpoint's `await db.SaveChangesAsync(ct)` ordering.
- `GET /checkout/{id}` — synchronous internal HTTP call to Orders.API for saga status (a lightweight new endpoint, or extend the existing `GET /orders/{id}` response), maps `CurrentState` → checkout vocabulary per the table above, returns `404` (IDOR-safe, same pattern as `GET /orders/{id}`) if not found or not owned by the caller.
- `POST /checkout/{id}/simulate-fulfillment-failure` (demo-only, D-01–D-03) — validates the order is `Paid` (proxy check via Orders), then **publishes `FulfillmentFailed` directly onto the bus** via `IPublishEndpoint` (Checkout.API needs its own lightweight MassTransit producer-only registration — outbox not required since it's a demo trigger, not a domain source of truth). No real Fulfillment.API round-trip needed.

Requires: `ecommerce.AppHost/Program.cs` — Checkout must be declared **after** Orders (matching the existing "declaration order matters for `.WithReference()`" comment already in that file) and gain `.WithReference(orders)`; Gateway must gain `.WithReference(checkout)`; `Gateway.API/appsettings.json` needs a new `checkout-route`/cluster entry for `/api/checkout/**`.

### System Architecture Diagram

```
Angular /checkout (signals, polling ~1-2s)
        │ POST /checkout                         │ GET /checkout/{id} (poll)
        ▼                                         ▼
   YARP Gateway  ──/api/checkout/**──────►  Checkout.API (thin façade, no DB)
                                              │  (sync HTTP, forwards bearer token)
                                              ▼
                                          Orders.API
                                    ┌─────────────────────────────┐
                                    │  OrderStateMachine (saga)    │
                                    │  Started ─► Pending ─► Paid  │
                                    │              │         │     │
                                    │      (timeout│Schedule)│FulfillmentFailed
                                    │              ▼         ▼     │
                                    │          Cancelled  Refunding│
                                    │                        │     │
                                    │                        ▼     │
                                    │                    Cancelled │
                                    └───────────┬──────────────────┘
                          publishes             │  publishes
                    OrderCreated/OrderStatusChanged   AuthorisePayment / RefundPayment
                          (outbox, ASB)          │        (outbox, ASB, topic="orders-events")
                                    ▼             ▼
                       OrderReadModelProjector   Payments.API (new PaymentsDb, idempotent by checkoutId)
                          (inbox, Postgres)              │
                                    │                     publishes PaymentAuthorised / PaymentFailed /
                                    ▼                     PaymentRefunded (outbox, topic="payments-events")
                              OrderReadModel  ◄───────────┘  (consumed back by OrderStateMachine)
                          (GET /orders/{id} — ORD-03 vocabulary + FailureReason)
```

### Recommended Project Structure (additions only)

```
src/services/orders/ECommerce.Orders.API/Features/Orders/
├── OrderStateMachine.cs        # extended: Started, Refunding states; new typed events; Schedule/Unschedule
├── Order.cs                    # add FailureReason (nullable string)
├── OrderReadModel.cs           # add FailureReason (nullable string)
├── OrderReadModelProjector.cs  # OrderStatusChanged handler copies FailureReason across
├── CheckoutOptions.cs          # new: binds Checkout:TimeoutMinutes (D-04)
└── OrdersEndpoints.cs          # POST /orders/checkout replaces test-create-from-cart; add internal status endpoint for Checkout.API

src/services/checkout/ECommerce.Checkout.API/Features/Checkout/
├── CheckoutEndpoints.cs        # POST /checkout, GET /checkout/{id}, POST /checkout/{id}/simulate-fulfillment-failure
├── IOrdersClient.cs            # mirrors ICartClient pattern
└── CheckoutStatusDto.cs        # saga-vocabulary mapping table

src/services/payments/ECommerce.Payments.API/
├── Data/PaymentsDbContext.cs   # new — first DB for this service
├── Features/Payments/
│   ├── ProcessedPayment.cs     # idempotency-key entity, unique index on CheckoutId
│   ├── AuthorisePaymentConsumer.cs
│   └── RefundPaymentConsumer.cs

src/building-blocks/Contracts/
├── Checkout/Commands/V1/StartCheckout.cs      # replaces Placeholder.cs
├── Payments/Commands/V1/AuthorisePayment.cs   # replaces Placeholder.cs
├── Payments/Commands/V1/RefundPayment.cs
├── Payments/Events/V1/PaymentAuthorised.cs    # replaces Placeholder.cs
├── Payments/Events/V1/PaymentFailed.cs
├── Payments/Events/V1/PaymentRefunded.cs
└── Fulfillment/Events/V1/FulfillmentFailed.cs # replaces Placeholder.cs

src/frontend/ecommerce-app/src/app/features/
├── checkout/checkout-page/           # NEW feature folder — none exists yet
│   ├── checkout-page.component.ts    # mat-stepper, polling, hint text, demo toggle (D-06/D-07/D-10/D-11)
├── orders/order-detail/              # NEW feature folder — none exists yet (see Common Pitfalls)
│   └── order-detail.component.ts     # status + FailureReason display (D-09)
└── core/services/checkout.service.ts # mirrors cart.service.ts's thin HttpClient wrapper pattern
```

### Pattern 1: Typed saga events replacing/supplementing generic status events

**What:** `OrderStateMachine` currently reacts only to a generic, stringly-typed `OrderStatusChanged` event for `Pending→Paid/Cancelled/Failed`. For Phase 4, add **typed** domain events (`PaymentAuthorised`, `PaymentFailed`, `FulfillmentFailed`) as the actual saga triggers, and have their `.Then()` activities **also publish** `OrderStatusChanged` (unchanged contract, `+FailureReason` field) purely so the existing `OrderReadModelProjector` keeps working without modification to its `OrderCreated` handler.

**When to use:** Any saga transition that needs to carry domain data (amount, failure reason) the generic status-change event doesn't have.

**Example (illustrative — plan/execution finalizes exact shape):**
```csharp
// Source: pattern synthesized from MassTransit official docs (Schedule/Unschedule, see Sources)
// applied to this repo's existing OrderStateMachine.cs
Event(() => PaymentAuthorisedEvent, x => x.CorrelateById(m => m.Message.CheckoutId));
Event(() => PaymentFailedEvent, x => x.CorrelateById(m => m.Message.CheckoutId));

During(Pending,
    When(PaymentAuthorisedEvent)
        .Unschedule(CheckoutTimeout)
        .Then(ctx => ctx.Saga.UpdatedAt = ctx.Message.AuthorisedAt)
        .Publish(ctx => new OrderStatusChanged(
            Guid.NewGuid(), ctx.Saga.CorrelationId, ctx.Message.MessageId, DateTimeOffset.UtcNow,
            ctx.Saga.CorrelationId, "Pending", "Paid", ctx.Message.AuthorisedAt, FailureReason: null))
        .TransitionTo(Paid),
    When(PaymentFailedEvent)
        .Unschedule(CheckoutTimeout)
        .Then(ctx => ctx.Saga.FailureReason = ctx.Message.Reason)
        .Publish(ctx => new OrderStatusChanged(
            Guid.NewGuid(), ctx.Saga.CorrelationId, ctx.Message.MessageId, DateTimeOffset.UtcNow,
            ctx.Saga.CorrelationId, "Pending", "Cancelled", ctx.Message.FailedAt, ctx.Message.Reason))
        .TransitionTo(Cancelled),
    When(CheckoutTimeoutExpired)
        .Then(ctx => ctx.Saga.FailureReason = "Checkout timed out before payment completed")
        .Publish(ctx => new OrderStatusChanged(
            Guid.NewGuid(), ctx.Saga.CorrelationId, Guid.Empty, DateTimeOffset.UtcNow,
            ctx.Saga.CorrelationId, "Pending", "Cancelled", DateTimeOffset.UtcNow, ctx.Saga.FailureReason))
        .TransitionTo(Cancelled));
```

### Pattern 2: Scheduled timeout via `Schedule`/`Unschedule` (CHK-05, highest technical risk)

**What:** MassTransit state machines support declaring a `Schedule<TSaga, TMessage>` with a `Delay` and a `Received` correlation, triggering it with `.Schedule(...)` inside a `When()` chain (dynamic per-message delay is supported by passing a delay-selector function), and cancelling it with `.Unschedule(...)`. The scheduled-message delivery itself is backed by whatever `IMessageScheduler` is registered — **this must be transport-aware**.

```csharp
// Source: [CITED: masstransit.massient.com/guides/saga-state-machines/schedule-event]
public class OrderStateMachine : MassTransitStateMachine<Order>
{
    public Schedule<Order, CheckoutTimeoutExpired> CheckoutTimeout { get; private set; } = null!;

    public OrderStateMachine()
    {
        Schedule(() => CheckoutTimeout, instance => instance.CheckoutTimeoutTokenId, s =>
        {
            s.Delay = TimeSpan.FromMinutes(15); // overridden per-message below via CheckoutOptions
            s.Received = r => r.CorrelateById(context => context.Message.OrderId);
        });

        During(Started,
            When(OrderCreatedEvent)
                .Then(/* existing Initially() body, moved here */)
                .Schedule(CheckoutTimeout,
                    context => context.Init<CheckoutTimeoutExpired>(new { OrderId = context.Saga.CorrelationId }),
                    context => TimeSpan.FromMinutes(checkoutOptions.TimeoutMinutes)) // D-04: injectable
                .TransitionTo(Pending));
    }
}
```

**Production transport (recommended):** Azure Service Bus native scheduling — no new package (`MassTransit.Azure.ServiceBus.Core` already referenced):
```csharp
// Source: [CITED: masstransit.massient.com/configuration/schedulers]
builder.Services.AddMassTransit(x =>
{
    x.AddServiceBusMessageScheduler();
    // ...existing saga/outbox config...
    x.UsingAzureServiceBus((context, cfg) =>
    {
        cfg.Host(messagingConnectionString);
        cfg.UseServiceBusMessageScheduler();
        cfg.ConfigureEndpoints(context);
    });
});
```

**Test transport (D-05, in-memory harness):** the existing `OrderStateMachineSteps` pattern (`AddMassTransitTestHarness` + `InMemoryRepository()`) has **no** message scheduler by default — `.Schedule()` will throw without one configured. Add a Quartz in-memory scheduler **in the test project only**:
```csharp
// Source: pattern from MassTransit's own QuartzInMemoryTestFixture
// [CITED: github.com/MassTransit/MassTransit/blob/develop/tests/MassTransit.QuartzIntegration.Tests/QuartzInMemoryTestFixture.cs]
services.AddMassTransitTestHarness(x =>
{
    x.AddSagaStateMachine<OrderStateMachine, Order>().InMemoryRepository();
    x.UsingInMemory((context, cfg) =>
    {
        cfg.UseInMemoryScheduler(); // MassTransit.Quartz 8.3.6 — non-durable, test-only
        cfg.ConfigureEndpoints(context);
    });
});
```
The D-05 test then configures `Checkout:TimeoutMinutes` to a tiny fraction (e.g. 3–5 seconds via a raw `TimeSpan` override, not literally "minutes") and **really waits** that short real-world interval using `harness.Consumed.Any<CheckoutTimeoutExpired>(timeout: TimeSpan.FromSeconds(15))` — this is simpler and more version-portable than time-travel APIs (`IInMemoryDelayProvider.Advance()` surfaced in research but appears to be a newer/unconfirmed-version feature — do not depend on it without re-verifying against the installed MassTransit version at execution time).

### Pattern 3: Idempotent Payments (PAY-03)

**What:** MassTransit's EF Core inbox already dedupes by **transport MessageId** (INF-02, applies platform-wide) — this is necessary but not sufficient for PAY-03, which requires dedup by the **business key** `CheckoutId` (a retried/duplicated `AuthorisePayment` command with a *different* MessageId but the *same* CheckoutId must not double-process). Add a dedicated table:

```csharp
// Source: synthesized — standard idempotent-receiver pattern, no external citation needed
public class ProcessedPayment
{
    public Guid CheckoutId { get; set; }   // unique index — the idempotency key (PAY-03)
    public string Outcome { get; set; } = default!; // "Authorised" | "Failed"
    public decimal Amount { get; set; }
    public string? FailureReason { get; set; }
    public DateTimeOffset ProcessedAt { get; set; }
}
// EF Core: modelBuilder.Entity<ProcessedPayment>().HasIndex(p => p.CheckoutId).IsUnique();
```

Consumer logic: look up by `CheckoutId` first; if found, **republish the stored outcome** (do not recompute/re-decide) so redelivery is safe even though the `.99` rule is itself deterministic — this also protects against the rule changing in a future version. On first sight, decide via the `.99` rule (PAY-02), insert transactionally, then publish.

## Don't Hand-Roll

| Problem | Don't Build | Use Instead | Why |
|---------|-------------|--------------|-----|
| Delayed/scheduled saga timeout | A `BackgroundService` polling loop or `Task.Delay` inside a consumer | MassTransit `Schedule`/`Unschedule` + ASB-native scheduler | Survives process restarts (ASB persists the scheduled message server-side), integrates with saga correlation, supports cancellation — a polling loop would need its own durable storage and race-handling to match this |
| Payment idempotency | Checking "have I seen this MessageId" only (relying on MassTransit inbox alone) | A dedicated `CheckoutId`-unique table (Pattern 3 above) | MessageId-based inbox dedupes *transport redelivery*, not *business-level duplicate intents* — PAY-03 explicitly asks for the latter |
| Failure-reason display logic | Per-page ad-hoc string formatting of raw saga state names in Angular | Server-computed human-readable `FailureReason` string persisted on `OrderReadModel`, Angular just renders it | Keeps the "what does 'Failed' mean" business logic in one place (the saga), not duplicated across `/checkout` and `/orders/:id` |

**Key insight:** Every "don't hand-roll" here already has a working analog elsewhere in this codebase (outbox/inbox, `AnyAsync` idempotency guard in `OrderReadModelProjector`) — Phase 4's job is to apply the same primitives to two new concerns (scheduling, cross-service idempotency), not invent new ones.

## Common Pitfalls

### Pitfall 1: ASB emulator scheduled-message fidelity is unconfirmed
**What goes wrong:** Native ASB message scheduling (`ScheduledEnqueueTimeUtc`) is well-documented for live Azure Service Bus, but the local Docker emulator (`mcr.microsoft.com/azure-messaging/servicebus-emulator`, already used via Aspire's `RunAsEmulator()` per ADR-0002) has known feature gaps versus cloud (ADR-0002 already flags "no sessions in some emulator versions"). Scheduled-message support specifically was **not** confirmed or denied in official emulator-limitations docs during this research pass.
**Why it happens:** The emulator is a relatively new (as of ADR-0002's writing) SQL-Server-backed local approximation of ASB, not a full reimplementation.
**How to avoid:** Run a Wave 0 spike: start the AppHost, manually schedule one message via `Schedule()` with a 10-second delay against the emulator, confirm delivery. If it fails, fall back to the Quartz.NET-for-both-environments alternative documented in "Alternatives Considered."
**Warning signs:** A scheduled message that never arrives when running against `docker compose`/Aspire locally, despite working against a real Azure namespace.

### Pitfall 2: ASB unschedule race condition (documented upstream)
**What goes wrong:** GitHub MassTransit/MassTransit#3753 documents a race where a scheduled message unscheduled very close to its delivery time can still be delivered, causing an "event not handled during this state" fault.
**Why it happens:** ASB's cancel-scheduled-message API and delivery are not transactionally coupled; there's a timing window.
**How to avoid:** For this phase's 15-minute (or few-second test) delay, the window is a tiny fraction of the total delay, so the practical risk is low — but keep the existing state machine's **trailing catch-all combinator pattern** (already used for `OrderStatusChangedEvent` in `Pending`/`Paid`) applied to `CheckoutTimeoutExpired` too, so a stray late-arriving timeout event in a state that's already moved on (e.g. already `Paid`) is absorbed rather than faulting the saga.
**Warning signs:** Rare `UnhandledEventException` faults correlated with payment authorization landing within ~1 second of the configured timeout.

### Pitfall 3: `GET /checkout/{id}` race immediately after `202 Accepted`
**What goes wrong:** If `POST /checkout` returns before the saga instance is durably created (outbox flushed), an immediate client poll can 404.
**Why it happens:** Eventual consistency between the HTTP response and the async saga/read-model pipeline.
**How to avoid:** Per Architecture Patterns above, make the internal `Checkout.API → Orders.API` HTTP call **synchronous and wait for `SaveChangesAsync`** (same ordering already used in Phase 3's test-trigger endpoint) before Checkout.API returns 202 — this guarantees the saga instance exists in `Started` state by the time the client receives the id and starts polling.
**Warning signs:** Flaky "order not found" on the very first poll in integration tests or live demos, especially under load.

### Pitfall 4: No `/orders/:id` Angular page exists yet
**What goes wrong:** CONTEXT.md's code_context section describes `/orders/:id` as something that "gets a failure-reason display added" — but `src/frontend/ecommerce-app/src/app/features/orders/` **does not exist** (verified via glob: only `auth/`, `catalog/`, `cart/` feature folders exist; `app.routes.ts` has no `orders` route). Phase 3 built the API (ORD-01/02) but no Angular view.
**Why it happens:** Phase 3's CONTEXT.md flagged this as an open question ("confirm during planning whether a minimal orders view is still needed") that was apparently resolved as API-only for Phase 3.
**How to avoid:** Phase 4 planning must scope building the **entire** `/orders/:id` page (routing, component, service call to `GET /orders/{id}`), not just "add a field to an existing page." This is materially more work than CONTEXT.md's phrasing implies — flag explicitly in the plan's task breakdown.
**Warning signs:** Underestimating FE-03's Angular scope.

### Pitfall 5: AppHost declaration order and missing references
**What goes wrong:** `ecommerce.AppHost/Program.cs` currently declares `checkout` **before** `orders` (line 33 vs line 38) and neither `checkout` nor `payments` reference `postgres`; `gateway` has no reference to `checkout`.
**Why it happens:** These were Phase 1 stubs wired before Phase 4's real dependencies were known.
**How to avoid:** Move the `checkout` project declaration to after `orders` (the file already has a comment documenting why declaration order matters for `.WithReference()`), add `.WithReference(orders)` to checkout, add `.WithReference(postgres).WaitFor(postgres)` to `payments`, add `.WithReference(checkout)` to `gateway`, and add the matching YARP route/cluster in `Gateway.API/appsettings.json`.
**Warning signs:** `Unable to resolve service for type 'Orders'` or connection-refused errors from Checkout.API at Aspire startup.

### Pitfall 6: Forwarding bearer tokens over the message bus
**What goes wrong:** It's tempting to put the caller's JWT in a `StartCheckout`/`AuthorisePayment` message so a downstream consumer can re-call another service — but MassTransit messages are persisted (outbox, potentially DLQ) and logged (OTel), turning a short-lived bearer token into semi-durable, widely-visible data.
**Why it happens:** Looks like the easiest way to thread auth context through an async pipeline.
**How to avoid:** Keep bearer-token forwarding confined to **synchronous HTTP hops only** (Checkout.API→Orders.API, Orders.API→Cart.API — both already established patterns). Once the flow crosses onto the bus (Orders→Payments), authorize by service-to-service trust (internal network, no public route) plus message-level correlation, not a forwarded user token.
**Warning signs:** A `BearerToken` or `AccessToken` field appearing in any `Contracts/**/V1/*.cs` record.

## Code Examples

### `POST /checkout` — Checkout.API façade
```csharp
// Source: pattern adapted from OrdersEndpoints.cs's existing test-create-from-cart handler
app.MapPost("/checkout", async (
    HttpContext httpContext, ClaimsPrincipal user, IOrdersClient ordersClient, CancellationToken ct) =>
{
    var checkoutId = Guid.NewGuid();
    var token = ExtractBearerToken(httpContext);

    var result = await ordersClient.StartCheckoutAsync(checkoutId, token, ct); // sync HTTP, awaits outbox flush
    if (result is null)
        return Results.BadRequest(new { error = "Cart is empty." });

    return Results.Accepted($"/checkout/{checkoutId}", new { checkoutId });
}).RequireAuthorization();
```

### Contract shapes (replace `Placeholder.cs` files)
```csharp
// src/building-blocks/Contracts/Payments/Commands/V1/AuthorisePayment.cs
using ECommerce.Contracts;
namespace ECommerce.Payments.Commands.V1;

public record AuthorisePayment(
    Guid MessageId, Guid CorrelationId, Guid CausationId, DateTimeOffset OccurredAt,
    Guid CheckoutId, decimal Amount
) : IMessageEnvelope;

// src/building-blocks/Contracts/Payments/Events/V1/PaymentFailed.cs
namespace ECommerce.Payments.Events.V1;

public record PaymentFailed(
    Guid MessageId, Guid CorrelationId, Guid CausationId, DateTimeOffset OccurredAt,
    Guid CheckoutId, decimal Amount, string Reason, DateTimeOffset FailedAt
) : IMessageEnvelope;
```

## State of the Art

| Old Approach | Current Approach | When Changed | Impact |
|--------------|-------------------|---------------|--------|
| MassTransit docs at masstransit.io / masstransit-project.com | Redirects (307) to `masstransit.massient.com` | Observed during this research pass (2026-08) | The MassTransit project/org appears to have been rebranded/acquired to "Massient" — consistent with ADR-0006's existing note about a "Massient commercial license" for v9. Treat `masstransit.massient.com` as the current official docs domain, but be aware it is the same commercial entity whose license v8.3.6 deliberately avoids — do not let this domain's own "latest" install instructions silently resolve packages to 9.x. |
| `MassTransit-Quartz` (old, pre-v3, separate GitHub repo) | `MassTransit.Quartz` (NuGet package, versioned alongside core MassTransit) | Long-standing (pre-dates this project) | Confirms the correct package name to search for/pin is `MassTransit.Quartz`, not `MassTransit-Quartz` |

**Deprecated/outdated:** `masstransit-project.com`, `masstransit-v6.netlify.app`, `masstransit-v7.netlify.app` — legacy version-pinned doc mirrors surfaced repeatedly in search results; not used as sources below except where explicitly noting version history.

## Assumptions Log

| # | Claim | Section | Risk if Wrong |
|---|-------|---------|----------------|
| A1 | `AddServiceBusMessageScheduler()`/`UseServiceBusMessageScheduler()` work unmodified against MassTransit 8.3.6 specifically (docs fetched were not version-pinned to 8.3.6) | Standard Stack, Pattern 2 | If the API differs in 8.3.6, the scheduling wiring needs adjustment at execution time — low risk, this API has existed since early v8 per training knowledge, but not independently re-verified against the 8.3.6 changelog |
| A2 | Azure Service Bus emulator supports scheduled message delivery (`ScheduledEnqueueTimeUtc`) with fidelity close enough to cloud ASB for local dev/demo | Common Pitfalls #1 | If unsupported, local `docker compose`/Aspire demos of CHK-05 would silently never fire the timeout — recommend the Wave 0 spike explicitly for this reason |
| A3 | `IInMemoryDelayProvider.Advance()` exists and is usable in MassTransit 8.3.6 specifically | Pattern 2 (explicitly NOT recommended, flagged as unconfirmed) | Not relied upon in the recommended approach — informational only |
| A4 | The MassTransit project/org rebrand to "Massient" (redirect observed on masstransit.io) is a genuine current-state fact and not a training-data-confusion artifact of this research session | State of the Art | If wrong (e.g. a temporary redirect/outage), sources cited as `masstransit.massient.com` should be re-verified against `masstransit.io` directly at execution time |
| A5 | `MassTransit.Quartz` 8.3.6's `Received =` correlation and `UseInMemoryScheduler()` API surface matches the `develop`-branch `QuartzInMemoryTestFixture` example fetched (that fixture's exact commit/version was not pinned to 8.3.6) | Pattern 2 | If the API shape differs slightly in 8.3.6, the test-setup code needs minor adjustment — functionally low risk since this is test-only code |

**If this table is empty:** N/A — see rows above. All ASB-scheduling-specific claims are flagged; state-machine architecture claims (Sections "Saga State Reconciliation" through "Don't Hand-Roll") are grounded directly in this repo's own code, not external sources, and are not included in this table.

## Open Questions

1. **Does the ASB emulator actually deliver scheduled messages?**
   - What we know: Native ASB scheduling API exists and is documented for cloud; emulator limitations docs don't explicitly confirm or deny scheduled-message support.
   - What's unclear: Whether a local `docker compose`/Aspire demo run will actually see the CHK-05 timeout fire without a live Azure namespace.
   - Recommendation: Wave 0 spike (manual `Schedule()` call with a 10s delay against the running emulator) before committing task plans to the ASB-native approach; fall back to Quartz.NET-for-both-environments if it fails.

2. **Should `Refunding` be a real visible saga state, or should CHK-04 go `Paid → Cancelled` directly with a fire-and-forget refund?**
   - What we know: D-06 explicitly allows "the failure/cancellation equivalent" beyond the literal three states, and D-09 wants a specific human-readable reason ("Fulfillment failed — order cancelled and refunded") that reads naturally as a single terminal message.
   - What's unclear: Whether the demo value of an extra visible stepper step outweighs the added state-machine complexity.
   - Recommendation: Default to the simpler `Paid → Cancelled` direct transition (publish `RefundPayment` as a `.Then()` side effect, don't wait for `PaymentRefunded` to transition) unless the UI-phase pass wants the extra visible step — flag for `/gsd-plan-phase` to decide explicitly rather than leaving it implicit.

3. **Exact route/host for `POST /orders/checkout`** (the real successor to `POST /orders/test-create-from-cart`) vs. keeping Checkout.API's internal call thin.
   - What we know: Orders.API already owns `ICartClient` and the cart-fetch-and-`OrderCreated`-publish logic.
   - What's unclear: Whether this becomes a new public-but-internal-only Orders endpoint, or logic is moved/duplicated into Checkout.API with its own `ICartClient`.
   - Recommendation: Reuse Orders.API's existing `ICartClient`/cart-fetch logic (single new internal endpoint on Orders.API) rather than duplicating it in Checkout.API — less code, one owner of "how an order gets created from a cart."

## Environment Availability

| Dependency | Required By | Available | Version | Fallback |
|------------|--------------|-----------|---------|----------|
| .NET SDK 10 | All new/modified services | Not directly probed this session (no bash `dotnet --version` run — informational only, prior phases already built and ran successfully on this stack) | 10.x expected | — |
| Azure Service Bus emulator (Docker) | CHK-05 timeout scheduling, all pub/sub in this phase | Assumed available via existing Aspire `RunAsEmulator()` wiring (ADR-0002) — not independently re-verified this session | — | Quartz.NET-for-both-environments (see Alternatives Considered) if the Wave 0 spike (Open Question 1) fails |
| PostgreSQL (Docker, via Aspire) | New PaymentsDb | Assumed available — same shared Aspire `postgres` resource Orders/Notifications already use | — | — |

**Missing dependencies with no fallback:** none identified — this phase only adds to already-provisioned infrastructure (Postgres, ASB emulator), it does not introduce a new infrastructure dependency class.

## Security Domain

### Applicable ASVS Categories

| ASVS Category | Applies | Standard Control |
|----------------|---------|--------------------|
| V2 Authentication | yes | Existing JWT bearer (OpenIddict-issued) on every new public endpoint (`POST /checkout`, `GET /checkout/{id}`, `POST /checkout/{id}/simulate-fulfillment-failure`) — `.RequireAuthorization()`, same as `OrdersEndpoints` |
| V3 Session Management | no (new surface) | No new session state — stateless JWT per existing pattern |
| V4 Access Control | yes | `GET /checkout/{id}` must enforce the same IDOR-safe ownership check as `GET /orders/{id}` (identical 404 for not-found vs. not-owned) — do not skip this because Checkout.API "just proxies" |
| V5 Input Validation | yes | `checkoutId` must be a valid `Guid` (Minimal API route constraint `{id:guid}`); reject empty carts server-side (already the pattern in the Phase 3 test-trigger endpoint) |
| V6 Cryptography | no | No new crypto surface — payments are simulated, no real card data ever touches the system (explicit project constraint) |
| V7 Error Handling / Logging | yes | `FailureReason` strings are user-facing (D-09) — ensure they never leak internal exception details/stack traces, only the curated business-reason strings this phase defines |

### Known Threat Patterns for this stack

| Pattern | STRIDE | Standard Mitigation |
|---------|--------|-----------------------|
| IDOR on `GET /checkout/{id}` (guessing another user's checkoutId) | Information Disclosure | Same 404-for-both pattern already proven in `OrdersEndpoints.MapGet("/orders/{id:guid}")` — reuse verbatim |
| Demo trigger abuse — any authenticated user cancelling/refunding *another* user's paid order via `POST /checkout/{id}/simulate-fulfillment-failure` | Elevation of Privilege / Tampering | Even though D-03 says "no timing restriction," it does **not** say "no ownership restriction" — enforce the same ownership check as `GET /checkout/{id}` before publishing `FulfillmentFailed` |
| Duplicate/replayed `AuthorisePayment` causing a double charge in a real-payment future (or duplicate `PaymentAuthorised` events confusing the saga now) | Repudiation / Tampering | PAY-03's `CheckoutId`-unique idempotency table (Pattern 3) |
| Bearer token leakage via message bus persistence (outbox/DLQ/OTel logs) | Information Disclosure | Pitfall 6 — never place tokens in `Contracts/**` message payloads |

## Sources

### Primary (HIGH confidence)
- This repository: `docs/adr/0002` through `0008`, `Order.cs`, `OrderStateMachine.cs`, `OrderStateMachineTests.cs`/`Steps.cs`, `OrdersEndpoints.cs`, `Program.cs` (Orders/Checkout/Payments/Fulfillment), `ecommerce.AppHost/Program.cs`, `Gateway.API/appsettings.json`, `.csproj` files, Angular `cart-page.component.ts`/`cart.service.ts`/`app.routes.ts` — read directly this session
- nuget.org package pages for `MassTransit` 8.3.6 and `MassTransit.Quartz` 8.3.6 / 9.2.0 — fetched directly, version/dependency data confirmed

### Secondary (MEDIUM confidence)
- masstransit.massient.com (current redirect target of masstransit.io) — `/guides/saga-state-machines/schedule-event`, `/configuration/schedulers`, `/documentation/patterns/saga/state-machine` — fetched directly; code examples cross-checked against training knowledge of the MassTransit v8 API surface, but not independently pinned to the 8.3.6 tag specifically
- [MassTransit GitHub Discussion #5887 — delayed message scheduling test practices](https://github.com/MassTransit/MassTransit/discussions/5887) — fetched, informed the "wait a short real interval" test recommendation over time-travel APIs
- [MassTransit GitHub Issue #3753 — ASB unschedule race condition](https://github.com/MassTransit/MassTransit/issues/3753) — fetched, informed Common Pitfall 2
- [QuartzInMemoryTestFixture.cs, MassTransit `develop` branch](https://github.com/MassTransit/MassTransit/blob/develop/tests/MassTransit.QuartzIntegration.Tests/QuartzInMemoryTestFixture.cs) — fetched, informed Pattern 2's test-setup code (branch not pinned to 8.3.6 tag — see Assumption A5)

### Tertiary (LOW confidence)
- General WebSearch result summaries (not independently fetched) on Azure Service Bus emulator scheduled-message support — informed Open Question 1 and Common Pitfall 1, explicitly flagged as unconfirmed rather than stated as fact

## Metadata

**Confidence breakdown:**
- Standard stack (MassTransit pins, Payments DB addition): HIGH — grounded in existing repo conventions and directly-fetched nuget.org data
- Architecture (saga state reconciliation, contracts, Checkout.API façade): HIGH — synthesized directly from this repo's existing, tested code plus locked ADRs; no external dependency
- Scheduling mechanics (CHK-05 timeout): MEDIUM-LOW — this is the flagged highest-risk area; recommendation is defensible and cross-referenced across 3+ sources, but the ASB-emulator-specific behavior is unconfirmed and needs a Wave 0 spike before planning locks in the ASB-native approach
- Pitfalls: MEDIUM-HIGH — most are grounded in direct repo inspection (missing AppHost references, missing Angular folder); the two ASB-specific pitfalls are MEDIUM (documented upstream issues, not independently reproduced)

**Research date:** 2026-08-08
**Valid until:** 2026-09-07 (30 days — MassTransit's apparent org/domain change and the unconfirmed ASB emulator scheduling behavior both warrant re-checking sooner if execution is delayed)
