# Phase 5: Fulfillment & Notifications - Research

**Researched:** 2026-08-14
**Domain:** MassTransit message scheduling, stateless event-driven consumers, EF Core outbox/inbox idempotency, Angular polling/read-model UI
**Confidence:** HIGH (all recommendations are grounded directly in this repository's existing, shipped Phase 2-4 code — not external libraries)

## Summary

Phase 5 is almost entirely a **replication exercise**, not a new-technology exercise. Every mechanism this phase needs (scheduled delayed messages, EF Core transactional outbox/inbox, JWT-scoped read endpoints, saga defensive `Ignore(...)` catch-alls, Angular polling) already exists and ships in this exact codebase from Phases 2-4. The research below is almost entirely codebase archaeology: locating the exact pattern to copy for each of Fulfillment's and Notifications' new responsibilities, and flagging the handful of places where the CONTEXT.md's working assumptions do not quite match what's on disk.

Three corrections to CONTEXT.md's framing surfaced during research, all resolved here:

1. **The Orders saga's timeout does NOT use "MassTransit.Quartz" in production.** The `MassTransit.Quartz` NuGet package is referenced only to obtain `UseInMemoryScheduler()` for the in-memory/test transport branch. The **live/production path uses MassTransit's Azure Service Bus-native scheduler** (`AddServiceBusMessageScheduler()` + `cfg.UseServiceBusMessageScheduler()`), which was deliberately chosen over Quartz-for-both-environments per ADR-0009's spike result. Fulfillment must replicate this **exact dual-branch pattern**, not a Quartz-only one.
2. **`OrderShipped` does not need its own command/consumer pair.** MassTransit's `ConsumeContext.SchedulePublish<T>()` extension lets a single stateless consumer schedule the *actual* `OrderShipped` event for future delivery in one call — no intermediate "ProcessShipment" command or second consumer required. This resolves Claude's Discretion item 2 in the simplest possible direction.
3. **`.WithReference(postgres)` is missing from Notifications' AppHost registration today** (a pre-existing gap, not something Phase 5 broke) and will also be needed for Fulfillment once it gets a DbContext. Both must be added/fixed in Phase 5's AppHost wiring or the services cannot resolve their `"postgres"` connection string outside of WebApplicationFactory-based tests.

**Primary recommendation:** Build Fulfillment as a stateless MassTransit consumer (no saga/state machine) that filters `OrderStatusChanged` on `NewStatus == "Paid"` and calls `context.SchedulePublish<OrderShipped>(delay, ...)`; give it its own EF Core DbContext purely for the transactional outbox/inbox (INF-01/INF-02), not business state. Build Notifications as three parallel stateless consumers (mirroring `CatalogSeededConsumer`) writing to a new `NotificationEntry` table, exposed via a JWT-scoped `GET /notifications` mirroring `OrdersEndpoints`'s `GetUserId`/`RequireAuthorization` pattern. Add `UserId` to `OrderStatusChanged`, `AuthorisePayment`, and `PaymentFailed` contracts (4 files, ~9 call sites) to satisfy D-03 without any synchronous cross-service lookups.

<phase_requirements>
## Phase Requirements

| ID | Description | Research Support |
|----|-------------|------------------|
| FUL-01 | Fulfillment service consumes OrderPaid events and starts processing | Reuse existing `OrderStatusChanged` event (already published by Orders on the Pending→Paid transition) filtered to `NewStatus == "Paid"`, rather than adding a new dedicated `OrderPaid` contract type — see "Don't Hand-Roll" and Code Examples. Resolves Claude's Discretion item 1. |
| FUL-02 | Fulfillment publishes OrderShipped after timer-based processing simulation | `context.SchedulePublish<OrderShipped>()` using the same dual-branch scheduler pattern as `OrderStateMachine`'s `CheckoutTimeout` — see Architecture Patterns Pattern 1 and Code Examples. Resolves Claude's Discretion items 2 and 4. |
| NOT-01 | User can view their in-app notification inbox (GET /notifications) | `GET /notifications` mirrors `OrdersEndpoints`'s `GetUserId(ClaimsPrincipal)` + `.RequireAuthorization()` pattern exactly; gateway route already exists (no gateway change needed) — see Architecture Patterns and Security Domain. |
| NOT-02 | Notifications service consumes saga events (OrderPaid, OrderShipped, PaymentFailed) and persists inbox entries, idempotently | Three stateless consumers mirroring `CatalogSeededConsumer`, relying on MassTransit's EF Core `InboxState` dedup by transport `MessageId` (verified below) — no custom business-key dedup needed. Test mirrors `CatalogSeededInboxDeduplicationSteps`. |

</phase_requirements>

<user_constraints>
## User Constraints (from CONTEXT.md)

### Locked Decisions

**Fulfillment Timer & Failure Trigger (FUL-01, FUL-02)**
- D-01: The simulated shipping delay is configurable (e.g. `Fulfillment:ProcessingSeconds`), defaulting to ~30s-1min, overridable to a short interval for tests. Reuse the same MassTransit scheduling mechanism already wired for the Orders saga's `CheckoutTimeout`. *(Research note: this mechanism is the ASB-native scheduler in production, MassTransit.Quartz's `UseInMemoryScheduler()` only in the in-memory/test branch — see Summary.)*
- D-02: Fulfillment always succeeds — no deterministic natural-failure trigger this phase. Phase 4's `POST /checkout/{id}/simulate-fulfillment-failure` demo endpoint remains the only way to exercise `FulfillmentFailed` — it is NOT retired or touched.

**Notification Content & User Scoping (NOT-01, NOT-02)**
- D-03: The events Notifications consumes (`OrderStatusChanged`/whichever "OrderPaid" signal is chosen, the new `OrderShipped`, and `PaymentFailed`) each gain a `UserId` field, populated by their publishers. No synchronous service-to-service lookup calls.
- D-04: Each inbox entry shows a short templated human-readable message plus an order reference (OrderId/CheckoutId) — e.g. "Your order has been paid", "Your order has shipped", "Payment failed for your order." Not a raw event-type/timestamp dump.

**Notifications UI Surface (NOT-01)**
- D-05: Adds a minimal Angular `/notifications` page (mat-list style) listing inbox entries with links to `/orders/:id`. Flag this phase for a `/gsd-ui-phase 5` pass even though ROADMAP.md doesn't mark a UI hint.
- D-06: The notifications page gets a simple nav bar link — no unread-count badge (deferred to NOT-V2-01).

**Order Detail Visibility During Fulfillment (SC1/SC2)**
- D-07: `/orders/:id` shows a "Shipping..." style indicator while the order is `Paid` but not yet `Fulfilled` — client-side inference from `status == Paid`, no new persisted state, no new `OrderStateMachine` state.
- D-08: `/orders/:id` reuses Phase 4's existing polling pattern (~1-2s interval), extending its stop-condition to include `Fulfilled` in addition to `Cancelled`/`Failed`. *(Research note: `/orders/:id` currently has ZERO polling on disk — see Common Pitfalls Pitfall 1. The pattern to reuse lives on `/checkout`, not `/orders/:id`; net-new polling code must be added to `order-detail.component.ts`.)*

### Claude's Discretion

- Exact contract/event name and shape for the "order paid" signal Fulfillment consumes — new dedicated `OrderPaid` event vs. filtering existing `OrderStatusChanged`. **Resolved by this research: reuse `OrderStatusChanged` filtered to `NewStatus == "Paid"`** — see Architecture Patterns.
- How the Orders saga is wired to react to `OrderShipped` (new `Event<OrderShipped>` binding on `During(Paid, ...)`, mirroring `FulfillmentFailedEvent`) — mechanical; exact call sites enumerated in Code Examples.
- Whether Fulfillment needs its own persisted state (EF Core DB + outbox/inbox) vs. relying on MassTransit's own inbox/scheduling primitives. **Resolved: Fulfillment needs an EF Core DbContext for the transactional outbox/inbox tables (INF-01/INF-02 apply uniformly), but NOT a saga/state machine or custom business-key idempotency table** — MassTransit's `InboxState` dedup (verified below) is sufficient.
- Exact `Fulfillment:ProcessingSeconds` config key name/shape. **Recommendation: `Fulfillment:ProcessingSeconds` as a `double`, default `45`**, mirroring `CheckoutOptions.TimeoutMinutes`'s double-for-sub-unit-test-overrides pattern.
- Notifications inbox message copy wording — unchanged from D-04's examples, Claude may polish.
- Exact visual treatment of "Shipping..." indicator — deferred to `/gsd-ui-phase 5`.

### Deferred Ideas (OUT OF SCOPE)

- Deterministic natural failure trigger for Fulfillment (PAY-02-style `.99` equivalent) — explicitly rejected (D-02).
- Mark notifications as read (NOT-V2-01) — V2.
- Real email/SMS delivery (NOT-V2-02) — V2.
- Unread-count badge on the notifications nav link (D-06) — needs read/unread state this phase doesn't track.
</user_constraints>

## Project Constraints (from CLAUDE.md)

- .NET 10, MassTransit 8.3.6 pinned exactly (Apache-2.0; **never** let it float to 9.x, which is commercially licensed) — `<PackageReference Include="MassTransit" Version="8.3.6" />` in every new/touched `.csproj`.
- EF Core 10 + Npgsql for persistence; PostgreSQL, not SQL Server.
- Every publishing service uses the MassTransit transactional outbox (INF-01); every consuming service uses the idempotent inbox (INF-02) — applies to both Fulfillment (publisher+consumer) and Notifications (consumer-only).
- ASP.NET Core Minimal APIs, not MVC controllers.
- `System.Text.Json`, not Newtonsoft.
- Angular 20, standalone components, signals, zoneless — no NgModules, no Zone.js.
- MADR-format ADRs in `docs/adr/` for any new architectural decision.
- Vertical-slice feature-folder structure (`Features/{Feature}/...`) per existing Orders/Payments precedent.

## Architectural Responsibility Map

| Capability | Primary Tier | Secondary Tier | Rationale |
|------------|-------------|----------------|-----------|
| Order-paid signal detection | API/Backend (Orders, existing) | — | Already published by `OrderStateMachine`'s Pending→Paid transition; no new publish site needed, only a new field. |
| Timer-based shipment simulation | API/Backend (Fulfillment, new) | — | Pure backend event-driven processing; no UI/CDN involvement. |
| Shipment-complete signal | API/Backend (Fulfillment, new) | — | New `OrderShipped` event, consumed by Orders saga (API/Backend) to close the loop. |
| Saga terminal-state transition (Paid→Fulfilled) | API/Backend (Orders, existing saga) | — | ADR-0005 locks orchestration in the Orders saga — Fulfillment does not orchestrate, only reports. |
| Notification persistence | API/Backend (Notifications, new) + Database/Storage | — | Consumer writes to Notifications' own Postgres DB; idempotency via MassTransit `InboxState`. |
| Notification inbox retrieval | API/Backend (Notifications, new) | — | `GET /notifications`, JWT-scoped, same tier as `GET /orders`. |
| "Shipping..." order-status indicator | Browser/Client (Angular) | — | D-07: purely a client-side inference from `status == "Paid"`, no new backend state. |
| Order-detail live status polling | Browser/Client (Angular) | API/Backend (existing `GET /orders/{id}`) | Client polls an existing, unchanged read endpoint; no new backend polling infrastructure. |
| Notifications inbox page | Browser/Client (Angular, new) | API/Backend (`GET /notifications`) | D-05: new `/notifications` route + nav link, consumes the new endpoint. |

## Standard Stack

### Core (all already pinned and running elsewhere in this repo — no new package names introduced)

| Library | Version | Purpose | Why Standard |
|---------|---------|---------|--------------|
| MassTransit | 8.3.6 | Messaging, consumers, scheduling | ADR-0006 pin; already used by every backend service. |
| MassTransit.Azure.ServiceBus.Core | 8.3.6 | ASB transport | Same as above; needed by Fulfillment's production branch. |
| MassTransit.EntityFrameworkCore | 8.3.6 | EF Core saga/outbox/inbox repository | Needed by Fulfillment (outbox+inbox) and already present in Notifications. |
| MassTransit.Quartz | 8.3.6 | Provides `UseInMemoryScheduler()` for the in-memory/test transport branch ONLY | Already referenced by `ECommerce.Orders.API.csproj`; Fulfillment needs the identical reference for its own in-memory/test branch. **Not used in production** — the ASB-native scheduler handles delayed `OrderShipped` in production. |
| Aspire.Npgsql.EntityFrameworkCore.PostgreSQL | 13.4.4 | Aspire-integrated Npgsql DbContext registration | Already used by every service with a DbContext (Orders, Payments, Notifications). |
| Microsoft.EntityFrameworkCore.Design | 10.0.9 | `dotnet ef migrations` tooling | Needed for Fulfillment's first migration (new project) and Notifications' new `NotificationEntry` migration. |
| Microsoft.AspNetCore.Authentication.JwtBearer | 10.0.8 | Validates OpenIddict-issued tokens | Needed newly by Notifications (currently has zero auth wiring) — mirrors Orders' exact `AddJwtBearer` block. |
| Microsoft.AspNetCore.OpenApi | 10.0.8 | OpenAPI generation | Already referenced by both stub projects; no change. |

### Supporting

| Library | Version | Purpose | When to Use |
|---------|---------|---------|-------------|
| Serilog.AspNetCore / Serilog.Sinks.OpenTelemetry | 10.0.0 / 4.2.0 | Structured logging | Already wired in both stub `Program.cs` files; unchanged. |
| OpenTelemetry.* (Extensions.Hosting, Instrumentation.AspNetCore, Exporter.OpenTelemetryProtocol) | 1.15.3 / 1.15.2 / 1.15.3 | Tracing (INF-03) | Already wired in both stub `Program.cs` files; unchanged. |

### Test-side (new: Fulfillment has no Tests project yet)

| Library | Version | Purpose |
|---------|---------|---------|
| xunit.v3 | 3.2.2 | Test framework, matches every other service's Tests project |
| FluentAssertions | 8.10.0 | Assertions |
| NSubstitute | 5.3.0 | Mocking (only where a Tests project needs it, e.g. Payments-style) |
| Microsoft.AspNetCore.Mvc.Testing | 10.0.9 | `WebApplicationFactory` for Notifications' new HTTP endpoint tests |
| MassTransit.TestFramework | 8.3.6 | In-memory bus test harness for consumer + forced-redelivery tests |
| Testcontainers.PostgreSql | 4.12.0 | Real Postgres for EF Core outbox/inbox integration tests |
| Microsoft.EntityFrameworkCore(.Relational/.InMemory) | 10.0.9 | Matches `Aspire.Npgsql.EntityFrameworkCore.PostgreSQL` 13.4.4's EF Core version requirement |

### Alternatives Considered

| Instead of | Could Use | Tradeoff |
|------------|-----------|----------|
| Stateless consumer + `SchedulePublish` for Fulfillment | Full `MassTransitStateMachine` saga (like `OrderStateMachine`) | A saga adds a persisted state machine, correlation table, and `Schedule()`/`Unschedule()` ceremony for a flow with exactly one linear step and zero compensation branches (D-02: always succeeds). Not justified by FUL-01/FUL-02's actual scope — reserve sagas for flows with real branching/compensation. |
| Filtering `OrderStatusChanged` on `NewStatus == "Paid"` | New dedicated `OrderPaid` contract event | A new event type duplicates information already carried by `OrderStatusChanged` and requires a second `.Publish()` call in the saga (more contract surface, more entities against ADR-0007's per-context topic budget) for no additional expressiveness — the existing event, filtered, already conveys exactly this signal (the Orders saga's own `During(Pending, ...)` uses the identical filter idiom). |

**Installation (Fulfillment — no packages currently referenced beyond OpenApi/Serilog/OTel/Contracts):**
```bash
dotnet add src/services/fulfillment/ECommerce.Fulfillment.API package MassTransit --version 8.3.6
dotnet add src/services/fulfillment/ECommerce.Fulfillment.API package MassTransit.Azure.ServiceBus.Core --version 8.3.6
dotnet add src/services/fulfillment/ECommerce.Fulfillment.API package MassTransit.EntityFrameworkCore --version 8.3.6
dotnet add src/services/fulfillment/ECommerce.Fulfillment.API package MassTransit.Quartz --version 8.3.6
dotnet add src/services/fulfillment/ECommerce.Fulfillment.API package Aspire.Npgsql.EntityFrameworkCore.PostgreSQL --version 13.4.4
dotnet add src/services/fulfillment/ECommerce.Fulfillment.API package Microsoft.EntityFrameworkCore.Design --version 10.0.9
```

**Installation (Notifications — adding auth):**
```bash
dotnet add src/services/notifications/ECommerce.Notifications.API package Microsoft.AspNetCore.Authentication.JwtBearer --version 10.0.8
```

**Version verification:** All versions above were read directly from `.csproj` files already committed and building in this repository (`ECommerce.Orders.API.csproj`, `ECommerce.Payments.API.csproj`, `ECommerce.Notifications.API.csproj`) — not from NuGet.org lookups or training data. This is the strongest possible verification available: these exact package+version combinations are already proven to restore and run together under .NET 10 in this repo's existing CI/build.

## Package Legitimacy Audit

No new/unfamiliar package names are introduced by this phase. Every package listed above is copy-pasted from a `.csproj` file that already exists and already builds elsewhere in this repository — `dotnet nuget`/npm-style slopsquat risk (a hallucinated or malicious package name) does not apply, because these are not newly-discovered names from search or training data; they are transcribed directly from this codebase's own already-vetted, already-running dependency tree. `slopcheck` targets npm/PyPI and has no NuGet-ecosystem support, so it was not run — this is a deliberate, justified skip, not a gap.

| Package | Registry | Evidence | Disposition |
|---------|----------|----------|-------------|
| MassTransit / MassTransit.Azure.ServiceBus.Core / MassTransit.EntityFrameworkCore / MassTransit.Quartz | NuGet | Already referenced (identical version) in `ECommerce.Orders.API.csproj`, verified via ADR-0006 | Approved — [VERIFIED: in-repo] |
| Aspire.Npgsql.EntityFrameworkCore.PostgreSQL | NuGet | Already referenced (identical version) in `ECommerce.Orders.API.csproj` / `ECommerce.Payments.API.csproj` | Approved — [VERIFIED: in-repo] |
| Microsoft.AspNetCore.Authentication.JwtBearer | NuGet | Already referenced (identical version) in `ECommerce.Orders.API.csproj` | Approved — [VERIFIED: in-repo] |
| Microsoft.EntityFrameworkCore.Design | NuGet | Already referenced (identical version) in `ECommerce.Payments.API.csproj` / `ECommerce.Orders.API.csproj` | Approved — [VERIFIED: in-repo] |

**Packages removed due to slopcheck [SLOP] verdict:** none — slopcheck not applicable (NuGet ecosystem).
**Packages flagged as suspicious [SUS]:** none.

## Architecture Patterns

### System Architecture Diagram

```
Orders (saga, existing)                Fulfillment (new)                  Notifications (new)
┌─────────────────────────┐            ┌──────────────────────┐           ┌───────────────────────────┐
│ OrderStateMachine        │            │ OrderPaidConsumer     │           │ OrderPaidNotifConsumer     │
│  Pending --PaymentAuth--> Paid        │  IConsumer<           │           │  IConsumer<OrderStatus     │
│                           │  │        │   OrderStatusChanged> │           │   Changed> (filter Paid)   │
│  .Publish(OrderStatus-    │  │  ──────>  if NewStatus!="Paid" │           │   -> insert NotificationEntry
│    Changed{NewStatus=     │  │   (ASB    return;               │           │      ("Your order has been │
│    "Paid", UserId})       │  │  topic:  context.SchedulePublish│           │       paid.")               │
│                           │  │  orders- <OrderShipped>(delay,  │           │                             │
│  Paid --OrderShipped--    │  │  events) new OrderShipped(...)) │           │ OrderShippedConsumer        │
│    TransitionTo(Fulfilled)│  │        └──────────┬───────────┘           │  IConsumer<OrderShipped>    │
│  .Publish(OrderStatus-    │  │                   │ (delay elapses;       │   -> insert NotificationEntry
│    Changed{NewStatus=     │  │                   │  ASB-native or        │      ("Your order has       │
│    "Fulfilled"})          │<─┼───────────────────┘  Quartz-in-memory     │       shipped.")             │
│                           │  │        scheduler actually publishes)      │                             │
│  During(*) catch-alls:    │  │                                            │ PaymentFailedNotifConsumer  │
│    Ignore(OrderShipped-   │<─┼──── PaymentFailed (from Payments, topic:  │  IConsumer<PaymentFailed>   │
│    Event) in every other  │       payments-events) ──────────────────────>│   -> insert NotificationEntry
│    state                  │                                                │      ("Payment failed for   │
└─────────────────────────┘                                                │       your order.")         │
                                                                             │                             │
                                                                             │ GET /notifications          │
                                                                             │  (JWT-scoped, WHERE UserId  │
Angular /orders/:id  <──── GET /orders/{id} (unchanged, polled) ────────────│   = claims.sub)             │
Angular /notifications <── GET /notifications (new) ────────────────────────┘
```

Entry points: `OrderCreated` (unchanged, existing) drives the saga into `Pending`; `PaymentAuthorised`/`PaymentFailed` (existing, from Payments) drive `Pending→Paid`/`Cancelled`. This phase adds one new outbound edge from Orders (`OrderStatusChanged{NewStatus="Paid"}` now also read by Fulfillment) and two new services subscribing to the existing `orders-events`/`payments-events` topics plus a new `fulfillment-events` topic for `OrderShipped`.

### Recommended Project Structure

```
src/services/fulfillment/ECommerce.Fulfillment.API/
├── Data/
│   ├── FulfillmentDbContext.cs      # AddInboxStateEntity/AddOutboxMessageEntity/AddOutboxStateEntity only — no business table
│   └── DbInitializer.cs             # IHostedService, db.Database.MigrateAsync() — mirrors Notifications/Payments
├── Features/Fulfillment/
│   ├── FulfillmentOptions.cs        # ProcessingSeconds (double), mirrors CheckoutOptions
│   └── OrderPaidConsumer.cs         # IConsumer<OrderStatusChanged>, filters NewStatus=="Paid", SchedulePublish<OrderShipped>
├── Migrations/                       # dotnet ef migrations add InitialFulfillmentSchema
└── Program.cs                        # dual-branch scheduler wiring (mirrors Orders exactly)

src/services/fulfillment/ECommerce.Fulfillment.Tests/   # NEW PROJECT — does not exist yet
├── Integration/
│   ├── OrderPaidConsumerTests.cs / Steps.cs             # two-class pattern
│   └── OrderPaidInboxDeduplicationTests.cs / Steps.cs   # forced-redelivery, mirrors Notifications' pattern

src/services/notifications/ECommerce.Notifications.API/
├── Data/
│   ├── NotificationsDbContext.cs    # add DbSet<NotificationEntry>, keep existing inbox/outbox entities
│   └── DbInitializer.cs             # unchanged
├── Consumers/
│   ├── CatalogSeededConsumer.cs     # unchanged (Phase 2)
│   ├── OrderPaidNotificationConsumer.cs      # IConsumer<OrderStatusChanged>, filters NewStatus=="Paid"
│   ├── OrderShippedNotificationConsumer.cs   # IConsumer<OrderShipped>
│   └── PaymentFailedNotificationConsumer.cs  # IConsumer<PaymentFailed>
├── Features/Notifications/
│   ├── NotificationEntry.cs         # entity: Id, UserId, OrderId, Message, EventType, OccurredAt
│   └── NotificationsEndpoints.cs    # GET /notifications, JWT-scoped, mirrors OrdersEndpoints.GetUserId
├── Migrations/                       # dotnet ef migrations add AddNotificationEntry
└── Program.cs                        # add AddAuthentication/AddJwtBearer/AddAuthorization + 3 new consumers
```

### Pattern 1: Timer-based delayed publish without a saga

**What:** A stateless `IConsumer<T>` schedules a *different* message type to be published at a future time, using `ConsumeContext.SchedulePublish<TMessage>()`. The scheduler (ASB-native in production, Quartz in-memory in tests) holds the message and performs the actual publish itself — no second consumer or "wake up" command needed.
**When to use:** Any linear, non-branching "wait then emit" flow with no compensation logic — exactly FUL-01/FUL-02's scope (D-02: Fulfillment always succeeds).
**Example — dual-branch scheduler wiring (mirrors `ECommerce.Orders.API/Program.cs` lines 58-88):**
```csharp
// Source: src/services/orders/ECommerce.Orders.API/Program.cs (existing, verified in this repo)
builder.Services.AddMassTransit(x =>
{
    x.AddConsumer<OrderPaidConsumer>();

    x.AddEntityFrameworkOutbox<FulfillmentDbContext>(o =>
    {
        o.UsePostgres();
        o.UseBusOutbox(); // Fulfillment publishes OrderShipped, so the bus outbox drainer is required
    });

    x.AddConfigureEndpointsCallback((context, name, cfg) =>
    {
        cfg.UseEntityFrameworkOutbox<FulfillmentDbContext>(context);
    });

    var messagingConnectionString = builder.Configuration.GetConnectionString("messaging");
    if (messagingConnectionString == "placeholder")
    {
        // Same "placeholder" test sentinel used by Orders/Payments/Checkout.
        x.UsingInMemory((context, cfg) =>
        {
            cfg.UseInMemoryScheduler(); // MassTransit.Quartz-backed, in-memory, non-durable
            cfg.ConfigureEndpoints(context);
        });
    }
    else
    {
        x.AddServiceBusMessageScheduler();
        x.UsingAzureServiceBus((context, cfg) =>
        {
            cfg.Host(messagingConnectionString);
            cfg.UseServiceBusMessageScheduler(); // ASB ScheduledEnqueueTime — production path
            cfg.ConfigureEndpoints(context);
        });
    }
});
```

**Example — the consumer itself:**
```csharp
// New file: src/services/fulfillment/ECommerce.Fulfillment.API/Features/Fulfillment/OrderPaidConsumer.cs
using ECommerce.Fulfillment.Events.V1;
using ECommerce.Orders.Events.V1;
using MassTransit;
using Microsoft.Extensions.Options;

namespace ECommerce.Fulfillment.API.Features.Fulfillment;

public class OrderPaidConsumer(IOptions<FulfillmentOptions> options) : IConsumer<OrderStatusChanged>
{
    public async Task Consume(ConsumeContext<OrderStatusChanged> context)
    {
        var msg = context.Message;

        // Fulfillment cares only about the Paid transition — every other NewStatus (Cancelled,
        // Failed, Fulfilled) is a no-op here, mirroring the filter idiom OrderStateMachine
        // already uses for the same event (When(OrderStatusChangedEvent, ctx => ctx.Message
        // .NewStatus == "Paid")). The message is still acked/inbox-recorded either way.
        if (msg.NewStatus != "Paid")
            return;

        var delay = TimeSpan.FromSeconds(options.Value.ProcessingSeconds);
        var now = DateTimeOffset.UtcNow;

        await context.SchedulePublish(delay, new OrderShipped(
            MessageId: Guid.NewGuid(),
            CorrelationId: msg.OrderId,
            CausationId: msg.MessageId,
            OccurredAt: now,
            CheckoutId: msg.OrderId,
            UserId: msg.UserId,
            ShippedAt: now + delay));
    }
}
```

### Pattern 2: Idempotent stateless consumer (no business-key dedup table needed)

**What:** MassTransit's EF Core inbox (`AddInboxStateEntity()` + `UseEntityFrameworkOutbox<TContext>(context)` on the receive endpoint) deduplicates by `(MessageId, ConsumerId)` **before** the consumer body executes — a redelivered message with the same transport `MessageId` does not re-invoke `Consume()`.
**When to use:** Every new consumer this phase (`OrderPaidConsumer`, all three Notifications consumers) — this is why NO custom "have I already processed OrderId X" check (like `ProcessedPayment` in Payments) is needed for either service. Payments needs its own business-key table because it must *replay a stored outcome* on redelivery (the outcome is non-deterministic-looking to a caller — Authorised vs Failed); Fulfillment and Notifications don't replay anything, they just skip re-execution entirely, which the inbox already guarantees.
**Verified via:** MassTransit's documented duplicate-detection-window/InboxState behavior (WebSearch, cross-referenced against this repo's own `CatalogSeededInboxDeduplicationSteps.cs`, which already proves exactly-once `InboxState` row creation for a duplicate-`MessageId` publish in this codebase).

### Pattern 3: JWT-scoped, IDOR-safe list endpoint

**What:** Extract `UserId` from `ClaimsPrincipal` via `ClaimTypes.NameIdentifier`/`"sub"` fallback, scope the EF Core query with `.Where(n => n.UserId == userId)`, require `[Authorize]`/`.RequireAuthorization()`.
**When to use:** `GET /notifications` — directly copy `OrdersEndpoints.GetUserId` and the `GET /orders` list-endpoint shape (not the single-resource 404-parity shape, since a list endpoint is naturally scoped by its `WHERE` clause with no existence-leak risk).
```csharp
// Source: src/services/orders/ECommerce.Orders.API/Features/Orders/OrdersEndpoints.cs (existing, verified)
private static string GetUserId(ClaimsPrincipal user) =>
    user.FindFirstValue(ClaimTypes.NameIdentifier)
    ?? user.FindFirstValue("sub")
    ?? throw new InvalidOperationException("Authenticated request missing a user id claim.");
```

### Anti-Patterns to Avoid

- **Building a `MassTransitStateMachine` for Fulfillment:** D-02 fixes the flow as always-succeed, single-step, no compensation — a saga's correlation table, `Initially`/`During`/`Ignore` ceremony buys nothing here and adds a persisted-state surface with no branching logic to justify it.
- **Adding a new `OrderPaid` contract type:** Duplicates `OrderStatusChanged{NewStatus="Paid"}`'s information, doubles the Orders saga's publish sites for the same semantic event, and adds an ASB topic-budget entity for zero added expressiveness (see Alternatives Considered).
- **Giving `PaymentAuthorised` a `UserId` field "for symmetry":** Notifications does not consume `PaymentAuthorised` (only `PaymentFailed`) — adding an unused field widens the contract change's blast radius (more call sites to touch in `AuthorisePaymentConsumer.cs`/tests) with no consumer to justify it. Keep the D-03 change scoped to exactly the three events actually consumed.

## Don't Hand-Roll

| Problem | Don't Build | Use Instead | Why |
|---------|-------------|-------------|-----|
| Delaying a message send by N seconds | A `BackgroundService`/`Task.Delay` inside the consumer, or a custom "due date" polling table | `ConsumeContext.SchedulePublish<T>()` / `IMessageScheduler` (ASB-native scheduler in prod, Quartz in-memory in tests) | Holding a consumer thread open for 30-60s ties up the receive endpoint's concurrency limit and risks the ASB message-lock timeout firing mid-wait; a custom polling table reinvents exactly what `Schedule()`/`SchedulePublish()` already does transactionally via the outbox. |
| Redelivery deduplication for a simple "run once" consumer | A custom `ProcessedX` table keyed by business ID (Payments' pattern) | MassTransit's EF Core `InboxState` (already wired via `AddInboxStateEntity()` + `UseEntityFrameworkOutbox` on the receive endpoint) | `InboxState` already deduplicates by transport `MessageId` before the consumer body runs — a business-key table is only needed when the consumer must *replay a stored outcome* on redelivery (Payments' non-deterministic authorise/decline), which is not the case here. |
| Determining which user a notification belongs to | A synchronous HTTP call from Notifications back to Orders/Identity | `UserId` carried on the event itself (D-03) | Preserves the "no direct coupling" Core Value premise (PROJECT.md); a synchronous lookup would reintroduce the exact coupling the event-driven architecture exists to avoid. |

**Key insight:** Every "hard part" of this phase (scheduling, idempotency, auth scoping) already has a proven, shipped implementation elsewhere in this exact repository. The discipline required is *finding and copying* those patterns precisely, not inventing new ones.

## Common Pitfalls

### Pitfall 1: `/orders/:id` has no polling today — D-08 describes "extending" code that doesn't exist there
**What goes wrong:** A plan that says "extend the existing polling stop-condition on `/orders/:id`" will fail at implementation time because `order-detail.component.ts` (read in full during this research) performs exactly one `ordersService.getOrder(id).subscribe(...)` call in `ngOnInit` — no `interval()`, no `switchMap`, no `takeWhile`. The polling *pattern* CONTEXT.md is referring to lives on `/checkout` (`checkout-page.component.ts`'s `startPolling` method), and that page's own `TERMINAL_STATUSES` array **already includes `'Fulfilled'`** — it needs no change.
**Why it happens:** CONTEXT.md's D-08 was written assuming both pages share one polling implementation; in fact only `/checkout` has one, and it already navigates away to `/orders/:id` once ANY terminal status (including `Fulfilled`) is reached.
**How to avoid:** Plan a net-new `interval(1500).pipe(switchMap(...), takeWhile(...), takeUntilDestroyed(...))` block in `order-detail.component.ts`, structurally copied from `checkout-page.component.ts`'s `startPolling`, with terminal statuses `['Fulfilled', 'Cancelled', 'Failed']`. This closes the real gap: a user who navigates directly to `/orders/:id` (order history link, page refresh, bookmark) while the order is still `Paid` currently sees a static snapshot forever, not a live-updating one.
**Warning signs:** A task titled "extend order-detail polling" with no task to *add* an `interval`/`switchMap`/`takeWhile` block is under-scoped.

### Pitfall 2: `[EntityName]`/topic-per-context (ADR-0007) is a decided-but-unimplemented ADR
**What goes wrong:** A plan that assumes Fulfillment/Notifications must apply `[EntityName("orders-events")]` overrides to subscribe correctly will discover **zero such overrides exist anywhere in the codebase today** (`grep -rn "EntityName" src/` returns nothing). Every service today uses MassTransit's default topology (`cfg.ConfigureEndpoints(context)` with no entity-name customization), which auto-creates one topic per message type and one subscription per consumer — this still works correctly for cross-service pub/sub (each consumer subscribes by declaring `IConsumer<T>`), it just doesn't match ADR-0007's stated one-topic-per-*context* intent.
**Why it happens:** ADR-0007 was written and accepted in Phase 1, but no phase since has actually needed a second service to subscribe to a first service's topic until now (Notifications' only prior consumer, `CatalogSeededConsumer`, is its first cross-service subscription too, and it also relies on MassTransit defaults).
**How to avoid:** Phase 5 does NOT need to fix this gap to ship FUL-01/02/NOT-01/02 correctly — MassTransit's default per-message-type topics will route `OrderStatusChanged`, `OrderShipped`, and `PaymentFailed` to their new subscribers correctly regardless. Flag it for the planner as a known, pre-existing architecture debt item (candidate for Phase 6/hardening), not a Phase 5 blocker. Current message-type-with-a-publisher count after this phase: 8 (`CatalogSeeded`, `OrderCreated`, `OrderStatusChanged`, `PaymentAuthorised`, `PaymentFailed`, `PaymentRefunded`, `FulfillmentFailed`, `OrderShipped`) — still well under the ASB emulator's 50-entity limit even counting per-consumer subscriptions.
**Warning signs:** A task that says "apply `[EntityName]` to `OrderStatusChanged`/`OrderShipped`" as a Phase 5 requirement — this is scope creep beyond FUL-01/02/NOT-01/02 and should be called out to the user as an optional add-on, not assumed.

### Pitfall 3: AppHost is missing `.WithReference(postgres)` for Notifications today, and needs it added for Fulfillment
**What goes wrong:** `src/ecommerce.AppHost/Program.cs` (read in full) shows `notifications` registered with only `.WithHttpEndpoint(...)` and `.WithReference(serviceBus)` — **no `.WithReference(postgres)`/`.WaitFor(postgres)`**, despite `NotificationsDbContext` already existing and being resolved via `builder.AddNpgsqlDbContext<NotificationsDbContext>("postgres")` in `Program.cs`. Without the AppHost reference, Aspire cannot inject the `ConnectionStrings:postgres` value at orchestration time — the service will fail to start (or silently use a stale/missing connection string) outside of `WebApplicationFactory`-based tests, which inject their own connection string directly.
**Why it happens:** Likely an oversight from Phase 2 when Notifications' DbContext was first scaffolded but the service had no real consumer/endpoint yet to surface the failure.
**How to avoid:** Phase 5 must add `.WithReference(postgres).WaitFor(postgres)` to the `notifications` registration (fixing the pre-existing gap) AND to the new `fulfillment` registration (which currently has neither `postgres` nor any DB dependency at all, since it's a bare stub).
**Warning signs:** `docker compose up` / `dotnet run` on the AppHost succeeds but Notifications/Fulfillment throw `Npgsql.NpgsqlException`/connection-string-null errors at migration time.

### Pitfall 4: Adding `UserId` to `OrderStatusChanged`/`AuthorisePayment`/`PaymentFailed` has a wider blast radius than "add one field"
**What goes wrong:** These are C# `record` types constructed positionally-or-named at multiple call sites across 4 files: `OrderStateMachine.cs` (5 `new OrderStatusChanged(...)` sites), `OrderStateMachineSteps.cs` (test construction), `AuthorisePaymentConsumer.cs` (4 `new PaymentAuthorised(...)`/`new PaymentFailed(...)` sites, including the redelivery-replay branch), and `AuthorisePaymentConsumerSteps.cs` (test construction). Missing even one site causes a compile error (all existing calls use named arguments, so a new required parameter is not silently defaulted) — which is actually a safety net, but the plan must budget for touching all 4 files, not just the 3 Contracts record files.
**Why it happens:** Positional/named-argument record construction scattered across saga transitions and a consumer's redelivery-replay branch is easy to undercount from the contract file alone.
**How to avoid:** Grep-verified list for the planner: `OrderStatusChanged` — 5 sites in `OrderStateMachine.cs` (Pending→Paid, Pending→Cancelled via PaymentFailed, Pending→Cancelled via timeout, Paid→Cancelled via FulfillmentFailed, plus the new Paid→Fulfilled site this phase adds) + test sites in `OrderStateMachineSteps.cs`. `AuthorisePayment` — 1 site in `OrderStateMachine.cs`'s `Initially()`. `PaymentFailed`/`PaymentAuthorised` — `AuthorisePaymentConsumer.cs` has FOUR publish sites total across its two branches (existing-row replay: Authorised/Failed cases; new-row: Failed/Authorised cases) — `msg.UserId` is directly available in all four since it's now on the redelivered `AuthorisePayment` command itself, so no new persisted column is needed on `ProcessedPayment`.
**Warning signs:** A plan that only lists the 3 Contracts `.cs` files as touched files for the D-03 contract change.

## Code Examples

### New `OrderShipped` contract (mirrors `FulfillmentFailed`'s field-naming convention: `CheckoutId`, not `OrderId`)
```csharp
// New file: src/building-blocks/Contracts/Fulfillment/Events/V1/OrderShipped.cs
using ECommerce.Contracts;

namespace ECommerce.Fulfillment.Events.V1;

public record OrderShipped(
    Guid MessageId,
    Guid CorrelationId,
    Guid CausationId,
    DateTimeOffset OccurredAt,
    Guid CheckoutId,
    string UserId,
    DateTimeOffset ShippedAt
) : IMessageEnvelope;
```

### `OrderStatusChanged` with the new `UserId` field (D-03)
```csharp
// Modified: src/building-blocks/Contracts/Orders/Events/V1/OrderStatusChanged.cs
public record OrderStatusChanged(
    Guid MessageId,
    Guid CorrelationId,
    Guid CausationId,
    DateTimeOffset OccurredAt,
    Guid OrderId,
    string UserId,               // NEW — D-03
    string PreviousStatus,
    string NewStatus,
    DateTimeOffset ChangedAt,
    string? FailureReason = null
) : IMessageEnvelope;
```

### `OrderStateMachine.cs` — new `Event<OrderShipped>` binding and the required `Ignore(...)` additions
```csharp
// Modified: src/services/orders/ECommerce.Orders.API/Features/Orders/OrderStateMachine.cs
// 1. New Event<T> registration (with the other Event<> declarations, ~line 32):
public Event<OrderShipped> OrderShippedEvent { get; private set; } = null!;
// ...
Event(() => OrderShippedEvent, x => x.CorrelateById(m => m.Message.CheckoutId));

// 2. Real binding inside During(Paid, ...) (alongside the existing FulfillmentFailedEvent binding):
When(OrderShippedEvent)
    .Then(ctx => ctx.Saga.UpdatedAt = ctx.Message.ShippedAt)
    .Publish(ctx => new OrderStatusChanged(
        MessageId: Guid.NewGuid(),
        CorrelationId: ctx.Saga.CorrelationId,
        CausationId: ctx.Message.MessageId,
        OccurredAt: DateTimeOffset.UtcNow,
        OrderId: ctx.Saga.CorrelationId,
        UserId: ctx.Saga.UserId,
        PreviousStatus: "Paid",
        NewStatus: "Fulfilled",
        ChangedAt: ctx.Message.ShippedAt,
        FailureReason: null))
    .TransitionTo(Fulfilled),

// 3. Ignore(OrderShippedEvent) MUST be added to every OTHER During() block's catch-all list —
//    During(Pending, ...), During(Cancelled, ...), During(Fulfilled, ...), During(Failed, ...) —
//    following the exact CR-01/CR-02/WR-02 discipline already documented at length in this file's
//    existing comments (a redelivered/late OrderShipped arriving after the saga has already left
//    Paid must be absorbed, not fault).
```

### Notifications consumer (mirrors `CatalogSeededConsumer` exactly)
```csharp
// New file: src/services/notifications/ECommerce.Notifications.API/Consumers/OrderShippedNotificationConsumer.cs
using ECommerce.Fulfillment.Events.V1;
using ECommerce.Notifications.API.Data;
using ECommerce.Notifications.API.Features.Notifications;
using MassTransit;

namespace ECommerce.Notifications.API.Consumers;

public class OrderShippedNotificationConsumer(NotificationsDbContext db, ILogger<OrderShippedNotificationConsumer> logger)
    : IConsumer<OrderShipped>
{
    public async Task Consume(ConsumeContext<OrderShipped> context)
    {
        var msg = context.Message;
        logger.LogInformation("OrderShipped received: CheckoutId={CheckoutId}, UserId={UserId}", msg.CheckoutId, msg.UserId);

        db.NotificationEntries.Add(new NotificationEntry
        {
            Id = Guid.NewGuid(),
            UserId = msg.UserId,
            OrderId = msg.CheckoutId,
            Message = "Your order has shipped.",
            EventType = "OrderShipped",
            OccurredAt = msg.ShippedAt
        });

        // Idempotency: MassTransit inbox deduplicates by transport MessageId — this consumer
        // body runs exactly once per unique delivery (same guarantee CatalogSeededConsumer relies on).
        await db.SaveChangesAsync(context.CancellationToken);
    }
}
```

### `GET /notifications` endpoint (mirrors `OrdersEndpoints`'s list-endpoint shape)
```csharp
// New file: src/services/notifications/ECommerce.Notifications.API/Features/Notifications/NotificationsEndpoints.cs
app.MapGet("/notifications", async (
    ClaimsPrincipal user,
    NotificationsDbContext db,
    CancellationToken ct) =>
{
    var userId = GetUserId(user);
    var items = await db.NotificationEntries
        .Where(n => n.UserId == userId)
        .OrderByDescending(n => n.OccurredAt)
        .ToListAsync(ct);

    return Results.Ok(items);
}).RequireAuthorization();
```

## Assumptions Log

| # | Claim | Section | Risk if Wrong |
|---|-------|---------|---------------|
| A1 | `ConsumeContext.SchedulePublish<T>()` (invoked from within a plain `IConsumer<T>.Consume`, not from an `IMessageScheduler` injected outside a consumer) participates in the same EF Core transactional outbox as `context.Publish`/`IPublishEndpoint.Publish` when `UseEntityFrameworkOutbox` is applied to the receive endpoint — i.e. the scheduled send is only actually handed to the scheduler after `SaveChangesAsync` commits. This was reasoned from MassTransit's documented outbox-filter-wraps-the-ConsumeContext design and cross-referenced against two WebSearch results, but not confirmed against an executed code sample in this repo (unlike the `Schedule()`/`Unschedule()` saga-activity pattern, which Phase 4's ADR-0009 spike DID execute and verify). | Architecture Patterns Pattern 1, Code Examples | If unverified and wrong, a redelivered `OrderStatusChanged{NewStatus="Paid"}` could theoretically schedule a duplicate `OrderShipped` before the first attempt's outbox row commits, producing a double-ship (though the Orders saga's own `Ignore(OrderShippedEvent)` catch-alls limit the blast radius to a harmless duplicate notification, not data corruption). **Recommend a short spike task, mirroring Phase 4's `spikes/04-asb-scheduling-spike/`, to confirm this empirically against the ASB emulator before or during Wave 1.** |
| A2 | `Fulfillment:ProcessingSeconds` as a `double`, default `45`, is the correct config shape/default — CONTEXT.md's specifics section suggested "~30-60s" as a working range without pinning an exact default. | User Constraints (Claude's Discretion), Standard Stack | Low — this is explicitly delegated to Claude's discretion per CONTEXT.md; any value in the 30-60s range satisfies D-01's "watchable live demo" intent, and the config is easily changed post-hoc. |

**If this table is empty:** N/A — see above.

## Open Questions

1. **Should ADR-0007's `[EntityName]` topic-per-context override be implemented as part of Phase 5, given this is the first phase where cross-service topic subscription actually matters?**
   - What we know: Zero `[EntityName]` overrides exist in the codebase today (verified via full-repo grep); MassTransit's default per-message-type topology still functions correctly for the new consumers this phase adds; ASB emulator's 50-entity limit is not at risk (8 published-message-types total after this phase).
   - What's unclear: Whether the user considers this in-scope hygiene for Phase 5 or explicit hardening-phase debt.
   - Recommendation: Do NOT implement `[EntityName]` in Phase 5 — it's not required by FUL-01/02/NOT-01/02, and doing so would touch every existing publisher (`OrderCreated`, `PaymentAuthorised`, etc.) well beyond this phase's blast radius. Flag it explicitly in the phase's final review/handoff notes as a candidate for Phase 6 (Hardening).

2. **Does the `NotificationEntry` read model need pagination for NOT-01, matching `GET /orders`'s `page`/`pageSize` pattern?**
   - What we know: `GET /orders` clamps and paginates (`PaginationHelper.Clamp`); REQUIREMENTS.md's NOT-01 text is silent on pagination; CONTEXT.md's D-05 describes a "mat-list" (implying a simple, likely unpaginated list for a demo).
   - What's unclear: Whether an unbounded `GET /notifications` is acceptable for a portfolio-scale demo dataset (a handful of orders per demo user) or whether the planner should add pagination defensively.
   - Recommendation: Skip pagination for v1 (unbounded list is fine at demo data volumes — a handful of notifications per user), consistent with D-05's "simple" framing; note it as a natural NOT-V2 addition if it ever becomes necessary.

## Environment Availability

| Dependency | Required By | Available | Version | Fallback |
|------------|------------|-----------|---------|----------|
| .NET SDK | All backend work | ✓ | 10.0.301 | — |
| Docker | Postgres/ASB emulator via Aspire, Testcontainers | ✓ | 29.5.2 | — |
| Node.js / npm | Angular frontend work (D-05 UI page) | ✓ | v24.16.0 / 11.13.0 | — |

**Missing dependencies with no fallback:** none.
**Missing dependencies with fallback:** none.

## Security Domain

### Applicable ASVS Categories (Level 1, per `.planning/config.json`)

| ASVS Category | Applies | Standard Control |
|---------------|---------|-------------------|
| V2 Authentication | yes | Existing OpenIddict-issued JWT, validated via `Microsoft.AspNetCore.Authentication.JwtBearer` — Notifications must add this wiring (currently has none), copying Orders' exact `AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(...)` block (`Authority = "http://identity"`, `RequireHttpsMetadata = false` for local dev, `ValidateAudience = false`). |
| V4 Access Control | yes | `GET /notifications` MUST scope its query by `UserId` extracted from the validated JWT (`ClaimTypes.NameIdentifier`/`"sub"`), never from a client-supplied parameter — identical pattern to `GET /orders`. This is the primary IDOR surface this phase introduces. |
| V5 Input Validation | yes | No client-supplied filters/IDs in `GET /notifications` v1 (no pagination per Open Question 2) — minimal input surface. If pagination is added later, clamp `page`/`pageSize` exactly like `PaginationHelper.Clamp` does for Orders. |
| V6 Cryptography | no | No new cryptographic operations this phase — JWT validation is delegated entirely to `Microsoft.AspNetCore.Authentication.JwtBearer`, never hand-rolled. |

### Known Threat Patterns for this stack

| Pattern | STRIDE | Standard Mitigation |
|---------|--------|----------------------|
| IDOR on `GET /notifications` (a caller viewing another user's inbox) | Information Disclosure | Query MUST be scoped `WHERE UserId == claimsUserId` server-side — never trust a client-supplied user identifier. Mirrors `GET /orders`'s existing, tested mitigation. |
| Cross-service UserId spoofing via message payload (a malicious/buggy service publishing an event with a forged `UserId`) | Spoofing | Out of scope for this phase's trust boundary — all publishers (Orders, Payments) are internal, first-party services within the same trust domain; `UserId` on events is derived server-side from the originating JWT (`OrdersEndpoints.GetUserId`) at the point the saga is first created, never from client input at the Fulfillment/Notifications layer. |
| Fulfillment's `OrderPaidConsumer` processing a redelivered message twice (double-ship / duplicate notification) | Tampering (data integrity) | MassTransit `InboxState` dedup by transport `MessageId` (Pattern 2, verified) — this is the NOT-02 forced-redelivery test's exact subject. |

## Sources

### Primary (HIGH confidence — direct repository inspection)
- `docs/adr/0005-saga-orchestration.md`, `0006-masstransit-outbox-inbox.md`, `0007-asb-topic-per-context.md`, `0009-checkout-saga-state-reconciliation.md` — full text read
- `src/services/orders/ECommerce.Orders.API/Features/Orders/OrderStateMachine.cs`, `CheckoutOptions.cs`, `Order.cs`, `OrdersEndpoints.cs` — full text read
- `src/services/orders/ECommerce.Orders.API/Program.cs` — full text read (dual-branch scheduler wiring)
- `src/services/payments/ECommerce.Payments.API/Program.cs`, `Features/Payments/AuthorisePaymentConsumer.cs`, `Data/PaymentsDbContext.cs` — full text read
- `src/services/notifications/ECommerce.Notifications.API/Program.cs`, `Consumers/CatalogSeededConsumer.cs`, `Data/NotificationsDbContext.cs`, `Data/DbInitializer.cs` — full text read
- `src/services/notifications/ECommerce.Notifications.Tests/Integration/CatalogSeededInboxDeduplicationSteps.cs` — full text read (forced-redelivery test pattern)
- `src/services/fulfillment/ECommerce.Fulfillment.API/Program.cs`, `.csproj` — full text read (confirmed bare stub)
- `src/building-blocks/Contracts/**/*.cs` — all existing contract records read (`OrderCreated`, `OrderStatusChanged`, `FulfillmentFailed`, `PaymentFailed`, `PaymentAuthorised`, `AuthorisePayment`, `IMessageEnvelope`)
- `src/ecommerce.AppHost/Program.cs` — full text read (confirmed missing `postgres` reference for Notifications)
- `src/services/gateway/ECommerce.Gateway.API/appsettings.json` — confirmed `/api/notifications/**` route already exists
- `src/frontend/ecommerce-app/src/app/features/checkout/checkout-page/checkout-page.component.ts`, `features/orders/order-detail/order-detail.component.ts`, `app.ts`, `app.html`, `app.routes.ts`, `shared/models/checkout.model.ts`, `shared/models/order.model.ts`, `core/services/orders.service.ts` — full text read
- `.planning/config.json` — confirmed `nyquist_validation: false`, `security_enforcement: true`, `security_asvs_level: 1`
- Direct tool probes: `dotnet --version` (10.0.301), `docker --version` (29.5.2), `node --version`/`npm --version`

### Secondary (MEDIUM confidence — WebSearch, cross-referenced against repo evidence)
- MassTransit `InboxState` deduplication-before-consumer-invocation behavior — WebSearch, corroborated by this repo's own `CatalogSeededInboxDeduplicationSteps.cs` test proving exactly-one `InboxState` row for a duplicate `MessageId`.
- `ConsumeContext.SchedulePublish<T>()` / `IMessageScheduler` API existence and `ScheduleSend` vs `SchedulePublish` distinction — WebSearch (MassTransit DeepWiki, official docs links). Transactional-outbox participation of `SchedulePublish` specifically is flagged as Assumption A1, not fully verified.

### Tertiary (LOW confidence)
- None used directly as a basis for a recommendation.

## Metadata

**Confidence breakdown:**
- Standard stack: HIGH — every package/version is transcribed from this repo's own already-building `.csproj` files, not external lookup.
- Architecture: HIGH for the consumer/scheduler/idempotency patterns (directly modeled on shipped code); MEDIUM for the specific `SchedulePublish`-participates-in-outbox claim (Assumption A1 — recommend a spike).
- Pitfalls: HIGH — all four pitfalls are drawn from direct, in-session inspection of the actual files involved (AppHost, order-detail.component.ts, ADR-0007 vs. grep results, contract call-site enumeration), not speculation.

**Research date:** 2026-08-14
**Valid until:** 2026-09-13 (30 days — stack is entirely intra-repo and stable; the one MEDIUM-confidence item (A1) should be resolved by a spike early in execution rather than by research staleness)
