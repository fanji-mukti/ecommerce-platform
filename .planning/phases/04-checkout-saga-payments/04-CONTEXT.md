# Phase 4: Checkout Saga & Payments - Context

**Gathered:** 2026-08-08
**Status:** Ready for planning

<domain>
## Phase Boundary

A user clicks "Place Order" and the checkout saga orchestrates Order creation, simulated payment, and compensation paths end-to-end — the headline demo of the whole project. `POST /checkout` returns `202 Accepted` with a `checkoutId`; `GET /checkout/{id}` (and the Angular `/checkout` + `/orders/:id` pages) poll the saga/order status in real time. Happy path: `Started → AwaitingPayment → Paid`. Two compensation paths must work end-to-end: a deterministic payment failure (cart total ending in `.99`) and a simulated fulfillment failure (Fulfillment service doesn't exist until Phase 5, so this is faked). An incomplete checkout also times out (~15 min) and cascades the same compensation path.

**Requirements in scope:** CHK-01, CHK-02, CHK-03, CHK-04, CHK-05, PAY-01, PAY-02, PAY-03, FE-03

**Explicitly NOT in scope:** real Fulfillment service logic (Phase 5), real Notifications inbox (Phase 5), real payment provider (out of scope for the whole project — PAY-V2-01).

</domain>

<decisions>
## Implementation Decisions

### Fulfillment-Failure Demo Trigger (CHK-04)

- **D-01:** Since Fulfillment doesn't exist until Phase 5, the fulfillment-failure compensation path is triggered by a demo-only test endpoint (e.g. `POST /checkout/{id}/simulate-fulfillment-failure`), mirroring Phase 3's `POST /orders/test-create-from-cart` pattern: clearly marked as non-production/demo-only, easy for Phase 5 to retire once the real Fulfillment service exists.
- **D-02:** This trigger is API/test-only — no Angular UI control for it in Phase 4. It's called via Swagger/curl during live demo narration or exercised by integration tests.
- **D-03:** The endpoint is callable any time the order is in `Paid` status — no additional timing/window restriction.

### Saga Timeout (CHK-05)

- **D-04:** The ~15-minute timeout is **configurable**, not hardcoded — e.g. `Checkout:TimeoutMinutes` (or similar) in appsettings/env var, defaulting to 15 for the "real" value but overridable to a short interval for local/live demos.
- **D-05:** The timeout path is proven via an xUnit integration test that injects a short test-only timeout value and asserts the compensation path (same as explicit failure) fires correctly — this validates the mechanism without requiring a live 15-minute wait, and works regardless of what the configured production default is.

### Checkout/Orders UX During the Saga (FE-03)

- **D-06:** `/checkout` shows a **step indicator** (e.g. `mat-stepper` or an ordered list) that lights up each saga stage as it's reached (`Started → AwaitingPayment → Paid`, or the failure/cancellation equivalent) — makes the orchestration visible, which is the point of this phase's demo.
- **D-07:** `/checkout` polls status on a **fixed short interval (~1–2s)** until a terminal state is reached, then stops polling.
- **D-08:** On reaching a terminal state, the page **auto-redirects to `/orders/:id`** — mirrors a real checkout flow.
- **D-09:** `/orders/:id` shows **status + a human-readable failure reason** on failure (e.g. "Payment declined", "Fulfillment failed — order cancelled and refunded"), not just the bare terminal status. This requires threading a failure reason through the saga into the Orders read model.

### Payment-Failure Demo Affordance (PAY-02)

- **D-10:** `/checkout` shows **hint text** near the cart total / place-order button (e.g. "Tip: cart totals ending in .99 simulate a payment failure") so the compensation path is discoverable during a live demo without prior knowledge.
- **D-11:** In addition to the natural `.99`-ending-total trigger, add an explicit **"Simulate payment failure" demo toggle/checkbox** on `/checkout` that force-triggers the same failure path regardless of the actual cart total — this is UI scope beyond PAY-02's literal wording, added deliberately for demo reliability (don't need to massage cart contents to land on `.99` live).

### Claude's Discretion

- Exact reconciliation between the roadmap's `Started`/`AwaitingPayment`/`Paid` state names (SC2) and the existing `OrderStateMachine`'s `Pending`/`Paid`/`Fulfilled`/`Cancelled`/`Failed` states (built in Phase 3, see Code Context below) — whether to rename/extend the existing state machine in place, add new states, or introduce a distinct pre-order "checkout" phase before `OrderCreated` fires. ADR-0005 already locks the saga as a single orchestrating state machine in the Orders service — the exact state shape is a research/planning call, not a user-vision question.
- Whether `Checkout.API` (existing stub service, kept because PROJECT.md counts "eight independent services") is a thin façade that publishes a command and proxies `GET /checkout/{id}` to the Orders read model, or holds any state of its own — implementation detail for planning.
- Exact `AuthorisePayment`/`RefundPayment` command/event shapes, Payments service's own idempotency-key storage — technical detail for research/planning.
- Visual styling of the step indicator, hint text placement, and demo-toggle placement — deferred to `/gsd-ui-phase 4` (this phase has `UI hint: yes`).

</decisions>

<canonical_refs>
## Canonical References

**Downstream agents MUST read these before planning or implementing.**

### Phase Requirements and Roadmap
- `.planning/ROADMAP.md` — Phase 4 goal, 5 success criteria (SC1–SC5), requirements mapping (CHK-01..05, PAY-01..03, FE-03)
- `.planning/REQUIREMENTS.md` — Full requirement definitions; Phase 4 traceability section

### Project Structure and Constraints
- `.planning/PROJECT.md` — Repo directory layout, "eight independent services" narrative (Checkout.API is one of them — do not retire it)
- `CLAUDE.md` — Full technology stack constraints: MassTransit 8.3.6 pin, EF Core + Npgsql, Angular 20 conventions

### Architecture Decision Records (all in `docs/adr/`)
- `docs/adr/0005-saga-orchestration.md` — **MUST read.** Locks orchestration (not choreography): a single MassTransit state machine in the Orders service drives the checkout saga; compensation paths are explicit state transitions.
- `docs/adr/0006-masstransit-outbox-inbox.md` — MassTransit 8.3.6 pin, outbox/inbox pattern already wired in Orders (Phase 3) — the saga's state persistence follows this.
- `docs/adr/0002-azure-service-bus.md` — ASB choice; relevant to saga timeout scheduling mechanics research (flagged as highest technical risk in STATE.md).
- `docs/adr/0007-asb-topic-per-context.md` — Topic-per-context convention for the new Checkout/Payments events.

### Prior Phase Context
- `.planning/phases/03-cart-orders-skeleton/03-CONTEXT.md` — D-01/D-02: the Phase 3 test-trigger endpoint (`POST /orders/test-create-from-cart`) is explicitly a stand-in that **Phase 4 replaces/wraps** with the real saga-driven `/checkout` endpoint. D-06/D-07/D-08: existing Orders CQRS/state-machine shape (see Code Context).
- `.planning/phases/02-identity-catalog-gateway/02-CONTEXT.md` — YARP route-prefix convention (`/api/{service}/...`), MassTransit 8.3.6 pin discipline, test infrastructure pattern (`Tests.Common`, two-class test suites).
- `.planning/phases/01-foundations/01-CONTEXT.md` — Project naming (`ECommerce.{ServiceName}.API`), .sln-per-service pattern, OTel/Serilog wiring baseline.

### Existing Scaffold
- `src/services/orders/ECommerce.Orders.API/Features/Orders/OrderStateMachine.cs` — **MUST read.** The first (and so far only) MassTransit saga state machine in the codebase. Currently enforces `Pending → Paid → Fulfilled / Cancelled / Failed` on the `Order` entity itself (`InstanceState`, correlated by `OrderId`). Phase 4 must reconcile this with SC2's `Started → AwaitingPayment → Paid` language.
- `src/services/checkout/ECommerce.Checkout.API/Program.cs` — Existing bare stub (health check + OTel only, no endpoints yet) to expand with `POST /checkout` and `GET /checkout/{id}`.
- `src/services/payments/ECommerce.Payments.API/Program.cs` — Existing bare stub, no MassTransit wired yet.
- `src/services/fulfillment/ECommerce.Fulfillment.API/Program.cs` — Existing bare stub; not implemented in Phase 4 (Phase 5 scope) beyond what's needed to receive the simulated-failure trigger's compensation events.
- `src/building-blocks/Contracts/Checkout/Commands/V1/Placeholder.cs`, `.../Checkout/Events/V1/Placeholder.cs` — replace with real Checkout contracts.
- `src/building-blocks/Contracts/Payments/Commands/V1/Placeholder.cs`, `.../Payments/Events/V1/Placeholder.cs` — replace with `AuthorisePayment`, `PaymentAuthorised`/`PaymentFailed`, `RefundPayment` (or similar).
- `src/building-blocks/Contracts/Fulfillment/Commands/V1/Placeholder.cs`, `.../Fulfillment/Events/V1/Placeholder.cs` — replace with whatever the simulated fulfillment-failure event needs (e.g. `FulfillmentFailed`), consumed directly by the Orders saga.
- `src/building-blocks/Tests.Common/ECommerce.Tests.Common.csproj` — reuse `PostgresFixture`, `WebApplicationFactory` base, builders for Checkout/Payments integration tests.

</canonical_refs>

<code_context>
## Existing Code Insights

### Reusable Assets
- `Tests.Common` (from Phase 2/3): `PostgresFixture` (Testcontainers), `WebApplicationFactory` base class, builder pattern (`OrderBuilder`, etc.) — extend with `PaymentBuilder`/`CheckoutBuilder` as needed.
- Phase 2/3's two-class test pattern (`*Tests` / `*Steps`, `Given_/When_/Then_` naming) — reuse for Checkout/Payments test suites.
- `OrderReadModelProjector` (Phase 3) — the pattern for idempotently projecting saga/domain events into a read model; likely needs new event types added (e.g. status change with failure reason).

### Established Patterns (from Phase 1–3 Context)
- `net10.0 + ImplicitUsings + Nullable enable` baseline for all projects.
- One `.sln` per service; `Checkout.sln`, `Payments.sln`, `Fulfillment.sln` already exist and reference `Contracts.csproj` by relative path.
- MassTransit 8.3.6 explicitly pinned wherever referenced.
- Vertical-slice feature folder structure (e.g. `ECommerce.Orders.API.Features.Orders`) established in Phase 2/3 — apply to Checkout and Payments feature code.
- IDOR-safe endpoint pattern from Orders (identical 404 for non-existent vs. other-user resources) — apply to `GET /checkout/{id}`.

### Integration Points
- `src/ecommerce.AppHost/Program.cs` — Checkout, Payments, Fulfillment services likely already wired as Aspire resources from Phase 1 stubs; confirm during planning.
- YARP gateway `appsettings.json` — add `/api/checkout/**` route prefix (Payments/Fulfillment likely stay internal-only, no direct client access), consistent with the existing `/api/{service}/` convention.
- Angular: new `src/app/features/checkout/` feature folder (checkout page with step indicator, polling, hint text, demo toggle); `src/app/features/orders/` (from Phase 3, if it exists) gets a failure-reason display added to the order detail view.
- Orders service: `OrderStateMachine.cs` is the most likely home for the saga logic per ADR-0005 — Phase 4 planning must decide whether this file is extended or a new state machine/states are introduced alongside it.

</code_context>

<specifics>
## Specific Ideas

- Fulfillment-failure trigger endpoint name (working name): `POST /checkout/{id}/simulate-fulfillment-failure` — Claude can finalize exact route.
- Timeout config key (working name): `Checkout:TimeoutMinutes`, default `15`.
- Payment-failure UI hint text (working copy): "Tip: cart totals ending in .99 simulate a payment failure."
- Demo toggle label (working copy): "Simulate payment failure."
- Failure-reason examples for `/orders/:id`: "Payment declined", "Fulfillment failed — order cancelled and refunded".

</specifics>

<deferred>
## Deferred Ideas

- Real Fulfillment service logic (timer-based shipping simulation, `OrderShipped`) — Phase 5.
- Real Notifications inbox surfacing saga lifecycle events — Phase 5.
- Retiring the Phase 3 `POST /orders/test-create-from-cart` test-trigger endpoint — happens as part of this phase since `/checkout` replaces it (per Phase 3's D-01/D-02), but exact removal mechanics are a planning detail, not re-discussed here.
- Real payment provider integration (Stripe) — PAY-V2-01, out of scope for the whole project's v1.

### Reviewed Todos (not folded)
None — no pending todos matched Phase 4 scope (`todo.match-phase` returned 0 matches).

</deferred>

---

*Phase: 4-Checkout Saga & Payments*
*Context gathered: 2026-08-08*
