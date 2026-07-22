# Phase 3: Cart & Orders Skeleton - Pattern Map

**Mapped:** 2026-07-21
**Files analyzed:** 22
**Analogs found:** 20 / 22

## File Classification

| New/Modified File | Role | Data Flow | Closest Analog | Match Quality |
|---|---|---|---|---|
| `ECommerce.Cart.API.csproj` | config | n/a | `ECommerce.Catalog.API.csproj` (has MassTransit/EF Core refs) | role-match |
| `ECommerce.Cart.API/Program.cs` | config/bootstrap | request-response | `ECommerce.Catalog.API/Program.cs` | exact (structure), partial (Cart uses Redis not EF Core for its own store) |
| `ECommerce.Cart.API/Data/RedisCartStore.cs` (or similar) | service | CRUD (Redis) | none in codebase | no analog — see below |
| `ECommerce.Cart.API/Features/Cart/CartItem.cs` | model | CRUD | `Product.cs` (Catalog) | role-match |
| `ECommerce.Cart.API/Features/Cart/CartDto.cs` | model (DTO) | CRUD | `ProductDto.cs` (Catalog) | role-match |
| `ECommerce.Cart.API/Features/Cart/CartEndpoints.cs` | controller (Minimal API) | CRUD (request-response) | `ProductsEndpoints.cs` (Catalog) | role-match |
| `ECommerce.Cart.API/Features/Cart/CatalogPriceClient.cs` (or `ICatalogClient`) | service (HTTP client) | request-response (sync HTTP) | none — first synchronous service-to-service HTTP call in the codebase | no analog — see below |
| `ECommerce.Orders.API.csproj` | config | n/a | `ECommerce.Catalog.API.csproj` | exact |
| `ECommerce.Orders.API/Program.cs` | config/bootstrap | request-response + event-driven | `ECommerce.Catalog.API/Program.cs` (has the commented-out MassTransit outbox wiring to activate) | exact |
| `ECommerce.Orders.API/Data/OrdersDbContext.cs` | model/config (EF DbContext) | CRUD + outbox/inbox | `CatalogDbContext.cs` | exact |
| `ECommerce.Orders.API/Data/DbInitializer.cs` | utility (hosted service) | batch | `Catalog/Data/DbInitializer.cs` | exact |
| `ECommerce.Orders.API/Features/Orders/Order.cs` (write aggregate) | model | CRUD + state machine | `Product.cs` (shape only); no state-machine analog exists yet | partial |
| `ECommerce.Orders.API/Features/Orders/OrderStateMachine.cs` | service (state machine) | event-driven | none in codebase (first MassTransit saga/state machine) | no analog — see below |
| `ECommerce.Orders.API/Features/Orders/OrderReadModel.cs` | model (read projection) | CRUD (read) | `Product.cs` (shape only) | partial |
| `ECommerce.Orders.API/Features/Orders/OrderReadModelProjector.cs` (consumer) | service (MassTransit consumer) | event-driven | none in codebase | no analog — see below |
| `ECommerce.Orders.API/Features/Orders/OrdersEndpoints.cs` (GET list/detail + test-trigger POST) | controller (Minimal API) | CRUD (request-response) | `ProductsEndpoints.cs` (Catalog) + `RegistrationEndpoints.cs` (Identity, for POST + validation pattern) | role-match |
| `ECommerce.Orders.API/Features/Orders/OrderDto.cs` / `OrderMapper.cs` | model / utility | CRUD | `ProductDto.cs` / `ProductMapper.cs` (Mapperly) | exact |
| `Contracts/Orders/Events/V1/OrderCreated.cs`, `OrderStatusChanged.cs` | model (event contract) | event-driven | `Contracts/Catalog/Events/V1/CatalogSeeded.cs` | exact |
| `Contracts/Orders/Commands/V1/CreateOrderFromCart.cs` (or similar) | model (command contract) | event-driven | `Contracts/Orders/Events/V1/Placeholder.cs` (`OrdersServiceReady`, envelope shape) | exact |
| `Contracts/Cart/Events/V1/*.cs` (if any cart events needed) | model (event contract) | event-driven | `Contracts/Catalog/Events/V1/CatalogSeeded.cs` | exact |
| `ECommerce.Orders.Tests/Integration/OrdersEndpointTests.cs` + `OrdersEndpointSteps.cs` | test | request-response | `Catalog.Tests/Integration/ProductsEndpointTests.cs` + `ProductsEndpointSteps.cs` | exact |
| `ECommerce.Cart.Tests/Integration/CartEndpointTests.cs` + `CartEndpointSteps.cs` | test | request-response (Redis-backed) | `Catalog.Tests/Integration/ProductsEndpointTests.cs` + `ProductsEndpointSteps.cs` (structure only; Cart needs a Redis testcontainer fixture, not `PostgresFixture`) | role-match |
| `Tests.Common/Builders/OrderBuilder.cs`, `CartBuilder.cs` | test utility | n/a | `Tests.Common/Builders/ProductBuilder.cs` | exact |
| `gateway/appsettings.json` (add cart/orders routes) | config (route table) | request-response | existing `identity-route`/`catalog-route` entries in same file | exact |
| `frontend/app/core/services/cart.service.ts` | service (Angular HttpClient) | CRUD (request-response) | `core/services/catalog.service.ts` | exact |
| `frontend/app/shared/models/cart.model.ts` | model | CRUD | `shared/models/product.model.ts` | exact |
| `frontend/app/features/cart/cart-page/cart-page.component.ts` (+ .html) | component | CRUD (request-response, debounced PATCH) | `features/catalog/catalog-list/catalog-list.component.ts` (+ .html) | role-match |
| `frontend/app/features/cart/cart-line-item/cart-line-item.component.ts` | component | CRUD | `features/catalog/product-card/product-card.component.ts` | role-match |

## Pattern Assignments

### `ECommerce.Cart.API/Program.cs` (config/bootstrap, request-response)

**Analog:** `src/services/catalog/ECommerce.Catalog.API/Program.cs`

**Bootstrap logger + Serilog + OTel pattern** (lines 1-29, full file read):
```csharp
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.OpenTelemetry()
    .CreateLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);
    builder.Host.UseSerilog();
    builder.Services.AddOpenApi();
    builder.Services.AddHealthChecks();
    builder.Services.AddOpenTelemetry()
        .WithTracing(tracing => tracing
            .AddAspNetCoreInstrumentation()
            .AddOtlpExporter());
    // ... service-specific registrations here (Redis connection multiplexer for Cart) ...
    var app = builder.Build();
    app.UseHttpsRedirection();
    app.MapOpenApi();
    app.MapHealthChecks("/health");
    // MapEndpoints call here
    app.Run();
}
catch (Exception ex) { Log.Fatal(ex, "Application terminated unexpectedly"); return 1; }
finally { Log.CloseAndFlush(); }
return 0;
```
Cart.API currently has this exact skeleton with no DB/endpoint wiring (`src/services/cart/ECommerce.Cart.API/Program.cs`, full file, 46 lines) — expand it by adding `IConnectionMultiplexer`/`StackExchange.Redis` DI registration where Catalog registers `AddDbContext<CatalogDbContext>`, and calling a new `CartEndpoints.Map(app)` where Catalog calls `ProductsEndpoints.Map(app)`.

### `ECommerce.Orders.API/Program.cs` (config/bootstrap, request-response + event-driven)

**Analog:** `src/services/catalog/ECommerce.Catalog.API/Program.cs` lines 31-56 — this is the most important excerpt because it shows the exact MassTransit outbox wiring, **currently commented out**, that Orders must activate for real (per D-07 / ADR-0006):
```csharp
builder.Services.AddDbContext<CatalogDbContext>(options =>
{
    options.UseNpgsql(builder.Configuration.GetConnectionString("postgres"));
});

// To  how to use MassTransit with Azure Service Bus and Entity Framework Outbox in this context.
//builder.Services.AddMassTransit(x =>
//{
//    x.AddEntityFrameworkOutbox<CatalogDbContext>(o =>
//    {
//        o.UsePostgres();
//        o.UseBusOutbox(); // enables outbox drainer background service
//    });
//
//    x.UsingAzureServiceBus((context, cfg) =>
//    {
//        cfg.Host(builder.Configuration.GetConnectionString("messaging"));
//        cfg.ConfigureEndpoints(context);
//    });
//});

builder.Services.AddHostedService<DbInitializer>();
```
Orders is the **first service to actually enable** this block (uncommented), plus register the `OrderStateMachine` saga and the read-model projection consumer via `x.AddConsumer<OrderReadModelProjector>()` inside the same `AddMassTransit` call. Pin `MassTransit`, `MassTransit.Azure.ServiceBus.Core`, `MassTransit.EntityFrameworkCore` at **8.3.6** exactly, per ADR-0006 (`docs/adr/0006-masstransit-outbox-inbox.md` lines 39, 69 — "Pin 8.3.6 explicitly in every `.csproj` file — do not use floating versions").

### `ECommerce.Orders.API/Data/OrdersDbContext.cs` (model/config, CRUD + outbox/inbox)

**Analog:** `src/services/catalog/ECommerce.Catalog.API/Data/CatalogDbContext.cs` (full file, 31 lines)
```csharp
protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    base.OnModelCreating(modelBuilder);

    // MassTransit outbox/inbox tables — required for transactional outbox
    modelBuilder.AddInboxStateEntity();
    modelBuilder.AddOutboxMessageEntity();
    modelBuilder.AddOutboxStateEntity();

    modelBuilder.Entity<Product>(b =>
    {
        b.HasKey(p => p.Id);
        b.Property(p => p.Name).IsRequired().HasMaxLength(200);
        // ...
    });
}
```
`OrdersDbContext` copies this exactly: keep `AddInboxStateEntity/AddOutboxMessageEntity/AddOutboxStateEntity` calls, then add `DbSet<Order>` (write aggregate) and `DbSet<OrderReadModel>` (read projection) with their own `modelBuilder.Entity<T>(b => ...)` fluent config blocks, per D-06 (same DB, separate tables).

### `ECommerce.Orders.API/Data/DbInitializer.cs` (utility, batch)

**Analog:** `src/services/catalog/ECommerce.Catalog.API/Data/DbInitializer.cs` — read this file directly before writing Orders' version; it is registered via `builder.Services.AddHostedService<DbInitializer>()` in Program.cs (Catalog Program.cs line 56) and is explicitly removed in test hosts (see Test pattern below) because it can race with test seeding.

### `ECommerce.Orders.API/Features/Orders/OrdersEndpoints.cs` and `CartEndpoints.cs` (controller, CRUD/request-response)

**Analog for GET list/detail:** `src/services/catalog/ECommerce.Catalog.API/Features/Products/ProductsEndpoints.cs` (full file, 45 lines):
```csharp
public static class ProductsEndpoints
{
    public static void Map(WebApplication app)
    {
        app.MapGet("/products", async (
            [Microsoft.AspNetCore.Mvc.FromQuery] int page,
            [Microsoft.AspNetCore.Mvc.FromQuery] int pageSize,
            CatalogDbContext db, CancellationToken ct) => { /* paginate, project to DTO, Results.Ok(new { Items, TotalCount, Page, PageSize }) */ });

        app.MapGet("/products/{id:guid}", async (Guid id, CatalogDbContext db, CancellationToken ct) =>
        {
            var product = await db.Products.FindAsync([id], ct);
            return product is null
                ? Results.NotFound(new { error = "Product not found." })
                : Results.Ok(new ProductDto(...));
        });
    }
}
```
`OrdersEndpoints.Map(app)` follows this exact static-class-with-`Map(WebApplication app)` convention: `GET /orders` (paginated, read from `OrderReadModel` table per D-07) and `GET /orders/{id}` (404 with `{ error = "..." }` shape on miss).

**Analog for POST + validation:** `src/services/identity/ECommerce.Identity.API/Features/Registration/RegisterEndpoint.cs` (full file, 38 lines) and `RegisterValidator.cs` (full file, 12 lines):
```csharp
public static async Task<IResult> Register(
    RegisterRequest request, IValidator<RegisterRequest> validator,
    UserManager<IdentityUser> userManager, CancellationToken ct)
{
    var validation = await validator.ValidateAsync(request, ct);
    if (!validation.IsValid)
        return Results.ValidationProblem(validation.ToDictionary());
    // ... create + Results.Created(...) or Results.Conflict(...) on failure
}
```
```csharp
public class RegisterValidator : AbstractValidator<RegisterRequest>
{
    public RegisterValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.Password).NotEmpty().MinimumLength(8);
    }
}
```
Use this exact FluentValidation + `Results.ValidationProblem`/`Results.Created` pattern for `POST /orders/test-create-from-cart` (D-01) and for Cart's `POST /cart/items` / `PATCH /cart/items/{productId}` endpoints.

### `Contracts/Orders/Events/V1/OrderCreated.cs`, `OrderStatusChanged.cs` (event contract)

**Analog:** `src/building-blocks/Contracts/Catalog/Events/V1/CatalogSeeded.cs` (full file, 14 lines):
```csharp
using ECommerce.Contracts;

namespace ECommerce.Catalog.Events.V1;

public record CatalogSeeded(
    Guid MessageId,
    Guid CorrelationId,
    Guid CausationId,
    DateTimeOffset OccurredAt,
    Guid SeedId,
    int ItemCount,
    DateTimeOffset SeededAt
) : IMessageEnvelope;
```
Every event record: `using ECommerce.Contracts;`, namespace `ECommerce.{Bounded Context}.Events.V1`, implements `IMessageEnvelope` (`src/building-blocks/Contracts/IMessageEnvelope.cs`: `MessageId`, `CorrelationId`, `CausationId`, `OccurredAt`), envelope fields first, domain-specific fields after. Replace `src/building-blocks/Contracts/Orders/Events/V1/Placeholder.cs` (which currently only has `OrdersServiceReady`) with `OrderCreated(... Guid OrderId, Guid UserId, decimal TotalAmount, ...)` and `OrderStatusChanged(... Guid OrderId, string PreviousStatus, string NewStatus, ...)` following this exact shape.

### Test pattern: two-class Steps/Tests suite with builders

**Analog:** `src/services/catalog/ECommerce.Catalog.Tests/Integration/ProductsEndpointTests.cs` (full file, 54 lines) + `ProductsEndpointSteps.cs` (full file, 199 lines) + `Tests.Common/Builders/ProductBuilder.cs` (full file, 78 lines):
```csharp
[Collection("Integration")]
public class ProductsEndpointTests(PostgresFixture fixture) : IClassFixture<PostgresFixture>
{
    private readonly ProductsEndpointSteps _steps = new(fixture);

    [Fact]
    public async Task GetProducts_WhenCatalogHasProducts_ReturnsPagedList()
    {
        await _steps.Given_CatalogHasProducts(count: 15);
        var response = await _steps.When_GetProductsIsCalled(page: 1, pageSize: 12);
        await _steps.Then_ResponseIs200WithPagedResult(response, expectedItemCount: 12, expectedTotalCount: 15);
    }
}
```
```csharp
internal sealed class CatalogWebApplicationFactory(string connectionString) : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((_, config) => config.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["ConnectionStrings:postgres"] = connectionString,
            ["ConnectionStrings:messaging"] = "placeholder" // avoids MassTransit throwing on null host
        }));
        builder.ConfigureServices(services => services.RemoveAll<ECommerce.Catalog.API.Data.DbInitializer>());
    }
}
```
`OrdersEndpointTests`/`OrdersEndpointSteps` copy this structure exactly, reusing `PostgresFixture` (`Tests.Common/PostgresFixture.cs`, full file, 19 lines — Testcontainers Postgres 17-alpine) and adding an `OrdersWebApplicationFactory` with the same `ConnectionStrings:messaging = "placeholder"` trick plus `RemoveAll<DbInitializer>()`. `Given_/When_/Then_` naming is mandatory. Add `OrderBuilder` to `Tests.Common/Builders/` mirroring `ProductBuilder`'s fluent `With*()` + `Build()` shape returning a portable `OrderData` record.

Cart's test suite needs a **new fixture** (no analog exists) — likely `RedisFixture` using `Testcontainers.Redis`, structurally parallel to `PostgresFixture` (single `IAsyncLifetime` wrapping a Testcontainers builder, exposing a connection string/endpoint).

### Gateway routing (`gateway/appsettings.json`)

**Analog:** existing `catalog-route`/`identity-route` entries in `src/services/gateway/ECommerce.Gateway.API/appsettings.json` (full file, 46 lines):
```json
"catalog-route": {
  "ClusterId": "catalog",
  "Match": { "Path": "/api/catalog/{**catch-all}" },
  "Transforms": [{ "PathRemovePrefix": "/api/catalog" }]
},
...
"catalog": { "Destinations": { "catalog": { "Address": "http://catalog" } } }
```
Add `cart-route`/`orders-route` + `cart`/`orders` cluster entries following this exact `{**catch-all}` + `PathRemovePrefix` + `Destinations.Address: "http://{service}"` shape.

### Angular: service, model, component (Cart page, FE-02)

**Analog for service:** `src/frontend/ecommerce-app/src/app/core/services/catalog.service.ts` (full file, 22 lines):
```typescript
@Injectable({ providedIn: 'root' })
export class CatalogService {
  private http = inject(HttpClient);
  getProducts(page: number, pageSize: number, category?: string | null): Observable<PagedResult<Product>> {
    return this.http.get<PagedResult<Product>>(`/api/catalog/products?...`);
  }
}
```
`CartService` follows the same `inject(HttpClient)` + typed `Observable<T>` return + `/api/cart/...` URL convention; add methods for `getCart()`, `addItem()`, `updateQuantity()` (PATCH, debounced per D-09), `removeItem()`.

**Analog for model:** `src/frontend/ecommerce-app/src/app/shared/models/product.model.ts` (full file, 18 lines) — plain `interface` per DTO shape, no classes. `cart.model.ts` mirrors this: `CartLineItem { productId, productName, unitPrice, quantity }`, `Cart { userId, items: CartLineItem[], grandTotal }`.

**Analog for component:** `src/frontend/ecommerce-app/src/app/features/catalog/catalog-list/catalog-list.component.ts` (full file, 81 lines) — standalone component, `signal()`/`computed()` state, `inject()` for services, `isLoading`/`hasError` signal pair, imperative `load*()` method calling `.subscribe({ next, error })`. `CartPageComponent` follows this exact signal-based state pattern; add a `debounceTime`-based RxJS `Subject` (or a `setTimeout`-based debounce on a signal effect) for the ~500ms PATCH debounce per D-09 — no existing debounce analog in the codebase, so this is a novel addition on top of the copied component shape.

**Analog for template:** `catalog-list.component.html` (full file, 52 lines) — `@if`/`@for` control flow, Material components (`mat-progress-bar`, `mat-chip-listbox`), explicit empty-state and error-state blocks. `cart-page.component.html` mirrors the empty-state block structure for the "cart is empty, browse catalog" case (D-10).

## Shared Patterns

### Serilog + OpenTelemetry bootstrap
**Source:** `src/services/catalog/ECommerce.Catalog.API/Program.cs` lines 1-29 (identical in `src/services/orders/ECommerce.Orders.API/Program.cs` and `src/services/cart/ECommerce.Cart.API/Program.cs` stubs today)
**Apply to:** Both Cart.API and Orders.API Program.cs — do not alter the bootstrap logger, `UseSerilog()`, `AddOpenTelemetry().WithTracing(...)` block; only add service-specific registrations after it.

### MassTransit 8.3.6 outbox/inbox wiring
**Source:** `src/services/catalog/ECommerce.Catalog.API/Program.cs` lines 38-52 (commented template) + `docs/adr/0006-masstransit-outbox-inbox.md`
**Apply to:** Orders.API only in Phase 3 (first service to activate it for real). Cart.API does not need MassTransit in Phase 3 (Redis-only, no outbox) unless CONTEXT.md's discretion section changes that.
**Critical constraint:** pin `MassTransit`, `MassTransit.Azure.ServiceBus.Core`, `MassTransit.EntityFrameworkCore` to exactly `8.3.6` in the `.csproj` — never a floating/default version (ADR-0006, "CRITICAL for Phase 2" note, same rule applies here).

### Minimal API static endpoint class + `Map(WebApplication app)` convention
**Source:** `src/services/catalog/ECommerce.Catalog.API/Features/Products/ProductsEndpoints.cs`
**Apply to:** `CartEndpoints.cs`, `OrdersEndpoints.cs` — one static class per feature, single `Map(WebApplication app)` entry point called from Program.cs.

### FluentValidation + `Results.ValidationProblem` for POST/PATCH
**Source:** `src/services/identity/ECommerce.Identity.API/Features/Registration/RegisterEndpoint.cs` + `RegisterValidator.cs`
**Apply to:** Cart's add/update endpoints, Orders' test-trigger endpoint.

### Mapperly `[Mapper]` partial class for entity-to-DTO mapping
**Source:** `src/services/catalog/ECommerce.Catalog.API/Features/Products/ProductMapper.cs` (full file, 10 lines)
**Apply to:** `OrderMapper` (Order/OrderReadModel → OrderDto), any Cart DTO mapping — do not use AutoMapper (CLAUDE.md explicitly forbids it).

### Event contract shape (`IMessageEnvelope` + record)
**Source:** `src/building-blocks/Contracts/Catalog/Events/V1/CatalogSeeded.cs`, `src/building-blocks/Contracts/IMessageEnvelope.cs`
**Apply to:** All new Orders/Cart event and command contracts replacing the `Placeholder.cs` files.

### Two-class integration test suite (`*Tests` + `*Steps`) with Given/When/Then naming
**Source:** `src/services/catalog/ECommerce.Catalog.Tests/Integration/ProductsEndpointTests.cs` + `ProductsEndpointSteps.cs`, `Tests.Common/PostgresFixture.cs`, `Tests.Common/Builders/ProductBuilder.cs`
**Apply to:** All new Cart and Orders integration test suites; extend `Tests.Common/Builders/` with `OrderBuilder`/`CartBuilder`.

### YARP route-prefix convention
**Source:** `src/services/gateway/ECommerce.Gateway.API/appsettings.json` (`catalog-route`, `identity-route`)
**Apply to:** New `cart-route`, `orders-route` entries.

### Angular signal-based standalone component + typed HttpClient service
**Source:** `src/frontend/ecommerce-app/src/app/features/catalog/catalog-list/catalog-list.component.ts`, `src/frontend/ecommerce-app/src/app/core/services/catalog.service.ts`, `src/frontend/ecommerce-app/src/app/shared/models/product.model.ts`
**Apply to:** New `cart.service.ts`, `cart.model.ts`, `cart-page.component.ts`, `cart-line-item.component.ts`.

## No Analog Found

Files/patterns with no close match in the codebase — planner should design these from scratch using CLAUDE.md stack guidance and the ADRs, not from a copied analog:

| File/Pattern | Role | Data Flow | Reason |
|---|---|---|---|
| `ECommerce.Cart.API/Data/RedisCartStore.cs` | service | CRUD (Redis) | No existing service uses Redis yet; this is the first Redis-backed persistence in the codebase. Use `IConnectionMultiplexer` (StackExchange.Redis) directly, key `cart:{userId}` per D-03, JSON-serialize the cart via `System.Text.Json` (per CLAUDE.md — no Newtonsoft.Json). |
| `CatalogPriceClient.cs` (Cart → Catalog sync HTTP call) | service (HTTP client) | request-response (sync) | First internal service-to-service synchronous HTTP call (bypassing YARP) in the codebase. Register a named/typed `HttpClient` pointed at the internal Catalog service address (Aspire service discovery, e.g. `http://catalog`) via `builder.Services.AddHttpClient<ICatalogPriceClient, CatalogPriceClient>(...)`; no existing typed-client registration to copy — follow standard ASP.NET Core `IHttpClientFactory` typed-client conventions. |
| `OrderStateMachine.cs` (MassTransit Automatonymous state machine) | service (saga) | event-driven | First MassTransit saga/state machine in the codebase (ADR-0005 references it but no code exists yet). Must be built from MassTransit 8.3.6 documentation patterns: `MassTransitStateMachine<OrderState>`, `Event<T>` declarations, `Initially()`/`During()` blocks enforcing `Pending → Paid → Fulfilled/Cancelled/Failed` per D-08. Persist via `MassTransit.EntityFrameworkCore` saga repository against `OrdersDbContext`. |
| `OrderReadModelProjector.cs` (MassTransit `IConsumer<T>`) | service (consumer) | event-driven | First MassTransit consumer (as opposed to saga) in the codebase; no existing `IConsumer<T>` implementation to copy. Follow standard MassTransit consumer shape (`public class OrderReadModelProjector : IConsumer<OrderCreated> { public Task Consume(ConsumeContext<OrderCreated> context) { ... } }`) with idempotent-inbox guarantee already provided by `AddInboxStateEntity()` on the shared `OrdersDbContext`. |

## Metadata

**Analog search scope:** `src/services/catalog/`, `src/services/identity/`, `src/services/gateway/`, `src/building-blocks/Contracts/`, `src/building-blocks/Tests.Common/`, `src/frontend/ecommerce-app/src/app/features/catalog/`, `src/frontend/ecommerce-app/src/app/core/services/`, `docs/adr/0006-masstransit-outbox-inbox.md`
**Files scanned:** ~35 (Catalog, Identity, Gateway, Contracts, Tests.Common, Angular catalog feature, Cart/Orders stubs)
**Pattern extraction date:** 2026-07-21
