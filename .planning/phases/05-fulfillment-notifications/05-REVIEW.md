---
phase: 05-fulfillment-notifications
reviewed: 2026-08-21T00:00:00Z
depth: standard
files_reviewed: 17
files_reviewed_list:
  - src/frontend/ecommerce-app/src/app/features/orders/order-detail/order-detail.component.spec.ts
  - src/frontend/ecommerce-app/src/app/features/orders/order-detail/order-detail.component.ts
  - src/services/notifications/ECommerce.Notifications.API/Consumers/OrderShippedNotificationConsumer.cs
  - src/services/notifications/ECommerce.Notifications.API/Consumers/OrderStatusSnapshotConsumer.cs
  - src/services/notifications/ECommerce.Notifications.API/Data/NotificationsDbContext.cs
  - src/services/notifications/ECommerce.Notifications.API/Features/Notifications/OrderStatusSnapshot.cs
  - src/services/notifications/ECommerce.Notifications.API/Migrations/20260820154643_AddOrderStatusSnapshot.cs
  - src/services/notifications/ECommerce.Notifications.API/Migrations/NotificationsDbContextModelSnapshot.cs
  - src/services/notifications/ECommerce.Notifications.API/Program.cs
  - src/services/notifications/ECommerce.Notifications.Tests/Integration/OrderShippedNotificationConsumerSuppressionSteps.cs
  - src/services/notifications/ECommerce.Notifications.Tests/Integration/OrderShippedNotificationConsumerSuppressionTests.cs
  - src/services/notifications/ECommerce.Notifications.Tests/Integration/OrderShippedNotificationInboxDeduplicationSteps.cs
  - src/services/notifications/ECommerce.Notifications.Tests/Integration/OrderShippedNotificationInboxDeduplicationTests.cs
  - src/services/notifications/ECommerce.Notifications.Tests/Integration/OrderStatusSnapshotConsumerSteps.cs
  - src/services/notifications/ECommerce.Notifications.Tests/Integration/OrderStatusSnapshotConsumerTests.cs
  - src/services/notifications/ECommerce.Notifications.Tests/Integration/PaymentFailedNotificationInboxDeduplicationSteps.cs
  - src/services/notifications/ECommerce.Notifications.Tests/Integration/PaymentFailedNotificationInboxDeduplicationTests.cs
findings:
  critical: 0
  warning: 2
  info: 2
  total: 4
status: issues_found
---

# Phase 05: Code Review Report

**Reviewed:** 2026-08-21T00:00:00Z
**Depth:** standard
**Files Reviewed:** 17
**Status:** issues_found

## Scope note

This review covers only the gap-closure delta introduced by plans 05-09, 05-10, and 05-11, which
close CR-01 (false "shipped" notification for cancelled/refunded orders), WR-02 (asymmetric
inbox-deduplication test coverage), and WR-03 (order-detail polling stops permanently on a
transient HTTP error) from the phase's original `05-REVIEW.md` and `05-VERIFICATION.md`. It is
**not** a full phase re-review. The base 56 files from plans 05-01..05-08 were previously reviewed
at standard depth in the earlier pass (`05-REVIEW.md`, `files_reviewed: 56`) and found clean aside
from the findings that these three gap-closure plans were written to address; they are unchanged by
this delta and are not re-reviewed here. This report **overwrites** the prior `05-REVIEW.md`.

The commit range actually verified for this delta is `f358d18^..495e0a6` (the 05-09/05-10/05-11
commits merged into `phase05/Fulfillment-Notifications`), confirmed via `git diff --stat` to touch
exactly the 17 files listed above (18 including the migration's `.Designer.cs`, which is not in the
reviewed file list but was inspected as a byproduct of the migration).

## Summary

All three targeted gaps are closed correctly and the fixes are minimal and well-targeted:

- **CR-01** is closed by a new `OrderStatusSnapshotConsumer` (locally-owned order-status
  read-model, avoiding a synchronous call back to Orders) plus a suppression check added to
  `OrderShippedNotificationConsumer` that skips the "shipped" notification when the last known
  status is `Cancelled`/`Failed`. The fix matches the shape suggested in the original CR-01 finding
  and is covered by four new suppression tests plus three snapshot-consumer tests (insert,
  update-on-newer, and reject-stale-out-of-order).
- **WR-02** is closed by two new Postgres-backed inbox-deduplication integration tests
  (`OrderShippedNotificationInboxDeduplicationTests`, `PaymentFailedNotificationInboxDeduplicationTests`)
  that mirror the existing `OrderPaidNotificationInboxDeduplicationTests` pattern faithfully.
- **WR-03** is closed by a single, surgical change — wrapping the per-tick `getOrder(id)` call in
  `catchError(() => EMPTY)` inside the `switchMap` — exactly as the original finding's fix
  suggestion described. The `git diff` confirms this is a 2-line change with no incidental
  behavior changes, and the new spec covers both the "transient error, then recovers" and
  "destroy stops polling" cases.

No new critical issues were introduced. Two warnings and two info items were found in the new
`OrderStatusSnapshotConsumer` — the linchpin of the CR-01 fix — around concurrent-write safety and
test-coverage symmetry with its sibling consumers.

## Warnings

### WR-01: `OrderStatusSnapshotConsumer`'s find-then-write is not safe under concurrent delivery of two `OrderStatusChanged` messages for the same order

**File:** `src/services/notifications/ECommerce.Notifications.API/Consumers/OrderStatusSnapshotConsumer.cs:22-37`

**Issue:** The consumer does a classic check-then-act:

```csharp
var existing = await db.OrderStatusSnapshots.FindAsync([msg.OrderId], context.CancellationToken);

if (existing is null)
{
    db.OrderStatusSnapshots.Add(new OrderStatusSnapshot { OrderId = msg.OrderId, ... });
}
else if (msg.ChangedAt >= existing.UpdatedAt)
{
    existing.Status = msg.NewStatus;
    existing.UpdatedAt = msg.ChangedAt;
}
```

If two `OrderStatusChanged` messages for the *same* `OrderId` (e.g. `Paid` followed shortly by
`Cancelled`, which is exactly the CR-01 reproduction scenario) are dispatched to two concurrently
running instances of this consumer, both can read `existing == null` before either commits, and
both will attempt `Add` with the same `OrderId` primary key. `Program.cs` does not configure a
`ConcurrentMessageLimit`/partitioner/session on this consumer's receive endpoint, so nothing in the
reviewed code rules this out — MassTransit's default receive-endpoint concurrency can dispatch more
than one message at a time. The result is a `DbUpdateException` (unique-constraint violation) on
`SaveChangesAsync`, which throws out of `Consume` uncaught. Because there is no visible
`UseMessageRetry` policy configured for this consumer's endpoint, the message is likely faulted
rather than retried, meaning the snapshot for that order can be left stale/missing — precisely the
condition the CR-01 fix depends on to suppress a false "shipped" notification.

**Fix:** Make the write idempotent under races, e.g. an upsert via raw SQL (`ON CONFLICT (order_id)
DO UPDATE ... WHERE excluded.updated_at >= order_status_snapshots.updated_at`), or catch
`DbUpdateException` on the insert path and retry as an update, or constrain this consumer's receive
endpoint to `ConcurrentMessageLimit = 1` / use a partitioner keyed on `OrderId` so per-order
ordering and mutual exclusion are guaranteed.

### WR-02: `OrderStatusSnapshotConsumer` has no dedicated inbox-deduplication integration test, unlike its sibling consumers in this same delta

**File:** `src/services/notifications/ECommerce.Notifications.Tests/Integration/OrderStatusSnapshotConsumerSteps.cs`

**Issue:** Plan 05-10 added `OrderShippedNotificationInboxDeduplicationTests` and
`PaymentFailedNotificationInboxDeduplicationTests` specifically to close the WR-02 gap ("every
consumer should have a redelivery/dedup test"). `OrderStatusSnapshotConsumer` is new in this same
delta (05-09) and is consumed on a receive endpoint that gets the same
`UseEntityFrameworkOutbox<NotificationsDbContext>` inbox middleware as every other consumer
(`Program.cs:48-51`), yet its `Steps` class builds the harness with only
`services.AddMassTransitTestHarness(x => x.AddConsumer<OrderStatusSnapshotConsumer>())` — no
`AddEntityFrameworkOutbox`, no Postgres fixture, and no assertion on `InboxState`/duplicate
delivery. This reintroduces exactly the asymmetric-coverage pattern WR-02 was written to eliminate,
for the one consumer that is arguably most safety-critical here (its correctness is what CR-01's
suppression check relies on).

**Fix:** Add an `OrderStatusSnapshotInboxDeduplicationTests`/`Steps` pair mirroring
`OrderShippedNotificationInboxDeduplicationSteps.cs` (Postgres-backed harness with
`AddEntityFrameworkOutbox`, publish the same pinned `MessageId` twice, assert one `InboxState` row
and the snapshot reflects a single applied update, not two).

## Info

### IN-01: Status literals ("Cancelled"/"Failed") are duplicated as unchecked string constants across the suppression check with no shared enum/constant

**File:** `src/services/notifications/ECommerce.Notifications.API/Consumers/OrderShippedNotificationConsumer.cs:16`

**Issue:** `snapshot.Status == "Cancelled" || snapshot.Status == "Failed"` re-derives the exact
status spelling used independently in `OrderStateMachine.cs` (`NewStatus: "Cancelled"`, etc.).
There is no shared constant or enum tying these together, so a future rename/typo of the status
string in the Orders service (or in this file) would silently defeat the CR-01 suppression fix —
`snapshot.Status` would simply never match, and the false "shipped" notification would resurface
with no compiler or test signal pointing at the root cause. (This mirrors a pre-existing pattern
already used elsewhere in the codebase for these same status strings, so it isn't a new class of
problem — but it is now guarding a correctness-critical suppression check.)

**Fix:** Introduce a shared `OrderStatus` constants class/enum in `Contracts` (or at minimum a
`private static readonly string[] TerminalNonShippedStatuses = ["Cancelled", "Failed"]` local
constant reused by both the check and its tests) so a spelling drift fails loudly rather than
silently.

### IN-02: The CR-01 fix narrows, but does not eliminate, the false-"shipped"-notification race — this residual risk isn't documented anywhere

**File:** `src/services/notifications/ECommerce.Notifications.API/Consumers/OrderShippedNotificationConsumer.cs:15-23`, `src/services/notifications/ECommerce.Notifications.API/Consumers/OrderStatusSnapshotConsumer.cs`

**Issue:** The suppression check only works if `OrderStatusSnapshotConsumer` has already applied
the `Cancelled`/`Failed` transition to the snapshot table by the time the delayed `OrderShipped`
event is consumed. `OrderStatusChanged` and `OrderShipped` are published from different services
(Orders vs. Fulfillment) on independent receive endpoints with no message session/partitioning tying
their delivery order together, so strict ordering is not guaranteed by the transport. In practice
the original 45-second `SchedulePublish` delay in Fulfillment (`OrderPaidConsumer`) gives ample time
for the snapshot update to land, so this is a large improvement over the pre-fix behavior (which had
*no* mitigation at all) — but it is a best-effort narrowing of the race window, not a hard guarantee,
and none of the new code comments, tests, or the 05-09 plan/summary call this out as an accepted
residual risk.

**Fix:** No code change required; add a one-line comment on the suppression check (or in the
consumer's existing XML doc) noting that this is a best-effort mitigation dependent on the
snapshot being updated before the delayed `OrderShipped` arrives, and that a stronger guarantee
would require Fulfillment to re-check order status synchronously (or via a saga-owned cancellation
token) immediately before publishing, as the original CR-01 finding's alternate fix suggested.

---

_Reviewed: 2026-08-21T00:00:00Z_
_Reviewer: Claude (gsd-code-reviewer)_
_Depth: standard_
