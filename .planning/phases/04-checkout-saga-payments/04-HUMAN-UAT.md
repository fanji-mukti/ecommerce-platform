---
status: partial
phase: 04-checkout-saga-payments
source: [04-VERIFICATION.md]
started: 2026-08-13T23:20:00+08:00
updated: 2026-08-13T23:20:00+08:00
---

## Current Test

[awaiting human testing]

## Tests

### 1. Full happy-path and PaymentFailed demo click-through
expected: Run the full Docker Compose / Aspire stack and click through Cart -> /checkout -> Place Order -> watch the mat-stepper update in real time -> auto-redirect to /orders/:id, for both a normal-priced cart (happy path) and a .99-ending cart (PaymentFailed demo trigger). Stepper advances Started -> AwaitingPayment -> Paid (or -> Cancelled with a visible failure reason), page auto-navigates to /orders/:id with no manual refresh, matching SC1/SC2/SC3.
result: [pending]

### 2. Rapid double fulfillment-failure trigger against a live broker
expected: Click "simulate fulfillment failure" on a Paid order via the checkout/order-detail UI, twice in rapid succession, against a live running system (real ASB, not the in-memory harness). Second delivery is absorbed by During(Cancelled, ...)'s Ignore(FulfillmentFailedEvent) — no dead-lettered message, no faulted consumer.
result: [pending]

### 3. Live saga timeout with in-flight payment response
expected: Leave a checkout un-actioned past the configured 15-minute timeout (or temporarily lower Checkout:TimeoutMinutes) on a live running system and observe whether any in-flight AuthorisePayment response arrives after the timeout has already cancelled the order. No UnhandledEventException / faulted consumer / dead-lettered message in Orders.API logs.
result: [pending]

## Summary

total: 3
passed: 0
issues: 0
pending: 3
skipped: 0
blocked: 0

## Gaps
