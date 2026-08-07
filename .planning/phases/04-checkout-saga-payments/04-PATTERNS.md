# Phase 4: Checkout Saga & Payments - Pattern Map

**Mapped:** 2026-08-08
**Files analyzed:** 26 (new + modified)
**Analogs found:** 24 / 26

## File Classification

| New/Modified File | Role | Data Flow | Closest Analog | Match Quality |
|---|---|---|---|---|
| `src/services/orders/ECommerce.Orders.API/Features/Orders/OrderStateMachine.cs` (extend) | model (saga state machine) | event-driven | itself (Phase 3) | exact — extend in place |
| `src/services/orders/ECommerce.Orders.API/Features/Orders/Order.cs` (add `FailureReason`) | model (saga instance) | CRUD | itself (Phase 3) | exact |
| `src/services/orders/ECommerce.Orders.API/Features/Orders/OrderReadModel.cs` (add `FailureReason`) | model (read model) | CRUD | itself (Phase 3) | exact |
| `src/services/orders/ECommerce.Orders.API/Features/Orders/OrderReadModelProjector.cs` (extend) | service (event consumer) | event-driven | itself (Phase 3) | exact |
| `src/services/orders/ECommerce.Orders.API/Features/Orders/OrderDto.cs` (add `FailureReason`) | model (DTO) | transform | itself (Phase 3) | exact |
| `src/services/orders/ECommerce.Orders.API/Features/Orders/OrdersEndpoints.cs` (add `POST /orders/checkout`, retire test-trigger) | controller | request-response | itself, `test-create-from-cart` handler (Phase 3) | exact |
| `src/services/orders/ECommerce.Orders.API/Features/Orders/CheckoutOptions.cs` (new) | config | — | `PaginationHelper.cs`-style small static/options class | role-match |
| `src/services/orders/ECommerce.Orders.API/Data/OrdersDbContext.cs` (no schema change expected, migration only) | config (EF Core context) | CRUD | itself (Phase 3) | exact |
| `src/services/checkout/ECommerce.Checkout.API/Features/Checkout/CheckoutEndpoints.cs` (new) | controller | request-response | `OrdersEndpoints.cs` | exact (same vertical-slice shape, new service) |
| `src/services/checkout/ECommerce.Checkout.API/Features/Checkout/IOrdersClient.cs` (new) | service (HTTP client interface) | request-response | `ICartClient.cs` | exact |
| `src/services/checkout/ECommerce.Checkout.API/Features/Checkout/OrdersClient.cs` (new) | service (HTTP client impl) | request-response | `CartClient.cs` | exact |
| `src/services/checkout/ECommerce.Checkout.API/Features/Checkout/CheckoutStatusDto.cs` (new) | model (DTO + mapping table) | transform | `OrderDto.cs` (shape) + a new static map | role-match |
| `src/services/checkout/ECommerce.Checkout.API/Program.cs` (expand) | config (composition root) | — | `OrdersEndpoints`-wiring in `Orders.API/Program.cs` | exact (subset: no saga/outbox, producer-only MassTransit + JWT + `IOrdersClient`) |
| `src/services/checkout/ECommerce.Checkout.API/ECommerce.Checkout.API.csproj` (add packages) | config | — | `ECommerce.Orders.API.csproj` | exact (subset — MassTransit core+ASB only, no EF outbox) |
| `src/services/payments/ECommerce.Payments.API/Data/PaymentsDbContext.cs` (new) | model (EF DbContext) | CRUD | `CatalogDbContext.cs` | exact (first DB for a new service, no saga) |
| `src/services/payments/ECommerce.Payments.API/Data/DbInitializer.cs` (new) | service (hosted service) | batch | `Orders.API/Data/DbInitializer.cs` | exact |
| `src/services/payments/ECommerce.Payments.API/Features/Payments/ProcessedPayment.cs` (new) | model (idempotency entity) | CRUD | `OrderReadModel.cs` (EF entity shape) | role-match |
| `src/services/payments/ECommerce.Payments.API/Features/Payments/AuthorisePaymentConsumer.cs` (new) | service (MassTransit consumer) | event-driven | `OrderReadModelProjector.cs` / `CatalogSeededConsumer.cs` | exact |
| `src/services/payments/ECommerce.Payments.API/Features/Payments/RefundPaymentConsumer.cs` (new) | service (MassTransit consumer) | event-driven | `OrderReadModelProjector.cs` / `CatalogSeededConsumer.cs` | exact |
| `src/services/payments/ECommerce.Payments.API/Program.cs` (expand) | config (composition root) | — | `Orders.API/Program.cs` (MassTransit+EF outbox block) | exact |
| `src/services/payments/ECommerce.Payments.API/ECommerce.Payments.API.csproj` (add packages) | config | — | `ECommerce.Orders.API.csproj` | exact |
| `src/building-blocks/Contracts/Checkout/Commands/V1/StartCheckout.cs` (replaces Placeholder) | model (message contract) | event-driven | `Orders/Events/V1/OrderCreated.cs` | exact |
| `src/building-blocks/Contracts/Payments/Commands/V1/AuthorisePayment.cs`, `RefundPayment.cs` (replaces Placeholder) | model (message contract) | event-driven | `Orders/Events/V1/OrderCreated.cs` | exact |
| `src/building-blocks/Contracts/Payments/Events/V1/PaymentAuthorised.cs`, `PaymentFailed.cs`, `PaymentRefunded.cs` (replaces Placeholder) | model (message contract) | event-driven | `Orders/Events/V1/OrderStatusChanged.cs` | exact |
| `src/building-blocks/Contracts/Fulfillment/Events/V1/FulfillmentFailed.cs` (replaces Placeholder) | model (message contract) | event-driven | `Orders/Events/V1/OrderStatusChanged.cs` | exact |
| `src/ecommerce.AppHost/Program.cs` (reorder + add references) | config | — | itself | exact |
| `src/services/gateway/ECommerce.Gateway.API/appsettings.json` (add `checkout-route`) | config | request-response | existing `orders-route`/`cart-route` blocks | exact |
| `src/services/orders/ECommerce.Orders.Tests/Unit/OrderStateMachineTests.cs` + `Steps.cs` (extend) | test | event-driven | itself (Phase 3) | exact |
| `src/services/orders/ECommerce.Orders.Tests/Integration/*Timeout*Tests.cs` (new, D-05) | test | event-driven | `OrderStateMachineSteps.cs` + Quartz in-memory scheduler addition | role-match |
| `src/services/checkout/ECommerce.Checkout.Tests/Integration/CheckoutEndpointTests.cs` + `Steps.cs` (new) | test | request-response | `OrdersEndpointTests.cs` + `OrdersEndpointSteps.cs` | exact |
| `src/services/payments/ECommerce.Payments.Tests/Integration/AuthorisePaymentConsumerTests.cs` + `Steps.cs` (new) | test | event-driven | `CatalogSeededInboxDeduplicationSteps.cs` | exact |
| `src/building-blocks/Tests.Common/Builders/PaymentBuilder.cs`, `CheckoutBuilder.cs` (new) | utility (test builder) | — | `OrderBuilder.cs` | exact |
| `src/frontend/ecommerce-app/src/app/features/checkout/checkout-page/checkout-page.component.ts` (+.html) (new) | component | request-response + polling | `product-detail.component.ts` (polling-adjacent: `ngOnInit` + `ActivatedRoute` + signals) and `cart-page.component.ts` (loading/error signal pattern) | role-match (no existing polling component — composite of two analogs) |
| `src/frontend/ecommerce-app/src/app/features/orders/order-detail/order-detail.component.ts` (+.html) (new) | component | request-response | `product-detail.component.ts` | exact (route-param detail fetch, not-found handling) |
| `src/frontend/ecommerce-app/src/app/core/services/checkout.service.ts` (new) | service (HTTP wrapper) | request-response | `cart.service.ts` | exact |
| `src/frontend/ecommerce-app/src/app/core/services/orders.service.ts` (new) | service (HTTP wrapper) | request-response | `cart.service.ts` / `catalog.service.ts` | exact |
| `src/frontend/ecommerce-app/src/app/app.routes.ts` (add `/checkout`, `/orders/:id`) | config (routing) | — | itself | exact |

## Pattern Assignments

### `src/services/orders/ECommerce.Orders.API/Features/Orders/OrderStateMachine.cs` (saga, event-driven)

**Analog:** itself — `src/services/orders/ECommerce.Orders.API/Features/Orders/OrderStateMachine.cs` (full file read, 84 lines)

**State/Event declaration pattern** (lines 13-29):
```csharp
public class OrderStateMachine : MassTransitStateMachine<Order>
{
    public State Pending { get; private set; } = null!;
    public State Paid { get; private set; } = null!;
    // ...
    public Event<OrderCreated> OrderCreatedEvent { get; private set; } = null!;
    public Event<OrderStatusChanged> OrderStatusChangedEvent { get; private set; } = null!;

    public OrderStateMachine()
    {
        InstanceState(x => x.CurrentState);
        Event(() => OrderCreatedEvent, x => x.CorrelateById(m => m.Message.OrderId));
        Event(() => OrderStatusChangedEvent, x => x.CorrelateById(m => m.Message.OrderId));
```
Add `Started` and `Refunding` (if adopted) states + typed `PaymentAuthorisedEvent`/`PaymentFailedEvent`/`FulfillmentFailedEvent`/`CheckoutTimeoutExpired` in the exact same style, correlating on `CheckoutId`/`OrderId` (same GUID, per RESEARCH.md's `checkoutId == orderId` decision).

**Trailing catch-all combinator pattern** (lines 51-66, MUST preserve for every `During()` block, including the new ones per Pitfall 2):
```csharp
During(Pending,
    When(OrderStatusChangedEvent, ctx => ctx.Message.NewStatus == "Paid")
        .Then(ctx => ctx.Saga.UpdatedAt = ctx.Message.ChangedAt)
        .TransitionTo(Paid),
    When(OrderStatusChangedEvent, ctx => ctx.Message.NewStatus == "Cancelled")
        .Then(ctx => ctx.Saga.UpdatedAt = ctx.Message.ChangedAt)
        .TransitionTo(Cancelled),
    // ...
    When(OrderStatusChangedEvent)); // <-- trailing catch-all, no filter/transition — MUST be last
```
Apply the identical no-filter trailing `When(...)` after every typed event branch added in Phase 4 (`CheckoutTimeoutExpired` especially, per RESEARCH.md Pitfall 2 — a late timeout arriving after the saga already left `Pending` must be absorbed, not fault).

**`SetCompletedWhenFinalized()`** (line 81) — keep at the end of the constructor; terminal states (`Cancelled`, `Failed`) already rely on it.

---

### `src/services/orders/ECommerce.Orders.API/Features/Orders/Order.cs` (saga instance, CRUD)

**Analog:** itself, full file (19 lines)
```csharp
public class Order : SagaStateMachineInstance
{
    public Guid CorrelationId { get; set; }
    public string CurrentState { get; set; } = default!;
    public string UserId { get; set; } = default!;
    public List<OrderLineItem> LineItems { get; set; } = new();
    public decimal TotalAmount { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}
```
Add `public string? FailureReason { get; set; }` following the existing nullable-string convention seen elsewhere (`ImageUrl` in `Product.cs`). If `Schedule()`/`Unschedule()` is adopted (Pattern 2 in RESEARCH.md), also add a token id property, e.g. `public Guid? CheckoutTimeoutTokenId { get; set; }`.

---

### `src/services/orders/ECommerce.Orders.API/Features/Orders/OrderReadModel.cs` + `OrderReadModelProjector.cs` (read model, event-driven)

**Analog:** itself, both full files (18 + 65 lines)

**Read model field addition** — mirror `Status`'s declaration style (`OrderReadModel.cs` line 13): add `public string? FailureReason { get; set; }`.

**Projector idempotency-guard pattern** (`OrderReadModelProjector.cs` lines 18-25, `OrderCreated` handler):
```csharp
var alreadyProjected = await db.OrderReadModels
    .AnyAsync(o => o.Id == msg.OrderId, context.CancellationToken);
if (alreadyProjected)
    return;
```
**Status-update handler pattern** (lines 51-63, `OrderStatusChanged` handler) — extend to also copy `FailureReason` when present:
```csharp
public async Task Consume(ConsumeContext<OrderStatusChanged> context)
{
    var msg = context.Message;
    var readModel = await db.OrderReadModels
        .FirstOrDefaultAsync(o => o.Id == msg.OrderId, context.CancellationToken);
    if (readModel is null)
        return; // No-op — matching write-side saga instance not yet projected (or unknown).

    readModel.Status = msg.NewStatus;
    readModel.UpdatedAt = msg.ChangedAt;
    await db.SaveChangesAsync(context.CancellationToken);
}
```
Per RESEARCH.md Pattern 1, extend `OrderStatusChanged` with a `FailureReason` field and set `readModel.FailureReason = msg.FailureReason;` here — no new consumer method needed, this is the single place ORD-03's read model gets mutated.

---

### `src/services/orders/ECommerce.Orders.API/Features/Orders/OrdersEndpoints.cs` (controller, request-response)

**Analog:** itself — the `POST /orders/test-create-from-cart` handler being replaced (lines 60-106) is the direct precursor to the new `POST /orders/checkout` internal endpoint.

**IDOR-safe 404 pattern** (lines 40-58, `GET /orders/{id:guid}`) — reuse verbatim for any new internal status-lookup endpoint Checkout.API calls:
```csharp
var order = await db.OrderReadModels
    .Include(o => o.LineItems)
    .FirstOrDefaultAsync(o => o.Id == id, ct);

if (order is null || order.UserId != userId)
    return Results.NotFound(new { error = "Order not found." });
```

**Cart-fetch + publish + outbox-flush-before-side-effect ordering** (lines 63-106) — this is the exact sequence `POST /orders/checkout` must follow (RESEARCH.md Pitfall 3: outbox must flush before returning so `GET /checkout/{id}` never 404s on first poll):
```csharp
var cart = await cartClient.GetCartAsync(token, ct);
if (cart is null || cart.Items.Count == 0)
    return Results.BadRequest(new { error = "Cart is empty." });

var orderId = Guid.NewGuid();
// ...
await publishEndpoint.Publish(new OrderCreated(...), ct);

// Flush the transactional outbox BEFORE clearing the cart.
await db.SaveChangesAsync(ct);
await cartClient.ClearCartAsync(token, ct);

return Results.Accepted($"/orders/{orderId}", new { orderId, ... });
```
**Bearer-token extraction helper** (lines 114-120) — reuse `ExtractBearerToken` verbatim in Checkout.API's own endpoints file (small enough to duplicate per-service, matches existing "each service owns its HTTP client code" convention — no shared library currently exists for this).

**`GetUserId` claim-extraction helper** (lines 109-112) — reuse verbatim in Checkout.API.

---

### `src/services/checkout/ECommerce.Checkout.API/Features/Checkout/IOrdersClient.cs` + `OrdersClient.cs` (service, request-response)

**Analog:** `src/services/orders/ECommerce.Orders.API/Features/Orders/ICartClient.cs` + `CartClient.cs` (full files, 21 + 33 lines)

**Interface + snapshot-record pattern** (`ICartClient.cs`, full file):
```csharp
public interface ICartClient
{
    Task<CartSnapshot?> GetCartAsync(string bearerToken, CancellationToken ct);
    Task ClearCartAsync(string bearerToken, CancellationToken ct);
}

public record CartSnapshot(List<CartLineItemSnapshot> Items, decimal GrandTotal);
public record CartLineItemSnapshot(Guid ProductId, string ProductName, decimal UnitPrice, int Quantity);
```
Model `IOrdersClient` the same way: `Task<Guid?> StartCheckoutAsync(Guid checkoutId, string bearerToken, CancellationToken ct)` (returns null/`orderId` per RESEARCH.md's façade design) and `Task<OrderStatusSnapshot?> GetStatusAsync(Guid checkoutId, string bearerToken, CancellationToken ct)`.

**Typed HttpClient implementation pattern** (`CartClient.cs`, full file):
```csharp
public class CartClient(HttpClient http) : ICartClient
{
    public async Task<CartSnapshot?> GetCartAsync(string bearerToken, CancellationToken ct)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "/cart");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", bearerToken);

        using var response = await http.SendAsync(request, ct);
        if (!response.IsSuccessStatusCode)
            return null;

        return await response.Content.ReadFromJsonAsync<CartSnapshot>(cancellationToken: ct);
    }
    // ClearCartAsync uses HttpMethod.Delete + EnsureSuccessStatusCode()
}
```
`OrdersClient` follows this exactly — `POST /orders/checkout` and a new internal `GET /orders/{id}/status`-style endpoint (or reuse `GET /orders/{id}` directly), forwarding the bearer token per RESEARCH.md's "sync HTTP hops only" rule (Pitfall 6).

**DI registration** (`Orders.API/Program.cs` line 75):
```csharp
builder.Services.AddHttpClient<ICartClient, CartClient>(c => c.BaseAddress = new Uri("http://cart"));
```
Checkout.API registers `IOrdersClient`/`OrdersClient` identically, pointed at `http://orders`.

---

### `src/services/checkout/ECommerce.Checkout.API/Program.cs` (composition root)

**Analog:** `src/services/orders/ECommerce.Orders.API/Program.cs` (full file, 120 lines) — Checkout.API needs a strict subset: JWT auth + `IOrdersClient` typed HttpClient + a **producer-only** MassTransit registration (for `FulfillmentFailed` demo-trigger publish), no saga, no EF outbox, no DbContext.

**MassTransit "placeholder" sentinel pattern for tests** (lines 55-72) — reuse the same branch structure for Checkout.API's producer-only bus registration:
```csharp
var messagingConnectionString = builder.Configuration.GetConnectionString("messaging");
if (messagingConnectionString == "placeholder")
{
    x.UsingInMemory((context, cfg) => cfg.ConfigureEndpoints(context));
}
else
{
    x.UsingAzureServiceBus((context, cfg) =>
    {
        cfg.Host(messagingConnectionString);
        cfg.ConfigureEndpoints(context);
    });
}
```
**JWT bearer wiring** (lines 87-95) — copy verbatim (`options.Authority = "http://identity"`, `RequireHttpsMetadata = false`, `ValidateAudience = false`).

**Bootstrap logger / try-catch-finally host shape** (lines 1-16, 97-120 of `Orders.API/Program.cs`, or the current bare `Checkout.API/Program.cs` which already has this skeleton) — Checkout.API's existing stub already matches this; just add the DI registrations inside the existing `try` block, do not restructure.

---

### `src/services/payments/ECommerce.Payments.API/Data/PaymentsDbContext.cs` (new DbContext, CRUD)

**Analog:** `src/services/catalog/ECommerce.Catalog.API/Data/CatalogDbContext.cs` (full file, 32 lines) — closest match for "first database this service has ever had."

**Outbox/inbox entity registration + entity config pattern**:
```csharp
public class CatalogDbContext(DbContextOptions<CatalogDbContext> options) : DbContext(options)
{
    public DbSet<Product> Products => Set<Product>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.AddInboxStateEntity();
        modelBuilder.AddOutboxMessageEntity();
        modelBuilder.AddOutboxStateEntity();

        modelBuilder.Entity<Product>(b =>
        {
            b.HasKey(p => p.Id);
            b.Property(p => p.Name).IsRequired().HasMaxLength(200);
            b.Property(p => p.Price).HasColumnType("decimal(18,2)");
        });
    }
}
```
`PaymentsDbContext` follows this exactly with `DbSet<ProcessedPayment> ProcessedPayments`, plus (per RESEARCH.md Pattern 3) `b.HasIndex(p => p.CheckoutId).IsUnique();`.

---

### `src/services/payments/ECommerce.Payments.API/Data/DbInitializer.cs` (hosted service, batch)

**Analog:** `src/services/orders/ECommerce.Orders.API/Data/DbInitializer.cs` (full file, 16 lines — simplest possible shape, no seed data needed for Payments):
```csharp
public class DbInitializer(IServiceProvider serviceProvider) : IHostedService
{
    public async Task StartAsync(CancellationToken ct)
    {
        await using var scope = serviceProvider.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<OrdersDbContext>();
        await db.Database.MigrateAsync(ct);
    }

    public Task StopAsync(CancellationToken ct) => Task.CompletedTask;
}
```
Copy verbatim, swap `OrdersDbContext` for `PaymentsDbContext`. (Catalog's seeded variant is NOT the right analog here — Payments has no seed data.)

---

### `src/services/payments/ECommerce.Payments.API/Features/Payments/AuthorisePaymentConsumer.cs` / `RefundPaymentConsumer.cs` (consumer, event-driven)

**Analog:** `src/services/notifications/ECommerce.Notifications.API/Consumers/CatalogSeededConsumer.cs` (full file, 25 lines) for consumer shape; `src/services/orders/ECommerce.Orders.API/Features/Orders/OrderReadModelProjector.cs` (lines 18-25) for the idempotency-guard pattern.

**Consumer shape**:
```csharp
public class CatalogSeededConsumer(NotificationsDbContext db, ILogger<CatalogSeededConsumer> logger)
    : IConsumer<CatalogSeeded>
{
    public async Task Consume(ConsumeContext<CatalogSeeded> context)
    {
        var msg = context.Message;
        logger.LogInformation("...", msg.SeedId, msg.ItemCount);
        await db.SaveChangesAsync(context.CancellationToken);
    }
}
```
`AuthorisePaymentConsumer(PaymentsDbContext db, IPublishEndpoint publish)` combines this shape with the `ProcessedPayment` idempotency check from RESEARCH.md Pattern 3 (look up by `CheckoutId`, republish stored outcome if found; else decide via `.99` rule, insert, publish `PaymentAuthorised`/`PaymentFailed`). Same transactional-outbox call ordering as `OrdersEndpoints`: publish first, then `SaveChangesAsync` commits message + entity atomically.

---

### `src/building-blocks/Contracts/**/V1/*.cs` (message contracts, event-driven)

**Analog:** `src/building-blocks/Contracts/Orders/Events/V1/OrderCreated.cs` and `OrderStatusChanged.cs` (full files, 23 + 15 lines)

**Envelope pattern** (every new contract MUST follow this exact record shape and namespace convention):
```csharp
using ECommerce.Contracts;
namespace ECommerce.Orders.Events.V1;

public record OrderStatusChanged(
    Guid MessageId,
    Guid CorrelationId,
    Guid CausationId,
    DateTimeOffset OccurredAt,
    Guid OrderId,
    string PreviousStatus,
    string NewStatus,
    DateTimeOffset ChangedAt
) : IMessageEnvelope;
```
New contracts (`StartCheckout`, `AuthorisePayment`, `RefundPayment`, `PaymentAuthorised`, `PaymentFailed`, `PaymentRefunded`, `FulfillmentFailed`) all take the first four envelope fields verbatim, then domain-specific fields (`CheckoutId`, `Amount`, `Reason`, `FailedAt`/`AuthorisedAt`, etc. — see RESEARCH.md Code Examples for exact shapes already drafted for `AuthorisePayment`/`PaymentFailed`). Per RESEARCH.md Pitfall 6: **never** add a bearer-token/access-token field to any of these.

**File to delete/replace, not edit-in-place:** `Placeholder.cs` files (e.g. `Checkout/Commands/V1/Placeholder.cs`, full file shown below) — same replacement pattern already used for `Orders/Commands/V1` in a prior phase (its Placeholder.cs still exists and is untouched — Orders never got its own command contract; Checkout/Payments/Fulfillment DO need theirs replaced this phase):
```csharp
using ECommerce.Contracts;
namespace ECommerce.Checkout.Commands.V1;

public record CheckoutCommandsPlaceholder(
    Guid MessageId, Guid CorrelationId, Guid CausationId, DateTimeOffset OccurredAt
) : IMessageEnvelope;
```

---

### `src/ecommerce.AppHost/Program.cs` (config)

**Analog:** itself, full file (69 lines) — Pitfall 5 identifies the exact required edits.

**Current declaration order problem** (lines 33-51):
```csharp
builder.AddProject<Projects.ECommerce_Checkout_API>("checkout")
    .WithHttpEndpoint(port: 5003)
    .WithReference(postgres)
    .WithReference(serviceBus);

var orders = builder.AddProject<Projects.ECommerce_Orders_API>("orders")
    .WithHttpEndpoint(port: 5004)
    .WithReference(postgres)
    .WithReference(serviceBus)
    .WithReference(cart)
    .WithReference(identity);

builder.AddProject<Projects.ECommerce_Payments_API>("payments")
    .WithHttpEndpoint(port: 5006)
    .WithReference(serviceBus);
```
Required changes (per RESEARCH.md + Pitfall 5): move `checkout` declaration to *after* `orders`, add `.WithReference(orders)` to checkout; add `.WithReference(postgres).WaitFor(postgres)` to `payments` (mirrors `catalog`'s pattern at lines 19-23); add `.WithReference(checkout)` to `gateway` (lines 57-63 currently lack it).

---

### `src/services/gateway/ECommerce.Gateway.API/appsettings.json` (config)

**Analog:** itself — existing `orders-route`/`orders` cluster block (lines 31-35, 58-62)
```json
"orders-route": {
  "ClusterId": "orders",
  "Match": { "Path": "/api/orders/{**catch-all}" },
  "Transforms": [{ "PathRemovePrefix": "/api/orders" }]
}
```
and cluster:
```json
"orders": { "Destinations": { "orders": { "Address": "http://orders" } } }
```
Add an identical `checkout-route`/`checkout` pair. Per RESEARCH.md, Payments/Fulfillment stay internal-only — no new route entries for them.

---

### `src/services/orders/ECommerce.Orders.Tests/Unit/OrderStateMachineTests.cs` + `Steps.cs` (test, event-driven)

**Analog:** itself, both full files (40 + 94 lines)

**In-memory saga test harness setup** (`Steps.cs` lines 23-41):
```csharp
services.AddMassTransitTestHarness(x =>
{
    x.AddSagaStateMachine<OrderStateMachine, Order>().InMemoryRepository();
});
_provider = services.BuildServiceProvider(true);
Machine = _provider.GetRequiredService<OrderStateMachine>();
_harness = _provider.GetRequiredService<ITestHarness>();
await _harness.Start();
_sagaHarness = _harness.GetSagaStateMachineHarness<OrderStateMachine, Order>();
```
For the D-05 timeout test, add `cfg.UseInMemoryScheduler()` (`MassTransit.Quartz` 8.3.6, test-project-only per RESEARCH.md) inside `x.UsingInMemory(...)`.

**Given/When/Then naming + `harness.Consumed.Any<T>()` await pattern** (`Steps.cs` lines 51-93):
```csharp
public async Task Given_OrderCreatedPublished(Guid orderId)
{
    await _harness!.Bus.Publish(new OrderCreated(...));
    (await _harness!.Consumed.Any<OrderCreated>(x => x.Context.Message.OrderId == orderId))
        .Should().BeTrue("the saga should have consumed the OrderCreated event before proceeding");
}
```
New steps (`Given_PaymentAuthorisedPublished`, `When_CheckoutTimeoutExpiredFires`, etc.) follow this exact publish-then-await-consumed shape. Test class pairs the same way: `OrderStateMachineTests : IAsyncLifetime` delegating to `_steps` (lines 5-11 of `Tests.cs`).

---

### `src/services/orders/ECommerce.Orders.Tests/Integration/OrdersEndpointSteps.cs` (test, request-response)

**Analog:** itself, full file (304 lines) — closest analog for Checkout.API's integration test suite.

**WebApplicationFactory with WireMock stub + env-var eager-config pattern** (lines 27-75):
```csharp
internal sealed class OrdersWebApplicationFactory : WebApplicationFactory<Program>
{
    public OrdersWebApplicationFactory(string postgresConnectionString, string cartBaseAddress)
    {
        Environment.SetEnvironmentVariable("ConnectionStrings__postgres", postgresConnectionString);
        Environment.SetEnvironmentVariable("ConnectionStrings__messaging", "placeholder");
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((_, config) => { /* postgres + messaging=placeholder */ });
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<DbInitializer>();
            services.AddAuthentication(TestAuthHandler.SchemeName)
                .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(TestAuthHandler.SchemeName, _ => { });
            services.AddHttpClient<ICartClient, CartClient>(c => c.BaseAddress = new Uri(cartBaseAddress));
        });
    }
}
```
`CheckoutWebApplicationFactory` uses the identical shape, stubbing `IOrdersClient`/`OrdersClient` against a WireMock server the way `ICartClient` is stubbed here. Checkout.API has no DB, so skip the `RemoveAll<DbInitializer>()` line and any Postgres wiring.

**Polling-until-visible helper** (lines 232-249) — directly reusable pattern for asserting `GET /checkout/{id}` eventually reflects saga progress:
```csharp
public async Task<(HttpResponseMessage Response, OrderDto? Body)> When_PollingUntilOrderIsVisible(
    string userId, Guid orderId, int maxAttempts = 5, int delayMs = 250)
{
    for (var attempt = 0; attempt < maxAttempts; attempt++)
    {
        response = await When_GetOrderByIdIsCalled(userId, orderId);
        if (response.StatusCode == HttpStatusCode.OK) { ... return (response, body); }
        await Task.Delay(delayMs);
    }
    return (response, null);
}
```

---

### `src/services/notifications/ECommerce.Notifications.Tests/Integration/CatalogSeededInboxDeduplicationSteps.cs` (test, event-driven — Payments idempotency analog)

**Analog:** full file (104 lines) — best available analog for proving PAY-03's `CheckoutId`-based idempotency (adapt the *pattern*, not the transport-MessageId assertion itself, since PAY-03 needs a business-key check, not transport dedup).

**EF outbox test harness wiring** (lines 35-70):
```csharp
services.AddDbContext<NotificationsDbContext>(o => o.UseNpgsql(connectionString));
services.AddMassTransitTestHarness(x =>
{
    x.AddConsumer<CatalogSeededConsumer>();
    x.AddEntityFrameworkOutbox<NotificationsDbContext>(o => { o.UsePostgres(); });
    x.AddConfigureEndpointsCallback((context, name, cfg) =>
        cfg.UseEntityFrameworkOutbox<NotificationsDbContext>(context));
});
```
Payments' equivalent test publishes two `AuthorisePayment` commands with the **same `CheckoutId`** but different transport `MessageId`s, then asserts `db.ProcessedPayments.CountAsync(p => p.CheckoutId == checkoutId)` is `1` and that `PaymentAuthorised`/`PaymentFailed` was only published once — a business-key check layered on top of (not replacing) this transport-inbox pattern.

---

### `src/building-blocks/Tests.Common/Builders/OrderBuilder.cs` (utility, test builder)

**Analog:** full file (63 lines)
```csharp
public record OrderReadModelData(Guid Id, string UserId, string Status, decimal TotalAmount, DateTimeOffset CreatedAt);

public class OrderBuilder
{
    private Guid _id = Guid.NewGuid();
    private string _status = "Pending";
    // ... fluent With*() methods returning `this`
    public OrderReadModelData Build() => new(...);
}
```
`PaymentBuilder`/`CheckoutBuilder` follow the identical fluent-builder-plus-portable-record shape.

---

### `src/frontend/ecommerce-app/src/app/features/checkout/checkout-page/checkout-page.component.ts` (component, polling)

**No exact analog exists** — no polling component exists anywhere in the Angular app yet (RESEARCH.md Pitfall 4 confirms this). Compose from two partial analogs:

**Analog A — signal-based loading/error state:** `src/frontend/ecommerce-app/src/app/features/cart/cart-page/cart-page.component.ts` (lines 33-77):
```typescript
export class CartPageComponent implements OnInit {
  private cartService = inject(CartService);
  private router = inject(Router);

  cart = signal<Cart | null>(null);
  isLoading = signal<boolean>(false);
  hasError = signal<boolean>(false);

  ngOnInit(): void { this.loadCart(); }

  loadCart(): void {
    this.isLoading.set(true);
    this.hasError.set(false);
    this.cartService.getCart().subscribe({
      next: (cart) => { this.cart.set(cart); this.isLoading.set(false); },
      error: (err: HttpErrorResponse) => { this.isLoading.set(false); this.handleError(err); },
    });
  }
}
```
**Analog B — route-param-driven detail fetch + not-found handling:** `src/frontend/ecommerce-app/src/app/features/catalog/product-detail/product-detail.component.ts` (lines 26-69), particularly the `ActivatedRoute` snapshot-params pattern and `computed()` derived-label pattern (`stockLabel`) — reuse this shape for a `stepLabel`/`currentStepIndex` computed signal driving the stepper (D-06).

**New pattern needed (no analog): interval polling that stops on terminal state.** Implement with `setInterval`/`interval()` + `takeUntil`, following the existing `Subject` + `subscribe` idiom already used for debouncing in `CartPageComponent` (lines 47-56):
```typescript
private quantityUpdate$ = new Subject<QuantityUpdate>();
constructor() {
  this.quantityUpdate$.pipe(debounceTime(500)).subscribe(({ productId, quantity }) => { ... });
}
```
Model the poll loop the same RxJS-in-constructor way (e.g. `interval(1500).pipe(switchMap(() => this.checkoutService.getStatus(id)), takeWhile(isNonTerminal, true))`), then on terminal state call `this.router.navigate(['/orders', id])` (mirrors `ProductDetailComponent`'s `this.router.navigate(['/cart'])` on success, lines 79-82).

---

### `src/frontend/ecommerce-app/src/app/features/orders/order-detail/order-detail.component.ts` (component, request-response)

**Analog:** `src/frontend/ecommerce-app/src/app/features/catalog/product-detail/product-detail.component.ts` (full file, 90 lines) — exact structural match: route param → service call → signal → not-found/error handling.
```typescript
ngOnInit(): void {
  const id = this.route.snapshot.params['id'];
  if (!id) { this.notFound.set(true); return; }
  this.isLoading.set(true);
  this.catalogService.getProduct(id).subscribe({
    next: (product) => { this.product.set(product); this.isLoading.set(false); },
    error: (err) => { this.isLoading.set(false); this.notFound.set(true); },
  });
}
```
`OrderDetailComponent` follows this exactly, calling `ordersService.getOrder(id)`, and additionally rendering `order.failureReason` when `order.status` is a failure/cancelled terminal state (D-09) — no analog needed for that part, it's a plain `@if` in the template.

---

### `src/frontend/ecommerce-app/src/app/core/services/checkout.service.ts` / `orders.service.ts` (service, request-response)

**Analog:** `src/frontend/ecommerce-app/src/app/core/services/cart.service.ts` (full file, 26 lines):
```typescript
@Injectable({ providedIn: 'root' })
export class CartService {
  private http = inject(HttpClient);

  getCart(): Observable<Cart> {
    return this.http.get<Cart>('/api/cart');
  }
}
```
`CheckoutService.startCheckout()` → `POST /api/checkout`; `getStatus(id)` → `GET /api/checkout/{id}`. `OrdersService.getOrder(id)` → `GET /api/orders/{id}`. Same thin-wrapper, no error handling inside the service (handled at the component level, per `CartPageComponent`'s convention).

---

### `src/frontend/ecommerce-app/src/app/app.routes.ts` (routing config)

**Analog:** itself, full file (18 lines):
```typescript
export const routes: Routes = [
  { path: '', redirectTo: 'catalog', pathMatch: 'full' },
  { path: 'catalog', component: CatalogListComponent, title: 'Catalog — eCommerce' },
  { path: 'product/:id', component: ProductDetailComponent, title: 'Product — eCommerce' },
  { path: 'cart', component: CartPageComponent, title: 'Cart — eCommerce' },
  // ...
  { path: '**', redirectTo: 'catalog' },
];
```
Add `{ path: 'checkout', component: CheckoutPageComponent, title: 'Checkout — eCommerce' }` and `{ path: 'orders/:id', component: OrderDetailComponent, title: 'Order — eCommerce' }` before the wildcard route, following the exact `title` convention.

## Shared Patterns

### JWT Bearer Authentication (all new backend endpoints)
**Source:** `src/services/orders/ECommerce.Orders.API/Program.cs` lines 87-95
**Apply to:** `Checkout.API/Program.cs`, all `CheckoutEndpoints.cs` and `OrdersEndpoints.cs` handlers
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
Every Minimal API endpoint ends with `.RequireAuthorization()` (see `OrdersEndpoints.cs` lines 38, 58, 106).

### IDOR-Safe 404 (GET-by-id endpoints)
**Source:** `src/services/orders/ECommerce.Orders.API/Features/Orders/OrdersEndpoints.cs` lines 52-55
**Apply to:** `GET /checkout/{id}`, `POST /checkout/{id}/simulate-fulfillment-failure` ownership check
```csharp
if (order is null || order.UserId != userId)
    return Results.NotFound(new { error = "Order not found." });
```
Identical 404 regardless of not-found vs. not-owned — never a 403, never a distinguishing message.

### Transactional Outbox / Bus-Outbox-Flush-Before-Side-Effect
**Source:** `src/services/orders/ECommerce.Orders.API/Features/Orders/OrdersEndpoints.cs` lines 82-99; `Program.cs` lines 44-53
**Apply to:** `POST /orders/checkout` (Orders.API), `AuthorisePaymentConsumer`/`RefundPaymentConsumer` (Payments.API)
```csharp
await publishEndpoint.Publish(new OrderCreated(...), ct);
await db.SaveChangesAsync(ct); // flush outbox BEFORE any external side effect (e.g. clearing cart)
```
Payments needs its own `AddEntityFrameworkOutbox<PaymentsDbContext>(o => { o.UsePostgres(); o.UseBusOutbox(); })` registration (mirrors `Orders.API/Program.cs` lines 44-48) since it now both consumes (`AuthorisePayment`) and publishes (`PaymentAuthorised`/`PaymentFailed`).

### MassTransit "placeholder" Test-Transport Sentinel
**Source:** `src/services/orders/ECommerce.Orders.API/Program.cs` lines 55-72
**Apply to:** `Checkout.API/Program.cs`, `Payments.API/Program.cs`
```csharp
var messagingConnectionString = builder.Configuration.GetConnectionString("messaging");
if (messagingConnectionString == "placeholder")
    x.UsingInMemory((context, cfg) => cfg.ConfigureEndpoints(context));
else
    x.UsingAzureServiceBus((context, cfg) => { cfg.Host(messagingConnectionString); cfg.ConfigureEndpoints(context); });
```

### Bootstrap Logger / Host Try-Catch-Finally Shape
**Source:** every existing `Program.cs` (identical across all 8 services, e.g. `Checkout.API/Program.cs` full file)
**Apply to:** no structural change needed — Checkout/Payments/Fulfillment stubs already have this; only add DI registrations inside the existing `try` block.

### Mapperly DTO Mapper Registration
**Source:** `src/services/orders/ECommerce.Orders.API/Features/Orders/OrderMapper.cs` + `Program.cs` line 83
**Apply to:** Any new Checkout/Payments DTO mapping (`CheckoutStatusDto`, if a mapper is warranted rather than a plain `switch`)
```csharp
[Mapper]
public partial class OrderMapper
{
    public partial OrderDto ToDto(OrderReadModel order);
}
// Program.cs: builder.Services.AddSingleton<OrderMapper>(); // MUST register — Minimal API param-source inference needs this
```

### MassTransit Package Version Pin (ADR-0006)
**Source:** `src/services/orders/ECommerce.Orders.API/ECommerce.Orders.API.csproj` lines 20-23
**Apply to:** `Payments.API.csproj`, `Checkout.API.csproj`, `Orders.Tests.csproj` (for `MassTransit.Quartz`)
```xml
<!-- Pinned exactly per ADR-0006 — never floating. MassTransit 9.x resolves to a commercial license. -->
<PackageReference Include="MassTransit" Version="8.3.6" />
<PackageReference Include="MassTransit.Azure.ServiceBus.Core" Version="8.3.6" />
<PackageReference Include="MassTransit.EntityFrameworkCore" Version="8.3.6" />
```
`MassTransit.Quartz` must be added with `--version 8.3.6` explicitly (never `dotnet add` without `--version`) — see RESEARCH.md Package Legitimacy Audit.

## No Analog Found

| File | Role | Data Flow | Reason |
|------|------|-----------|--------|
| `CheckoutTimeout` `Schedule<Order, CheckoutTimeoutExpired>` declaration + ASB/Quartz scheduler wiring | model/config (saga scheduling) | event-driven (delayed) | No prior scheduled-message usage anywhere in the codebase — this is the first `Schedule()`/`Unschedule()` saga activity. Use RESEARCH.md Pattern 2's code examples directly (cited from MassTransit docs, cross-checked against this repo's existing `OrderStateMachine` conventions) rather than an internal analog. Flagged as highest technical risk — Wave 0 spike recommended before planning locks in ASB-native scheduling (see RESEARCH.md Common Pitfalls #1). |
| `/checkout` polling-until-terminal-state RxJS logic | component (Angular) | streaming/polling | No polling UI exists in the Angular app yet (confirmed via glob — only `auth`, `catalog`, `cart` feature folders exist). Compose from `CartPageComponent`'s debounce-`Subject` idiom + `ProductDetailComponent`'s route/service/signal shape (documented above under Pattern Assignments) rather than a single copy-source. |

## Metadata

**Analog search scope:** `src/services/**`, `src/building-blocks/**`, `src/ecommerce.AppHost/Program.cs`, `src/frontend/ecommerce-app/src/app/**` (entire repo — 8 services + Contracts + Tests.Common + Angular app)
**Files scanned:** ~45 read directly (full-file reads for all files ≤ 120 lines; no file in this repo exceeded 2,000 lines, so no offset/limit targeting was required)
**Pattern extraction date:** 2026-08-08
