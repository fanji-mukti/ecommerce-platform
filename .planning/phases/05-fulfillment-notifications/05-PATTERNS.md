# Phase 5: Fulfillment & Notifications - Pattern Map

**Mapped:** 2026-08-14
**Files analyzed:** 34
**Analogs found:** 32 / 34

## File Classification

| New/Modified File | Role | Data Flow | Closest Analog | Match Quality |
|--------------------|------|-----------|-----------------|----------------|
| `src/building-blocks/Contracts/Fulfillment/Events/V1/OrderShipped.cs` | model (contract) | event-driven | `src/building-blocks/Contracts/Fulfillment/Events/V1/FulfillmentFailed.cs` | exact |
| `src/building-blocks/Contracts/Orders/Events/V1/OrderStatusChanged.cs` (add `UserId`) | model (contract) | event-driven | itself (field addition) | exact |
| `src/building-blocks/Contracts/Payments/Events/V1/PaymentFailed.cs` (add `UserId`) | model (contract) | event-driven | itself (field addition) | exact |
| `src/building-blocks/Contracts/Payments/Commands/V1/AuthorisePayment.cs` (add `UserId`) | model (contract) | request-response | itself (field addition) | exact |
| `src/services/orders/ECommerce.Orders.API/Features/Orders/OrderStateMachine.cs` (modified) | service (saga) | event-driven | itself (extend in place) | exact |
| `src/services/orders/ECommerce.Orders.Tests/Unit/OrderStateMachineSteps.cs` (modified) | test | event-driven | itself (extend in place) | exact |
| `src/services/payments/ECommerce.Payments.API/Features/Payments/AuthorisePaymentConsumer.cs` (modified) | service (consumer) | event-driven | itself (extend in place) | exact |
| `src/services/payments/ECommerce.Payments.Tests/Integration/AuthorisePaymentConsumerSteps.cs` (modified) | test | event-driven | itself (extend in place) | exact |
| `src/services/fulfillment/ECommerce.Fulfillment.API/Data/FulfillmentDbContext.cs` | model (DbContext) | CRUD (outbox/inbox only) | `src/services/notifications/ECommerce.Notifications.API/Data/NotificationsDbContext.cs` | exact |
| `src/services/fulfillment/ECommerce.Fulfillment.API/Data/DbInitializer.cs` | service (hosted service) | batch | `src/services/notifications/ECommerce.Notifications.API/Data/DbInitializer.cs` | exact |
| `src/services/fulfillment/ECommerce.Fulfillment.API/Features/Fulfillment/FulfillmentOptions.cs` | config | — | `src/services/orders/ECommerce.Orders.API/Features/Orders/CheckoutOptions.cs` | exact |
| `src/services/fulfillment/ECommerce.Fulfillment.API/Features/Fulfillment/OrderPaidConsumer.cs` | service (consumer) | event-driven / streaming (delayed publish) | `src/services/notifications/ECommerce.Notifications.API/Consumers/CatalogSeededConsumer.cs` (consumer shape) + `OrderStateMachine.cs` (scheduler usage) | role-match |
| `src/services/fulfillment/ECommerce.Fulfillment.API/Program.cs` (modified, full build-out) | config (bootstrap) | — | `src/services/orders/ECommerce.Orders.API/Program.cs` (dual-branch scheduler) | exact |
| `src/services/fulfillment/ECommerce.Fulfillment.API/ECommerce.Fulfillment.API.csproj` (modified) | config | — | `src/services/notifications/ECommerce.Notifications.API/ECommerce.Notifications.API.csproj` | exact |
| `src/services/fulfillment/ECommerce.Fulfillment.API/Migrations/*_InitialFulfillmentSchema.cs` | migration | batch | Notifications' initial migration (inbox/outbox only, no business table) | role-match |
| `src/services/fulfillment/ECommerce.Fulfillment.Tests/Integration/OrderPaidConsumerTests.cs` / `Steps.cs` | test | event-driven | `CatalogSeededConsumerTests.cs` / `CatalogSeededConsumerSteps.cs` | exact |
| `src/services/fulfillment/ECommerce.Fulfillment.Tests/Integration/OrderPaidInboxDeduplicationTests.cs` / `Steps.cs` | test | event-driven | `CatalogSeededInboxDeduplicationTests.cs` / `CatalogSeededInboxDeduplicationSteps.cs` | exact |
| `src/services/notifications/ECommerce.Notifications.API/Data/NotificationsDbContext.cs` (modified) | model (DbContext) | CRUD | `src/services/payments/ECommerce.Payments.API/Data/PaymentsDbContext.cs` (business-entity + inbox/outbox shape) | exact |
| `src/services/notifications/ECommerce.Notifications.API/Features/Notifications/NotificationEntry.cs` | model (entity) | CRUD | `src/services/payments/ECommerce.Payments.API/Features/Payments/ProcessedPayment.cs` | exact |
| `src/services/notifications/ECommerce.Notifications.API/Features/Notifications/NotificationsEndpoints.cs` | controller (route) | request-response | `src/services/orders/ECommerce.Orders.API/Features/Orders/OrdersEndpoints.cs` (`GET /orders` list shape) | exact |
| `src/services/notifications/ECommerce.Notifications.API/Consumers/OrderPaidNotificationConsumer.cs` | service (consumer) | event-driven | `src/services/notifications/ECommerce.Notifications.API/Consumers/CatalogSeededConsumer.cs` | exact |
| `src/services/notifications/ECommerce.Notifications.API/Consumers/OrderShippedNotificationConsumer.cs` | service (consumer) | event-driven | `src/services/notifications/ECommerce.Notifications.API/Consumers/CatalogSeededConsumer.cs` | exact |
| `src/services/notifications/ECommerce.Notifications.API/Consumers/PaymentFailedNotificationConsumer.cs` | service (consumer) | event-driven | `src/services/notifications/ECommerce.Notifications.API/Consumers/CatalogSeededConsumer.cs` | exact |
| `src/services/notifications/ECommerce.Notifications.API/Program.cs` (modified) | config (bootstrap) | — | `src/services/orders/ECommerce.Orders.API/Program.cs` (JWT bearer block) + itself (existing MassTransit wiring) | exact |
| `src/services/notifications/ECommerce.Notifications.API/ECommerce.Notifications.API.csproj` (modified) | config | — | `src/services/orders/ECommerce.Orders.API/ECommerce.Orders.API.csproj` (JwtBearer package ref) | exact |
| `src/services/notifications/ECommerce.Notifications.API/Migrations/*_AddNotificationEntry.cs` | migration | batch | Payments' `ProcessedPayment` migration | role-match |
| `src/services/notifications/ECommerce.Notifications.Tests/Integration/OrderPaidNotificationConsumerTests.cs`+`Steps.cs` (and Shipped/PaymentFailed variants) | test | event-driven | `CatalogSeededConsumerTests.cs` / `CatalogSeededConsumerSteps.cs` | exact |
| `src/services/notifications/ECommerce.Notifications.Tests/Integration/*InboxDeduplication*` (x3, one per new consumer) | test | event-driven | `CatalogSeededInboxDeduplicationTests.cs` / `Steps.cs` | exact |
| `src/ecommerce.AppHost/Program.cs` (modified) | config (orchestration) | — | itself (existing `orders`/`payments` `.WithReference(postgres).WaitFor(postgres)` registrations) | exact |
| `src/frontend/ecommerce-app/src/app/shared/models/notification.model.ts` | model | request-response | `src/frontend/ecommerce-app/src/app/shared/models/order.model.ts` | exact |
| `src/frontend/ecommerce-app/src/app/core/services/notifications.service.ts` | service (HTTP client) | request-response | `src/frontend/ecommerce-app/src/app/core/services/orders.service.ts` | exact |
| `src/frontend/ecommerce-app/src/app/features/notifications/notifications-page/notifications-page.component.ts` | component | request-response | `src/frontend/ecommerce-app/src/app/features/catalog/catalog-list/catalog-list.component.ts` (list-fetch-render shape); no `mat-list` precedent exists in repo | partial |
| `src/frontend/ecommerce-app/src/app/features/orders/order-detail/order-detail.component.ts` (modified — add polling) | component | request-response (polling) | `src/frontend/ecommerce-app/src/app/features/checkout/checkout-page/checkout-page.component.ts` (`startPolling` method) | exact |
| `src/frontend/ecommerce-app/src/app/app.html` / `app.routes.ts` (modified) | component (shell/routing) | — | itself (existing nav links / route entries) | exact |

## Pattern Assignments

### `src/building-blocks/Contracts/Fulfillment/Events/V1/OrderShipped.cs` (model, event-driven)

**Analog:** `src/building-blocks/Contracts/Fulfillment/Events/V1/FulfillmentFailed.cs` (full file, 14 lines)

```csharp
// src/building-blocks/Contracts/Fulfillment/Events/V1/FulfillmentFailed.cs
using ECommerce.Contracts;

namespace ECommerce.Fulfillment.Events.V1;

public record FulfillmentFailed(
    Guid MessageId,
    Guid CorrelationId,
    Guid CausationId,
    DateTimeOffset OccurredAt,
    Guid CheckoutId,
    string Reason,
    DateTimeOffset FailedAt
) : IMessageEnvelope;
```

Copy this shape exactly for `OrderShipped` — same envelope fields (`MessageId`/`CorrelationId`/`CausationId`/`OccurredAt`), same `CheckoutId` naming convention (not `OrderId` — this is the Fulfillment context's own vocabulary), plus the new `UserId` (D-03) and `ShippedAt`:

```csharp
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

---

### `OrderStatusChanged.cs`, `PaymentFailed.cs`, `AuthorisePayment.cs` (model, contract field addition)

**Current state — `src/building-blocks/Contracts/Orders/Events/V1/OrderStatusChanged.cs`** (full file, 16 lines):
```csharp
public record OrderStatusChanged(
    Guid MessageId,
    Guid CorrelationId,
    Guid CausationId,
    DateTimeOffset OccurredAt,
    Guid OrderId,
    string PreviousStatus,
    string NewStatus,
    DateTimeOffset ChangedAt,
    string? FailureReason = null
) : IMessageEnvelope;
```
Insert `string UserId,` immediately after `Guid OrderId,` (positional record — insertion point matters; every call site uses named args so a compile error will surface any missed site, per RESEARCH.md Pitfall 4).

**Current state — `src/building-blocks/Contracts/Payments/Events/V1/PaymentFailed.cs`** (full file, 15 lines): insert `string UserId,` after `Guid CheckoutId,`.

**Current state — `src/building-blocks/Contracts/Payments/Commands/V1/AuthorisePayment.cs`** (full file, 14 lines): insert `string UserId,` after `Guid CheckoutId,`. This is required so `AuthorisePaymentConsumer` has a `UserId` to echo into `PaymentFailed` (Payments never queries Orders/Identity — D-03's "no synchronous lookup" constraint). `PaymentAuthorised` does **not** get a `UserId` field — Notifications never consumes it (see Anti-Patterns in RESEARCH.md); do not add it "for symmetry."

**Blast radius (grep-verified in this pass, matches RESEARCH.md Pitfall 4):**
| Contract | Call sites to update |
|---|---|
| `OrderStatusChanged` | `src/services/orders/ECommerce.Orders.API/Features/Orders/OrderStateMachine.cs` (5 `new OrderStatusChanged(...)` sites — 4 existing + 1 new for the `OrderShipped` binding) + `src/services/orders/ECommerce.Orders.Tests/Unit/OrderStateMachineSteps.cs` (test construction sites) |
| `AuthorisePayment` | `OrderStateMachine.cs`'s `Initially()` (1 site — supply `ctx.Saga.UserId`, already set by the preceding `.Then()`) + `OrderStateMachineSteps.cs` |
| `PaymentFailed` (+`PaymentAuthorised` unchanged) | `src/services/payments/ECommerce.Payments.API/Features/Payments/AuthorisePaymentConsumer.cs` (4 publish sites total: 2 `PaymentFailed` sites need `msg.UserId` from the now-enriched `AuthorisePayment`; 2 `PaymentAuthorised` sites are untouched) + `src/services/payments/ECommerce.Payments.Tests/Integration/AuthorisePaymentConsumerSteps.cs` |

---

### `src/services/orders/ECommerce.Orders.API/Features/Orders/OrderStateMachine.cs` (service/saga, event-driven, modified in place)

**Analog:** itself — extend the existing file following its own established idioms exactly (this is the single canonical saga in the codebase; ADR-0005 locks orchestration here).

**Event registration pattern** (lines 28-44, existing):
```csharp
public Event<FulfillmentFailed> FulfillmentFailedEvent { get; private set; } = null!;
// ...
Event(() => FulfillmentFailedEvent, x => x.CorrelateById(m => m.Message.CheckoutId));
```
Add a parallel `Event<OrderShipped> OrderShippedEvent` declared the same way, correlated by `CheckoutId` (matches `FulfillmentFailedEvent`'s correlation key, not `OrderId` — `OrderShipped` uses `CheckoutId` per the contract above).

**`During(Paid, ...)` real transition binding** — mirror the existing `FulfillmentFailedEvent` binding (lines 190-212) structurally, but simpler (no compensation publish, D-02 always-succeeds):
```csharp
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
```

**Mandatory defensive catch-alls** (the file's dominant, heavily-commented discipline — see lines 161-174, 225-231, 249-257, 266-274, 276-284): `Ignore(OrderShippedEvent)` MUST be added to **every other** `During()` block (`Pending`, `Cancelled`, `Fulfilled`, `Failed`) alongside the existing `Ignore(...)` lines for the other five event types. Follow the exact comment style already in the file (e.g. the CR-01/CR-02/WR-02 review-cycle annotations) explaining *why* — a redelivered/late `OrderShipped` arriving after the saga has left `Paid` must be absorbed, not fault.

**`Initially()` — add `UserId` to the existing `AuthorisePayment` publish** (lines 77-84):
```csharp
.Publish(ctx => new AuthorisePayment(
    MessageId: Guid.NewGuid(),
    CorrelationId: ctx.Saga.CorrelationId,
    CausationId: ctx.Message.MessageId,
    OccurredAt: DateTimeOffset.UtcNow,
    CheckoutId: ctx.Saga.CorrelationId,
    UserId: ctx.Saga.UserId,   // NEW — ctx.Saga.UserId already set by the preceding .Then()
    Amount: ctx.Message.TotalAmount,
    SimulatePaymentFailure: ctx.Message.SimulatePaymentFailure))
```

**Every existing `new OrderStatusChanged(...)` site** (4 of them, lines 116-125, 130-139, 145-154, 202-211) needs `UserId: ctx.Saga.UserId` inserted (all named-argument construction, so compile errors will catch any miss).

---

### `src/services/payments/ECommerce.Payments.API/Features/Payments/AuthorisePaymentConsumer.cs` (service/consumer, event-driven, modified in place)

**Analog:** itself (full file read, 93 lines). Add `msg.UserId` to the two `PaymentFailed` publish sites only:

```csharp
// Replay branch (line 33-37) — existing.Outcome does not carry UserId, so use msg.UserId
// (the redelivered AuthorisePayment command itself now carries it):
case "Failed":
    await publish.Publish(new PaymentFailed(
        Guid.NewGuid(), msg.CheckoutId, msg.MessageId, now,
        msg.CheckoutId, msg.UserId, existing.Amount, existing.FailureReason!, existing.ProcessedAt), context.CancellationToken);
    break;

// New-row branch (line 70-72):
await publish.Publish(new PaymentFailed(
    Guid.NewGuid(), msg.CheckoutId, msg.MessageId, now,
    msg.CheckoutId, msg.UserId, msg.Amount, DeclinedReason, now), context.CancellationToken);
```
Note per RESEARCH.md Pitfall 4: no new column on `ProcessedPayment` (`src/services/payments/ECommerce.Payments.API/Features/Payments/ProcessedPayment.cs`) is needed — `UserId` comes from the command, not stored state. The two `PaymentAuthorised` publish sites (lines 29-31, 85-87) are unchanged (no `UserId` field added to that contract).

---

### `src/services/fulfillment/ECommerce.Fulfillment.API/Program.cs` (config/bootstrap, full build-out from bare stub)

**Analog:** `src/services/orders/ECommerce.Orders.API/Program.cs` (full file, 136 lines) — this is the **only** file in the repo with the dual-branch (ASB-native prod / Quartz in-memory test) scheduler wiring that Fulfillment needs.

**Dual-branch scheduler pattern** (lines 36-89 of `Orders/Program.cs`, adapt `AddSagaStateMachine` → plain `AddConsumer<OrderPaidConsumer>`, no saga repository needed):
```csharp
builder.Services.AddMassTransit(x =>
{
    x.AddConsumer<OrderPaidConsumer>();

    x.AddEntityFrameworkOutbox<FulfillmentDbContext>(o =>
    {
        o.UsePostgres();
        o.UseBusOutbox(); // Fulfillment publishes (schedules) OrderShipped — bus outbox required
    });

    x.AddConfigureEndpointsCallback((context, name, cfg) =>
    {
        cfg.UseEntityFrameworkOutbox<FulfillmentDbContext>(context);
    });

    var messagingConnectionString = builder.Configuration.GetConnectionString("messaging");
    if (messagingConnectionString == "placeholder")
    {
        x.UsingInMemory((context, cfg) =>
        {
            cfg.UseInMemoryScheduler();
            cfg.ConfigureEndpoints(context);
        });
    }
    else
    {
        x.AddServiceBusMessageScheduler();
        x.UsingAzureServiceBus((context, cfg) =>
        {
            cfg.Host(messagingConnectionString);
            cfg.UseServiceBusMessageScheduler();
            cfg.ConfigureEndpoints(context);
        });
    }
});

builder.Services.Configure<FulfillmentOptions>(
    builder.Configuration.GetSection(FulfillmentOptions.SectionName));

builder.AddNpgsqlDbContext<FulfillmentDbContext>("postgres");
builder.Services.AddHostedService<DbInitializer>();
```
Fulfillment does **not** need the JWT bearer block (lines 103-111 of `Orders/Program.cs`) — it has no client-facing authenticated endpoints, only a health check. Do not copy that section.

---

### `src/services/fulfillment/ECommerce.Fulfillment.API/Data/FulfillmentDbContext.cs` (model/DbContext)

**Analog:** `src/services/notifications/ECommerce.Notifications.API/Data/NotificationsDbContext.cs` (full file, 18 lines) — exact match: outbox/inbox tables only, no business `DbSet`.

```csharp
using MassTransit;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Notifications.API.Data;

public class NotificationsDbContext(DbContextOptions<NotificationsDbContext> options)
    : DbContext(options)
{
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // MassTransit inbox/outbox tables required for idempotent inbox pattern
        modelBuilder.AddInboxStateEntity();
        modelBuilder.AddOutboxMessageEntity();
        modelBuilder.AddOutboxStateEntity();
    }
}
```
Copy verbatim, rename class/namespace to `FulfillmentDbContext` / `ECommerce.Fulfillment.API.Data`. No `DbSet<T>` needed — per RESEARCH.md, Fulfillment has no business state, only transactional outbox/inbox.

---

### `src/services/fulfillment/ECommerce.Fulfillment.API/Data/DbInitializer.cs` (service/hosted-service)

**Analog:** `src/services/notifications/ECommerce.Notifications.API/Data/DbInitializer.cs` (full file, 16 lines) — copy verbatim, swap DbContext type:

```csharp
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Notifications.API.Data;

public class DbInitializer(IServiceProvider serviceProvider) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await using var scope = serviceProvider.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<NotificationsDbContext>();
        await db.Database.MigrateAsync(cancellationToken);
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
```

---

### `src/services/fulfillment/ECommerce.Fulfillment.API/Features/Fulfillment/FulfillmentOptions.cs` (config)

**Analog:** `src/services/orders/ECommerce.Orders.API/Features/Orders/CheckoutOptions.cs` (full file, 15 lines):

```csharp
namespace ECommerce.Orders.API.Features.Orders;

public class CheckoutOptions
{
    public const string SectionName = "Checkout";

    public double TimeoutMinutes { get; set; } = 15;
}
```
Mirror exactly — `double` type (sub-unit test overrides, matches D-01/`ProcessingSeconds`), `const string SectionName`, XML doc explaining why `double` not `int`:
```csharp
public class FulfillmentOptions
{
    public const string SectionName = "Fulfillment";

    public double ProcessingSeconds { get; set; } = 45;
}
```

---

### `src/services/fulfillment/ECommerce.Fulfillment.API/Features/Fulfillment/OrderPaidConsumer.cs` (service/consumer, event-driven + delayed-publish)

**Analog (consumer body shape):** `src/services/notifications/ECommerce.Notifications.API/Consumers/CatalogSeededConsumer.cs` (full file, 25 lines) — primary-constructor DI, `ConsumeContext<T>`, log + `SaveChangesAsync`:
```csharp
public class CatalogSeededConsumer(NotificationsDbContext db, ILogger<CatalogSeededConsumer> logger)
    : IConsumer<CatalogSeeded>
{
    public async Task Consume(ConsumeContext<CatalogSeeded> context)
    {
        var msg = context.Message;
        logger.LogInformation("CatalogSeeded received: SeedId={SeedId}, ItemCount={ItemCount}", msg.SeedId, msg.ItemCount);
        await db.SaveChangesAsync(context.CancellationToken);
    }
}
```

**Analog (filter idiom):** `OrderStateMachine.cs` line 101 — `When(OrderStatusChangedEvent, ctx => ctx.Message.NewStatus == "Paid")`. `OrderPaidConsumer` filters the same way but as an early-return guard clause (stateless consumer, no saga `When()`):
```csharp
public class OrderPaidConsumer(IOptions<FulfillmentOptions> options) : IConsumer<OrderStatusChanged>
{
    public async Task Consume(ConsumeContext<OrderStatusChanged> context)
    {
        var msg = context.Message;
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
No `db.SaveChangesAsync()` call needed here — no `DbSet` write, only the scheduled publish (still flows through the EF Core outbox transactionally per `UseEntityFrameworkOutbox` on the receive endpoint).

---

### `src/services/notifications/ECommerce.Notifications.API/Data/NotificationsDbContext.cs` (modified — add business entity)

**Analog:** `src/services/payments/ECommerce.Payments.API/Data/PaymentsDbContext.cs` (full file, 31 lines) — the only DbContext in the repo combining outbox/inbox tables with a real business `DbSet`:
```csharp
public class PaymentsDbContext(DbContextOptions<PaymentsDbContext> options)
    : DbContext(options)
{
    public DbSet<ProcessedPayment> ProcessedPayments => Set<ProcessedPayment>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.AddInboxStateEntity();
        modelBuilder.AddOutboxMessageEntity();
        modelBuilder.AddOutboxStateEntity();

        modelBuilder.Entity<ProcessedPayment>(b =>
        {
            b.HasKey(p => p.CheckoutId);
            b.Property(p => p.Outcome).IsRequired().HasMaxLength(20);
            b.Property(p => p.Amount).HasColumnType("decimal(18,2)");
            b.Property(p => p.FailureReason).HasMaxLength(500);
            b.HasIndex(p => p.CheckoutId).IsUnique();
        });
    }
}
```
Apply the same shape to `NotificationsDbContext`: add `public DbSet<NotificationEntry> NotificationEntries => Set<NotificationEntry>();` and a `modelBuilder.Entity<NotificationEntry>(b => { ... })` block (PK `Id` Guid, `HasIndex(n => n.UserId)` for the `GET /notifications` query scope, `Message`/`EventType` length-limited strings).

---

### `src/services/notifications/ECommerce.Notifications.API/Features/Notifications/NotificationEntry.cs` (model/entity)

**Analog:** `src/services/payments/ECommerce.Payments.API/Features/Payments/ProcessedPayment.cs` (full file, 11 lines) — plain mutable POCO, no records for EF Core entities:
```csharp
public class ProcessedPayment
{
    public Guid CheckoutId { get; set; }
    public string Outcome { get; set; } = default!;
    public decimal Amount { get; set; }
    public string? FailureReason { get; set; }
    public DateTimeOffset ProcessedAt { get; set; }
}
```
Shape for `NotificationEntry` (fields per RESEARCH.md's recommended project structure): `Id` (Guid, PK), `UserId` (string), `OrderId` (Guid), `Message` (string), `EventType` (string), `OccurredAt` (DateTimeOffset).

---

### `src/services/notifications/ECommerce.Notifications.API/Features/Notifications/NotificationsEndpoints.cs` (controller/route, request-response)

**Analog:** `src/services/orders/ECommerce.Orders.API/Features/Orders/OrdersEndpoints.cs` — specifically the `GET /orders` list endpoint (lines 14-39) and the `GetUserId` helper (lines 112-115):
```csharp
app.MapGet("/orders", async (
    [Microsoft.AspNetCore.Mvc.FromQuery] int page,
    [Microsoft.AspNetCore.Mvc.FromQuery] int pageSize,
    ClaimsPrincipal user,
    OrdersDbContext db,
    OrderMapper mapper,
    CancellationToken ct) =>
{
    (page, pageSize) = PaginationHelper.Clamp(page, pageSize);
    var userId = GetUserId(user);

    var query = db.OrderReadModels.Where(o => o.UserId == userId);
    var total = await query.CountAsync(ct);
    var orders = await query
        .OrderByDescending(o => o.CreatedAt)
        .ThenBy(o => o.Id)
        .Skip((page - 1) * pageSize)
        .Take(pageSize)
        .ToListAsync(ct);

    var items = orders.Select(mapper.ToSummaryDto).ToList();
    return Results.Ok(new { Items = items, TotalCount = total, Page = page, PageSize = pageSize });
}).RequireAuthorization();

private static string GetUserId(ClaimsPrincipal user) =>
    user.FindFirstValue(ClaimTypes.NameIdentifier)
    ?? user.FindFirstValue("sub")
    ?? throw new InvalidOperationException("Authenticated request missing a user id claim.");
```
Per RESEARCH.md Open Question 2, skip pagination for `GET /notifications` v1 (unbounded list, D-05's "simple" framing) — omit `page`/`pageSize` params, just `.Where(n => n.UserId == userId).OrderByDescending(n => n.OccurredAt).ToListAsync(ct)`. Still copy `GetUserId` verbatim (IDOR-safe pattern is non-negotiable per ASVS V4 in RESEARCH.md's Security Domain).

---

### `OrderPaidNotificationConsumer.cs` / `OrderShippedNotificationConsumer.cs` / `PaymentFailedNotificationConsumer.cs` (service/consumer, event-driven)

**Analog:** `CatalogSeededConsumer.cs` (see full file above) — same primary-constructor DI shape (`NotificationsDbContext db, ILogger<T> logger`), same `Consume` structure. Each new consumer adds a `db.NotificationEntries.Add(new NotificationEntry {...})` before `SaveChangesAsync`:
```csharp
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

        await db.SaveChangesAsync(context.CancellationToken);
    }
}
```
`OrderPaidNotificationConsumer` subscribes to `IConsumer<OrderStatusChanged>` and applies the same `if (msg.NewStatus != "Paid") return;` filter guard as `OrderPaidConsumer` (Fulfillment) before writing. `PaymentFailedNotificationConsumer` subscribes to `IConsumer<PaymentFailed>`, no filter needed (every delivery is a real failure). Message copy per D-04: `"Your order has been paid."` / `"Your order has shipped."` / `"Payment failed for your order."`.

---

### `src/services/notifications/ECommerce.Notifications.API/Program.cs` (modified — add auth + 3 consumers)

**Analog (auth block):** `src/services/orders/ECommerce.Orders.API/Program.cs` lines 103-111:
```csharp
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.Authority = "http://identity";
        options.RequireHttpsMetadata = false;
        options.TokenValidationParameters.ValidateAudience = false;
    });
builder.Services.AddAuthorization();
```
And `app.UseAuthentication(); app.UseAuthorization();` before `app.MapOpenApi()` (lines 116-117).

**Analog (own existing MassTransit block, extend in place):** current `Notifications/Program.cs` lines 32-52 — add `x.AddConsumer<OrderPaidNotificationConsumer>(); x.AddConsumer<OrderShippedNotificationConsumer>(); x.AddConsumer<PaymentFailedNotificationConsumer>();` alongside the existing `x.AddConsumer<CatalogSeededConsumer>();`. No scheduler branch needed here (Notifications never schedules delayed messages) — keep the existing single `x.UsingAzureServiceBus(...)` call as-is (do not copy Orders'/Fulfillment's dual-branch scheduler wiring into Notifications).

Also call `NotificationsEndpoints.Map(app);` before `app.Run()`, mirroring `OrdersEndpoints.Map(app);` (Orders `Program.cs` line 121).

---

## Shared Patterns

### Idempotent stateless consumer (no custom dedup table)
**Source:** MassTransit's EF Core `InboxState`, wired via `AddInboxStateEntity()` + `x.AddConfigureEndpointsCallback((context, name, cfg) => cfg.UseEntityFrameworkOutbox<TContext>(context))` — already present in `NotificationsDbContext`/`Program.cs` and to be replicated in `FulfillmentDbContext`/`Program.cs`.
**Apply to:** `OrderPaidConsumer` (Fulfillment), `OrderPaidNotificationConsumer`, `OrderShippedNotificationConsumer`, `PaymentFailedNotificationConsumer` (Notifications). None of these need a business-key dedup table (unlike Payments' `ProcessedPayment`) — see RESEARCH.md Pattern 2.

### Dual-branch scheduler wiring (ASB-native prod / Quartz in-memory test)
**Source:** `src/services/orders/ECommerce.Orders.API/Program.cs` lines 58-88 (see excerpt above).
**Apply to:** `src/services/fulfillment/ECommerce.Fulfillment.API/Program.cs` only — Notifications and the rest of the stack do not schedule delayed messages.

### JWT-scoped, IDOR-safe query
**Source:** `OrdersEndpoints.GetUserId` + `.Where(o => o.UserId == userId)` (`src/services/orders/ECommerce.Orders.API/Features/Orders/OrdersEndpoints.cs` lines 24-26, 112-115).
**Apply to:** `NotificationsEndpoints.cs`'s `GET /notifications` — the only new authenticated endpoint this phase adds.

### `IHostedService` migration-on-startup
**Source:** `src/services/notifications/ECommerce.Notifications.API/Data/DbInitializer.cs` (full file, 16 lines, shown above).
**Apply to:** `src/services/fulfillment/ECommerce.Fulfillment.API/Data/DbInitializer.cs` (new, copy verbatim with type swap).

### Two-class test pattern (`*Tests`/`*Steps`, `Given_/When_/Then_`)
**Source:** `src/services/notifications/ECommerce.Notifications.Tests/Integration/CatalogSeededConsumerSteps.cs` (InMemory harness, consumer-invocation-count assertion) and `CatalogSeededInboxDeduplicationSteps.cs` (real Postgres via `PostgresFixture`, `InboxState` row-count assertion) — both full files shown above.
**Apply to:** every new consumer test in both `ECommerce.Fulfillment.Tests` (new project) and `ECommerce.Notifications.Tests`. Key detail to replicate: pin the transport-level `MessageId` explicitly on both `Bus.Publish` calls (`ctx.MessageId = messageId`) — MassTransit generates a fresh transport `MessageId` per `Publish` call by default, so this step is required for the dedup assertion to be meaningful, not optional boilerplate.

### Angular polling-until-terminal-status
**Source:** `checkout-page.component.ts`'s `startPolling` method (lines 115-138, full excerpt read):
```typescript
private startPolling(id: string): void {
    interval(1500)
      .pipe(
        switchMap(() => this.checkoutService.getStatus(id)),
        takeWhile((status) => {
          this.currentStatus.set(status.status);
          return !this.isTerminal(status.status);
        }, true),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe({
        next: (status) => { if (this.isTerminal(status.status)) { this.router.navigate(['/orders', id]); } },
        error: () => { this.hasError.set(true); },
      });
}
private isTerminal(status: CheckoutStatusValue): boolean {
    return TERMINAL_STATUSES.includes(status);
}
```
**Apply to:** `order-detail.component.ts` (net-new polling block — RESEARCH.md Pitfall 1 confirms this page currently has zero polling, only a single `ngOnInit` fetch). Adapt: poll `ordersService.getOrder(id)` instead of `checkoutService.getStatus(id)`; terminal set becomes `['Fulfilled', 'Cancelled', 'Failed']` (D-08); on terminal, stop polling in place (no navigation — the user is already on `/orders/:id`) rather than `router.navigate`. Note `checkout-page.component.ts`'s own `TERMINAL_STATUSES` (line 19) already includes `'Fulfilled'` and needs **no change** — only `order-detail.component.ts` needs the new block.

### Angular HTTP service (thin wrapper)
**Source:** `src/frontend/ecommerce-app/src/app/core/services/orders.service.ts` (full file, 13 lines):
```typescript
@Injectable({ providedIn: 'root' })
export class OrdersService {
  private http = inject(HttpClient);
  getOrder(id: string): Observable<OrderDetail> {
    return this.http.get<OrderDetail>(`/api/orders/${id}`);
  }
}
```
**Apply to:** new `notifications.service.ts` — `getNotifications(): Observable<NotificationEntry[]> { return this.http.get<NotificationEntry[]>('/api/notifications'); }`.

### Nav bar link (conditional on auth)
**Source:** `src/frontend/ecommerce-app/src/app/app.html` lines 5-11:
```html
@if (isAuthenticated().isAuthenticated) {
  <a mat-button routerLink="/cart" routerLinkActive="active-link">Cart</a>
  ...
}
```
**Apply to:** add `<a mat-button routerLink="/notifications" routerLinkActive="active-link">Notifications</a>` inside the same `@if` block, per D-06 (no badge).

## No Analog Found

| File | Role | Data Flow | Reason |
|------|------|-----------|--------|
| `src/frontend/ecommerce-app/src/app/features/notifications/notifications-page/notifications-page.component.ts` | component | request-response | No `mat-list` usage exists anywhere in the repo (`grep -rl "mat-list"` returned zero matches) — D-05 specifies "mat-list style" but there's no in-repo precedent for that exact Material module. Closest structural analog is `catalog-list.component.ts` (signals + `OnInit` fetch + loading/error state), but its rendering is card/grid-based, not list-based. Planner should treat the *component TypeScript structure* (signals, `ngOnInit`, `isLoading`/`hasError`) as copy-from-`catalog-list.component.ts`, and the *template* (`MatListModule`, `mat-list-item`) as new Material-doc-sourced markup, not an in-repo copy. |

## Metadata

**Analog search scope:** `src/services/{orders,payments,notifications,fulfillment}`, `src/building-blocks/Contracts`, `src/ecommerce.AppHost`, `src/frontend/ecommerce-app/src/app/{core,features,shared}`
**Files scanned:** ~28 read in full (all under 300 lines; no file exceeded the 2,000-line large-file threshold)
**Pattern extraction date:** 2026-08-14
