# Phase 3: Cart & Orders Skeleton - Context

**Gathered:** 2026-07-21
**Status:** Ready for planning

<domain>
## Phase Boundary

A logged-in user can build a per-user Redis cart with price snapshots and view their orders history through a CQRS read model backed by an Orders state machine — **no real checkout yet** (checkout/saga is Phase 4). Phase 3 introduces a non-production test-trigger endpoint that converts a cart into a Pending order, purely to give the Orders skeleton something to display and exercise.

**Requirements in scope:** CART-01, CART-02, CART-03, CART-04, ORD-01, ORD-02, ORD-03, ORD-04, FE-02

</domain>

<decisions>
## Implementation Decisions

### Cart↔Order Boundary (CART-04, ORD-01..04)

- **D-01:** Phase 3 adds a non-production test-trigger endpoint (e.g. `POST /orders/test-create-from-cart`) that snapshots the caller's cart into a new `Order` in `Pending` status and clears the cart. This is explicitly a stand-in — Phase 4 replaces/wraps it with the real saga-driven `/checkout` endpoint. Name and mark it clearly (e.g. XML doc comment or route prefix) as temporary/demo-only so Phase 4 planning knows to retire it.
- **D-02:** CART-04 ("cart cleared after checkout completes") is satisfied via this test-trigger endpoint in Phase 3; the real trigger (`OrderPaid`/checkout-complete event) replaces it in Phase 4.

### Redis Cart Data Model (CART-01, CART-02, CART-03)

- **D-03:** Redis key structure: `cart:{userId}`. No TTL/expiry — the cart persists until explicitly cleared (by the test-trigger endpoint or explicit clear action). No sliding expiry in Phase 3.
- **D-04:** Price snapshot is captured server-side: when an item is added, Cart.API makes a synchronous HTTP call to the Catalog service (internal service-to-service URL, not through YARP) to fetch the current product price and name, then stores that snapshot in the cart line item. The client never supplies price.
- **D-05:** Cart line item stores at minimum: `ProductId`, `ProductName` (snapshot), `UnitPrice` (snapshot), `Quantity`. Cart summary derives line totals (`UnitPrice * Quantity`) and grand total from these snapshots — never re-fetches current catalog price for totals.

### Orders CQRS / State Machine (ORD-03, ORD-04)

- **D-06:** Write-side and read-side live in the same OrdersDB (Postgres), in separate tables: an `orders` write table (aggregate) and an `order_read_models` (or similar) projection table. No separate read database in Phase 3.
- **D-07:** The read-model projection is built asynchronously via MassTransit domain events, consistent with the outbox/inbox pattern established in Phase 2. The Orders write-side publishes domain events (e.g. `OrderCreated`, `OrderStatusChanged`) through the transactional outbox; a same-service (or dedicated) consumer updates the `order_read_models` table via idempotent inbox. `GET /orders` and `GET /orders/{id}` are served exclusively from the read-model table, never the write-side aggregate directly.
- **D-08:** Order state machine strictly enforces `Pending → Paid → Fulfilled / Cancelled / Failed` transitions on the write-side aggregate — invalid transitions must be rejected. In Phase 3 only `Pending` is reachable via the test-trigger endpoint; `Paid`/`Fulfilled`/`Cancelled`/`Failed` become reachable in Phases 4–5 but the state machine and its guard logic should be built correctly now.

### Angular /cart Page (FE-02)

- **D-09:** Quantity stepper interactions update a local signal immediately for a responsive UI, then debounce (~500ms) before sending a `PATCH` to the backend — avoids a network call per click while feeling instant.
- **D-10:** Layout and empty-cart state are Claude's discretion: use `mat-card` line items, `mat-icon-button` quantity steppers, a summary panel with grand total, and a simple empty-state message with a "Browse catalog" link — consistent with the Angular Material 20 setup from Phase 2.

### Claude's Discretion

- Exact Order entity fields beyond OrderId, UserId, LineItems, Status, Timestamps — Claude can choose a sensible set for the demo.
- Whether the read-model projection consumer lives in-process within Orders service or as a separate consumer class — no specific requirement, just must use MassTransit outbox/inbox per D-07.
- Cart clear semantics (full delete of Redis key vs. empty line-item list) — Claude decides, consistent with "cart persists until cleared."
- `/cart` page visual details beyond the Material component choices noted in D-10.

</decisions>

<canonical_refs>
## Canonical References

**Downstream agents MUST read these before planning or implementing.**

### Phase Requirements and Roadmap
- `.planning/ROADMAP.md` — Phase 3 goal, 5 success criteria (SC1–SC5), requirements mapping (CART-01/02/03/04, ORD-01/02/03/04, FE-02)
- `.planning/REQUIREMENTS.md` — Full requirement definitions; Phase 3 traceability section

### Project Structure and Constraints
- `.planning/PROJECT.md` — Repo directory layout, multi-solution mono-repo rationale
- `CLAUDE.md` — Full technology stack constraints: MassTransit 8.3.6 pin, EF Core + Npgsql, Redis for cart, Angular 20 conventions

### Architecture Decision Records (all in `docs/adr/`)
- `docs/adr/0003-database-per-service.md` — DB-per-service rule; applies to CartDB (Redis) / OrdersDB (Postgres) isolation
- `docs/adr/0006-masstransit-outbox-inbox.md` — MassTransit 8.3.6 pin, outbox/inbox pattern; **MUST read before wiring the Orders read-model projection**

### Prior Phase Context
- `.planning/phases/02-identity-catalog-gateway/02-CONTEXT.md` — Locked decisions from Phase 2: namespace conventions, test infrastructure pattern (`Tests.Common`, two-class test suites, `IClassFixture<PostgresFixture>`), YARP route-prefix convention (`/api/{service}/...`), MassTransit 8.3.6 pin discipline
- `.planning/phases/01-foundations/01-CONTEXT.md` — Project naming (`ECommerce.{ServiceName}.API`), .sln-per-service pattern, OTel/Serilog wiring baseline

### Existing Scaffold
- `src/services/cart/ECommerce.Cart.API/Program.cs` — Existing stub to expand with Redis-backed cart endpoints
- `src/services/orders/ECommerce.Orders.API/Program.cs` — Existing stub to expand with EF Core, MassTransit outbox/inbox, and Orders endpoints
- `src/building-blocks/Contracts/Cart/Commands/V1/Placeholder.cs`, `src/building-blocks/Contracts/Cart/Events/V1/Placeholder.cs` — replace with real Cart contracts
- `src/building-blocks/Contracts/Orders/Commands/V1/Placeholder.cs`, `src/building-blocks/Contracts/Orders/Events/V1/Placeholder.cs` — replace with `OrderCreated`, `OrderStatusChanged` (or similar) events
- `src/building-blocks/Tests.Common/ECommerce.Tests.Common.csproj` — reuse `PostgresFixture`, `WebApplicationFactory` base, builders for Orders integration tests

</canonical_refs>

<code_context>
## Existing Code Insights

### Reusable Assets
- `Tests.Common` (from Phase 2): `PostgresFixture` (Testcontainers), `WebApplicationFactory` base class, builder pattern (`ProductBuilder` etc.) — extend with `OrderBuilder`, `CartBuilder` as needed.
- Phase 2's two-class test pattern (`*Tests` / `*Steps`, `Given_/When_/Then_` naming) — reuse for Cart and Orders test suites.
- Catalog service's paginated `GET /products` endpoint — Cart.API's synchronous price-fetch call targets Catalog's existing `GET /products/{id}`.

### Established Patterns (from Phase 1 & 2 Context)
- `net10.0 + ImplicitUsings + Nullable enable` baseline for all projects.
- One `.sln` per service; Cart.sln and Orders.sln already exist and reference Contracts.csproj by relative path.
- MassTransit 8.3.6 explicitly pinned wherever referenced — Orders service adds it for the first time in Phase 3 (outbox for write-side, inbox for read-model projection consumer).
- Vertical-slice feature folder structure (e.g. `ECommerce.Orders.API.Orders`) established in Phase 2 — apply to Cart and Orders feature code.

### Integration Points
- `src/ecommerce.AppHost/Program.cs` — Cart and Orders services likely already wired as Aspire resources from Phase 1 stubs; confirm Redis resource is available to Cart service.
- Cart.API → Catalog.API: new internal synchronous HTTP call (service-to-service, not via YARP) for price snapshot on add-to-cart.
- YARP gateway `appsettings.json` — add `/api/cart/**` and `/api/orders/**` route prefixes forwarding to Cart and Orders services, consistent with Phase 2's `/api/catalog/`, `/api/identity/` convention.
- Angular: new `src/app/features/cart/` feature folder (cart page, line-item component), new `src/app/features/orders/` feature folder if `/orders` list/detail pages are needed for ORD-01/02 (note: FE-02 only covers `/cart`; order history page is FE-V2-01, deferred to V2 per STATE.md — confirm during planning whether a minimal orders view is still needed to demonstrate ORD-01/02, or whether API-only suffices for Phase 3).

</code_context>

<specifics>
## Specific Ideas

- Redis key: `cart:{userId}` — no TTL.
- Test-trigger endpoint name: `POST /orders/test-create-from-cart` (working name; Claude can finalize exact route).
- Cart line item shape: `{ ProductId, ProductName, UnitPrice, Quantity }` (all price/name fields are snapshots, never re-fetched for totals).
- Debounce interval for quantity PATCH: ~500ms.

</specifics>

<deferred>
## Deferred Ideas

- Real checkout-triggered cart clear (via `OrderPaid` or checkout-complete event) — Phase 4, replaces the Phase 3 test-trigger endpoint.
- Order history list page (FE-V2-01) — already flagged as deferred to V2 in STATE.md; Phase 3 planning should confirm whether ORD-01/02 need a minimal Angular view or are API-only for now.
- Sliding TTL / cart abandonment cleanup for Redis carts — not needed for portfolio demo scope.
- Separate read-model database for Orders CQRS — same-DB/separate-tables is sufficient at this scale; revisit only if a future phase demands it.

</deferred>

---

*Phase: 3-Cart & Orders Skeleton*
*Context gathered: 2026-07-21*
