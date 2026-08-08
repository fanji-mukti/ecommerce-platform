---
status: accepted
date: 2026-08-08
decision-makers: [Fanji Ari Mukti]
consulted: []
informed: []
---

# Reconcile Checkout Vocabulary onto the Existing OrderStateMachine (No New "Started" State)

## Context and Problem Statement

The Phase 4 roadmap describes the checkout-facing flow using the vocabulary `Started → AwaitingPayment → Paid`. The existing `OrderStateMachine` (Phase 3, ADR-0005) already implements and tests a different, locked vocabulary: `Pending → Paid → Fulfilled / Cancelled / Failed` (ORD-03). Both vocabularies describe the same underlying process, but if both were persisted literally, `OrderStateMachine` would need a new leading `Started` state, and `OrderReadModel.Status` would need to support values `OrdersEndpoints` and `OrderReadModelProjector` don't currently expect.

## Decision Drivers

- ORD-03 (already implemented and tested in Phase 3) requires the persisted `Order`/`OrderReadModel.Status` vocabulary to be exactly `Pending → Paid → Fulfilled / Cancelled / Failed` — changing this would break existing, shipped behavior.
- ADR-0005 locks a single orchestrating state machine in Orders — no second saga, no choreography hand-off.
- The checkout-facing UI (D-06, `/checkout`'s step indicator) needs a `Started` step in its vocabulary, but that is a display concern, not necessarily a persistence concern.
- Introducing a new persisted state means a new column value, new EF migration considerations, and new transition-guard tests for a concept — "the saga instance has been created" — that already has an unambiguous technical answer (the saga row exists, or it doesn't).

## Considered Options

- Add a new persisted `Started` state to `OrderStateMachine`, with a real transition `Started -> Pending`.
- Keep `OrderStateMachine`'s existing states exactly as built (`Pending → Paid → Fulfilled / Cancelled / Failed`), and synthesize `Started` at the HTTP layer (Checkout.API, plan 04-05) whenever a saga row does not yet exist.

## Decision Outcome

Chosen: **Keep the existing state machine's states unchanged; synthesize `"Started"` at the HTTP layer.** `OrderStateMachine`'s `Initially(When(OrderCreatedEvent))` transitions straight to `Pending`, exactly as it did in Phase 3 — this plan (04-02) only adds typed payment/fulfillment events and a scheduled timeout onto the existing `Pending`/`Paid` states, with no new state. Checkout.API (plan 04-05) will report `"Started"` to callers whenever `GET /orders/{id}` returns 404 for a `checkoutId` within the brief eventual-consistency window before the saga row is durably projected — mirroring RESEARCH.md's Pitfall 3 mitigation (the internal `Checkout.API → Orders.API` HTTP call waits for the outbox to flush before returning `202 Accepted`, closing most of that window, but `"Started"` remains the correct synthesized answer for the residual gap between the synchronous HTTP response and the read-model projector catching up).

This plan's Task 2 spike (`spikes/04-asb-scheduling-spike/`) additionally resolved RESEARCH.md's Open Question 1 — whether the Azure Service Bus emulator honors `ScheduledEnqueueTime` — with an observed `SPIKE-RESULT: PASS` (2026-08-08). That result is the evidentiary basis for adopting the ASB-native `Schedule`/`Unschedule` scheduler (rather than the Quartz.NET-for-both-environments alternative) for CHK-05's saga timeout in Task 3.

### Consequences

- Good: Zero changes to `OrderStateMachine`'s existing, tested `Initially()`/`During(Pending)`/`During(Paid)` transition guards for the states that already ship — this plan is purely additive (new events, new `.Schedule()`/`.Unschedule()` activities).
- Good: ORD-03's persisted vocabulary is untouched — no migration risk to existing `Order`/`OrderReadModel` rows.
- Good: `"Started"` is derived, not stored — there is exactly one source of truth (saga-row-exists-or-not), not two competing state representations.
- Bad: Checkout.API (plan 04-05) carries the small mapping responsibility of synthesizing `"Started"` from a 404, rather than the saga machine expressing it natively — this mapping logic lives outside `OrderStateMachine.cs` and must be kept in sync if the checkout vocabulary changes again.
- Bad: A caller polling `GET /checkout/{id}` during the (now narrow, but non-zero) window between `202 Accepted` and the read-model projector catching up sees a synthesized status, not a saga-observed one — acceptable per Pitfall 3's mitigation, which already closes most of this window via the synchronous outbox-flush-before-return ordering.

## Pros and Cons of the Options

### Add a persisted `Started` state
- Pro: The saga machine natively expresses every checkout-facing status without an HTTP-layer synthesis step.
- Con: New state requires new transition-guard tests, a new EF migration touching an already-shipped table, and duplicates information the saga-row-existence check already provides for free.
- Con: Widens the surface `OrderStateMachineTests`/`OrderStateMachineSteps` must cover for a state that carries no new business logic (no compensation, no side effect — it is purely "the row exists now").

### Synthesize `"Started"` at the HTTP layer (chosen)
- Pro: No change to ORD-03's already-shipped persisted vocabulary.
- Pro: Keeps `OrderStateMachine.cs`'s diff for this plan scoped to genuinely new saga logic (payment/fulfillment events, timeout scheduling) rather than mixing in a display-only state.
- Con: The synthesis logic lives in a different service (Checkout.API) than the state it's approximating (Orders.API's saga) — requires the two to stay conceptually aligned across future phases.

## More Information

- ADR-0005 (saga orchestration) — locks the single-state-machine-in-Orders decision this ADR builds on.
- ADR-0006 (MassTransit outbox/inbox) — the transactional outbox is what makes the "wait for `SaveChangesAsync` before returning 202" ordering (RESEARCH.md Pitfall 3) safe.
- `spikes/04-asb-scheduling-spike/` — the Task 2 spike whose `SPIKE-RESULT: PASS` outcome underwrites the ASB-native scheduler choice for CHK-05 (see RESEARCH.md, "Open Questions" → Open Question 1).
- The `"Started"`-synthesis HTTP-layer implementation itself is deferred to plan 04-05 (Checkout.API's `GET /checkout/{id}` endpoint) — this ADR records the design decision, not the endpoint code.
