# Phase 5: Fulfillment & Notifications - Context

**Gathered:** 2026-08-14
**Status:** Ready for planning

<domain>
## Phase Boundary

The saga reaches a fully-shipped terminal state and the user sees the entire lifecycle reflected in an in-app notification inbox. Fulfillment consumes the "order paid" signal, runs a timer-based shipping simulation, and publishes `OrderShipped` — driving the Orders saga from `Paid` to `Fulfilled`/completed. Notifications consumes saga lifecycle events (`OrderPaid`-equivalent, `OrderShipped`, `PaymentFailed`) and persists an idempotent, per-user inbox exposed via `GET /notifications`.

**Requirements in scope:** FUL-01, FUL-02, NOT-01, NOT-02

**Explicitly NOT in scope:** real payment provider, mark-as-read (NOT-V2-01), real email/SMS delivery (NOT-V2-02), a deterministic natural failure trigger for Fulfillment (decided below — stays manual/demo-only via Phase 4's endpoint).

</domain>

<decisions>
## Implementation Decisions

### Fulfillment Timer & Failure Trigger (FUL-01, FUL-02)

- **D-01:** The simulated shipping delay is **configurable**, not hardcoded — following Phase 4's `Checkout:TimeoutMinutes` pattern (e.g. `Fulfillment:ProcessingSeconds` or similar), defaulting to roughly 30s–1min for a watchable live demo, overridable to a short interval for tests. Reuse the same MassTransit.Quartz scheduling mechanism already wired for the Orders saga's `CheckoutTimeout`.
- **D-02:** The real Fulfillment service **always succeeds** — FUL-01/02 as written only describe the happy path (consume the paid signal → wait → publish `OrderShipped`). No deterministic natural-failure trigger (no PAY-02-style `.99` equivalent) is added in this phase. Phase 4's `POST /checkout/{id}/simulate-fulfillment-failure` demo endpoint remains the only way to exercise the `FulfillmentFailed` compensation branch — it is NOT retired.

### Notification Content & User Scoping (NOT-01, NOT-02)

- **D-03:** The events Notifications consumes (`OrderStatusChanged`/whatever "OrderPaid" signal is chosen during planning, the new `OrderShipped`, and `PaymentFailed`) each gain a **`UserId` field**, populated by their publishers. This keeps Notifications fully event-driven — no synchronous service-to-service lookup calls, preserving the project's "no direct coupling" premise (see PROJECT.md Core Value). Publishers (Orders, Payments, Fulfillment) already have or can resolve `UserId` at publish time.
- **D-04:** Each inbox entry shows a **short templated human-readable message plus an order reference** (OrderId/CheckoutId) so the frontend can link to `/orders/:id` — e.g. "Your order has been paid", "Your order has shipped", "Payment failed for your order." Not a raw event-type/timestamp dump.

### Notifications UI Surface (NOT-01)

- **D-05:** Despite ROADMAP.md not flagging a `UI hint: yes` for Phase 5, this phase **adds a minimal Angular `/notifications` page** (mat-list style, consistent with existing Material conventions) listing inbox entries with links to `/orders/:id`. This makes NOT-01 demoable end-to-end rather than API/Swagger-only, matching the precedent FE-03 set for saga visibility in Phase 4. **Planning/research should flag this phase for a `/gsd-ui-phase 5` pass** even though the roadmap doesn't currently mark it — this decision effectively sets an implicit UI hint.
- **D-06:** The notifications page gets a **simple nav bar link** (consistent with existing catalog/cart/orders toolbar entries) — no unread-count badge, since mark-as-read/unread tracking is deferred to NOT-V2-01 and a badge would need state this phase doesn't track.

### Order Detail Visibility During Fulfillment (SC1/SC2 — order UX)

- **D-07:** `/orders/:id` shows a **"Shipping..." style indicator** while the order is `Paid` but not yet `Fulfilled` — this is a **client-side inference from `status == Paid`**, not a new backend/persisted state. No new intermediate state is added to `OrderStateMachine` (it stays `Pending → Paid → Fulfilled/Cancelled/Failed`, unchanged from Phase 3/4) — the frontend alone renders the "in progress" affordance to satisfy SC1's "visible in the saga state and order detail view" language.
- **D-08:** `/orders/:id` **reuses Phase 4's existing polling mechanism** (~1–2s interval, established in D-07/D-08 of `04-CONTEXT.md`), extending its stop-condition to include `Fulfilled` (in addition to the existing `Cancelled`/`Failed`) so the page keeps polling until the order reaches any terminal state, then stops. No new polling pattern introduced.

### Claude's Discretion

- Exact contract/event name and shape for the "order paid" signal Fulfillment consumes — whether it's a new dedicated `OrderPaid` event (matching ROADMAP.md's literal wording) or Fulfillment subscribes to the existing `OrderStatusChanged` event filtered to `NewStatus == "Paid"`. Both `OrderStatusChanged` and `PaymentAuthorisedEvent` already exist and fire on the Paid transition (see `OrderStateMachine.cs`) — this is a contract-design/topic-wiring call for research and planning, informed by ADR-0007 (topic-per-context), not a user-vision question. Whichever is chosen must carry `UserId` per D-03.
- How the Orders saga is wired to react to `OrderShipped` (a new `Event<OrderShipped>` binding on `During(Paid, ...)` transitioning to `Fulfilled`, mirroring the existing `FulfillmentFailedEvent` binding pattern) — mechanical, follows the established saga pattern.
- Whether Fulfillment needs its own persisted state (EF Core DB + outbox/inbox, mirroring Payments' idempotency approach) to avoid double-processing on message redelivery during the timer wait, or whether MassTransit's own inbox/scheduling primitives suffice — technical/research call.
- Exact `Fulfillment:ProcessingSeconds` (or equivalent) config key name and default value — Claude finalizes during planning.
- Notifications inbox message copy exact wording beyond the three examples in D-04 — Claude can polish.
- Exact visual treatment of the "Shipping..." indicator (spinner, badge, text) — deferred to a `/gsd-ui-phase 5` pass per D-05.

</decisions>

<canonical_refs>
## Canonical References

**Downstream agents MUST read these before planning or implementing.**

### Phase Requirements and Roadmap
- `.planning/ROADMAP.md` — Phase 5 goal, 4 success criteria (SC1–SC4), requirements mapping (FUL-01, FUL-02, NOT-01, NOT-02); note ROADMAP.md does not currently flag a UI hint for this phase, but D-05 effectively requires one
- `.planning/REQUIREMENTS.md` — Full requirement definitions; Phase 5 traceability section

### Project Structure and Constraints
- `.planning/PROJECT.md` — "no direct coupling" event-driven premise (Core Value) — directly informs D-03's decision to enrich events with UserId rather than add synchronous lookups
- `CLAUDE.md` — Full technology stack constraints: MassTransit 8.3.6 pin, EF Core + Npgsql, Angular 20 conventions

### Architecture Decision Records (all in `docs/adr/`)
- `docs/adr/0005-saga-orchestration.md` — **MUST read.** Locks orchestration (not choreography) — the Orders saga (`OrderStateMachine`) is the single place that reacts to `OrderShipped`/`FulfillmentFailed`; Fulfillment does not orchestrate.
- `docs/adr/0006-masstransit-outbox-inbox.md` — MassTransit 8.3.6 pin, outbox/inbox pattern — Fulfillment (as a new publisher) and Notifications (as a consumer, already partially wired) must both follow this.
- `docs/adr/0007-asb-topic-per-context.md` — Topic-per-context convention — relevant to where `OrderShipped` (Fulfillment context) and the "order paid" signal (Orders context) are published, and which topics Notifications subscribes to.
- `docs/adr/0009-checkout-saga-state-reconciliation.md` — Phase 4's saga state-shape decisions (`Pending/Paid/Fulfilled/Cancelled/Failed`) — Phase 5 does NOT add new persisted states (per D-07), stays consistent with this ADR.

### Prior Phase Context
- `.planning/phases/04-checkout-saga-payments/04-CONTEXT.md` — D-06/D-07/D-08: existing `/checkout` step-indicator and polling pattern that D-08 (this phase) extends to `/orders/:id`; D-01/D-02: the Phase 4 fulfillment-failure demo trigger that D-02 (this phase) explicitly keeps as the only failure path.
- `.planning/phases/03-cart-orders-skeleton/03-CONTEXT.md` — D-06/D-07/D-08: Orders CQRS read-model and state-machine shape that Phase 5 builds on without altering.
- `.planning/phases/02-identity-catalog-gateway/02-CONTEXT.md` — D-13/D-14: Notifications service's original inbox-consumer pattern (`CatalogSeededConsumer`, MassTransit InMemory-harness forced-redelivery test) — Phase 5's NOT-02 idempotency test follows the same test shape.

### Existing Scaffold
- `src/services/orders/ECommerce.Orders.API/Features/Orders/OrderStateMachine.cs` — **MUST read.** Current saga: `Pending → Paid → Fulfilled/Cancelled/Failed`. Already has `Event<FulfillmentFailedEvent>` bound in `During(Paid, ...)` — Phase 5 adds a parallel `Event<OrderShipped>` binding here for the `Paid → Fulfilled` transition. Every `During()` block ends with defensive `Ignore(...)` catch-alls for all six registered event types — any new event Fulfillment introduces must be added to those catch-alls too (see the extensive in-file comments explaining why, e.g. CR-01/CR-02/WR-02 notes).
- `src/services/orders/ECommerce.Orders.API/Features/Orders/CheckoutOptions.cs` — Pattern to mirror for a new `FulfillmentOptions`/similar config class backing D-01.
- `src/building-blocks/Contracts/Orders/Events/V1/OrderStatusChanged.cs` — Existing event already fired on the Paid transition; candidate for D-03's UserId addition and for Fulfillment's consumption target (see Claude's Discretion).
- `src/building-blocks/Contracts/Fulfillment/Events/V1/FulfillmentFailed.cs` — Existing Fulfillment-context event (already consumed by the saga since Phase 4); `OrderShipped` should be added alongside it in `Contracts/Fulfillment/Events/V1/`.
- `src/building-blocks/Contracts/Fulfillment/Commands/V1/Placeholder.cs` — Replace if Fulfillment needs a command (e.g. `ProcessFulfillment`) rather than reacting directly to the Orders/Payments event.
- `src/building-blocks/Contracts/Notifications/Events/V1/Placeholder.cs`, `.../Notifications/Commands/V1/Placeholder.cs` — Notifications' own placeholders; likely stay as-is since Notifications only consumes other contexts' events, doesn't publish its own domain events.
- `src/services/fulfillment/ECommerce.Fulfillment.API/Program.cs` — **Bare stub** (health check + OTel only, no EF Core, no MassTransit). Needs full build-out: DbContext, outbox, consumer(s), scheduling.
- `src/services/notifications/ECommerce.Notifications.API/Program.cs` — Already has MassTransit + outbox/inbox wiring and `NotificationsDbContext` (currently only `AddInboxStateEntity`/`AddOutboxMessageEntity`/`AddOutboxStateEntity` — no actual `NotificationEntry` entity yet). Needs: new consumers for the paid/shipped/failed events, a real `NotificationEntry` table, and `GET /notifications` endpoint.
- `src/services/notifications/ECommerce.Notifications.API/Consumers/CatalogSeededConsumer.cs` — **MUST read.** Existing consumer pattern (constructor-injected `DbContext` + `ILogger`, relies on MassTransit inbox for dedup) to replicate for the new Phase 5 consumers.
- `src/services/orders/ECommerce.Orders.API/Program.cs` and AppHost wiring — confirm Fulfillment/Notifications ASB topic subscriptions during planning.

</canonical_refs>

<code_context>
## Existing Code Insights

### Reusable Assets
- `Tests.Common` (from Phase 2/3/4): `PostgresFixture`, `WebApplicationFactory` base, builder pattern — extend with `FulfillmentBuilder`/`NotificationBuilder` as needed.
- Two-class test pattern (`*Tests`/`*Steps`, `Given_/When_/Then_`) — reuse for Fulfillment and Notifications test suites.
- `CatalogSeededConsumer` — the only existing "real" MassTransit consumer in the codebase; directly reusable shape for Notifications' new consumers.
- MassTransit.Quartz scheduling (`Schedule()`/`ctx.Init<T>()` pattern in `OrderStateMachine.cs`) — reusable for Fulfillment's timer-based processing (D-01), whether via a Fulfillment-owned saga/state machine or a simple scheduled redelivery.

### Established Patterns (from Phase 1–4 Context)
- `net10.0 + ImplicitUsings + Nullable enable` baseline for all projects.
- One `.sln` per service; `Fulfillment.sln`/`Notifications.sln` already exist, reference `Contracts.csproj` by relative path.
- MassTransit 8.3.6 explicitly pinned wherever referenced.
- Vertical-slice feature folder structure — apply to Fulfillment and Notifications feature code.
- IDOR-safe endpoint pattern from Orders (identical 404 for non-existent vs. other-user resources) — apply to `GET /notifications` (a user must only ever see their own inbox entries).
- Every saga `During()` block requires defensive `Ignore(...)` catch-alls for every registered event type (established through Phase 4's CR-01/CR-02/WR-02 review cycles) — apply the same discipline to any new event added to `OrderStateMachine`.

### Integration Points
- `src/ecommerce.AppHost/Program.cs` — Fulfillment, Notifications services already wired as Aspire resources from Phase 1 stubs; confirm during planning.
- YARP gateway `appsettings.json` — add `/api/notifications/**` route prefix (Fulfillment likely stays internal-only, no direct client access), consistent with existing `/api/{service}/` convention.
- Angular: new `src/app/features/notifications/` feature folder (D-05); `src/app/features/orders/` (Phase 3/4) gets the "Shipping..." indicator added (D-07) and its polling stop-condition extended (D-08); app shell nav gets a new link (D-06).
- Orders service: `OrderStateMachine.cs` gets a new `Event<OrderShipped>` binding in `During(Paid, ...)`, per Claude's Discretion above.

</code_context>

<specifics>
## Specific Ideas

- Fulfillment processing-time config key (working name): `Fulfillment:ProcessingSeconds`, default ~30–60s.
- Inbox message copy (working examples): "Your order has been paid.", "Your order has shipped.", "Payment failed for your order."
- Order detail "in progress" copy (working example): "Shipping..." / "Preparing your shipment" while `status == Paid`.
- Notifications page: simple `mat-list` of inbox entries, each linking to `/orders/:id`; nav bar link added, no badge.

</specifics>

<deferred>
## Deferred Ideas

- Deterministic natural failure trigger for Fulfillment (a PAY-02-style condition) — explicitly rejected for this phase per D-02; Phase 4's manual demo endpoint stays the only compensation-path trigger.
- Mark notifications as read (NOT-V2-01) — V2, unaffected by this phase's UI addition.
- Real email/SMS delivery (NOT-V2-02) — V2.
- Unread-count badge on the notifications nav link — explicitly deferred per D-06 (needs read/unread state this phase doesn't track).

### Reviewed Todos (not folded)
None — no pending todos matched Phase 5 scope (`todo.match-phase` returned 0 matches).

</deferred>

---

*Phase: 5-Fulfillment & Notifications*
*Context gathered: 2026-08-14*
