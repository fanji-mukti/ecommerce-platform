# Walking Skeleton: ECommerce Platform — Phase 1

**Phase:** 01-foundations
**Deliverable:** `docker compose up` brings up all 8 service stubs with `/health` 200 responses, Postgres, Redis, and ASB emulator, all visible in the Aspire dashboard. The repo compiles cleanly across 10 solutions. 8 MADR ADRs document every foundational architectural choice.

---

## What "Done" Looks Like

After Phase 1 completes, a developer can:

1. Run `dotnet run --project src/ecommerce.AppHost` — Aspire launches 8 service containers + Postgres + Redis + ASB emulator + OTel dashboard.
2. Open `http://localhost:18888` (Aspire dashboard) — all 8 services appear as separate trace sources.
3. `GET http://localhost:{port}/health` on each service — returns HTTP 200 with `{"status":"Healthy"}`.
4. `docker compose up` (after `aspire publish -o ./`) — same stack starts with no Aspire dependency.
5. `dotnet build` across all 10 solutions — zero errors.
6. CI (`push` to `main`) — 10 parallel matrix jobs pass.

No business logic exists. All services are stubs. No auth, no data persistence, no ASB consumers.

---

## Architectural Decisions (locked in Phase 1)

| Decision | Choice | ADR |
|----------|--------|-----|
| ADR format | MADR 4.0 | 0001 |
| Messaging backbone | Azure Service Bus (Standard tier, topics/subscriptions) | 0002 |
| Persistence isolation | Database-per-service (enforced via Aspire-provisioned DBs) | 0003 |
| API gateway | YARP 2.x reverse proxy | 0004 |
| Saga pattern | Orchestration (MassTransit state machines) over choreography | 0005 |
| Messaging library | MassTransit 8.3.6 (Apache-2.0; v9 is commercial) + transactional outbox/inbox | 0006 |
| ASB topic design | One topic per producing bounded context (`[EntityName]` override in Phase 2) | 0007 |
| Repo structure | Mono-repo, multi-solution (per-service `.sln` + shared Contracts via `<ProjectReference>`) | 0008 |

---

## Runtime Stack

| Layer | Technology | Version | Notes |
|-------|-----------|---------|-------|
| Service runtime | .NET 10 (ASP.NET Core Minimal APIs) | 10.0 LTS | All 8 stubs target `net10.0` |
| Local orchestration | .NET Aspire AppHost | 13.4.0 | Single entry point for all services + infra |
| Compose export | `Aspire.Hosting.Docker` + `aspire publish` | 13.4.0 | `docker-compose.yml` at repo root is generated, not hand-authored |
| Structured logging | Serilog + `Serilog.Sinks.OpenTelemetry` | 10.0.0 / 4.2.0 | W3C TraceContext enrichment via `Enrich.FromLogContext()` |
| Distributed tracing | OpenTelemetry SDK + `AddAspNetCoreInstrumentation` | 1.15.3 / 1.15.2 | OTLP export to Aspire dashboard |
| Messaging (future) | MassTransit 8.3.6 (Apache-2.0) | 8.3.6 | Pinned to last OSS release; v9 requires Massient commercial license |
| Message broker | Azure Service Bus emulator | latest | `mcr.microsoft.com/azure-messaging/servicebus-emulator` via `RunAsEmulator()` |
| Database | PostgreSQL | Aspire-managed | Aspire `AddPostgres()` — one container for Phase 1 |
| Cache | Redis | Aspire-managed | Aspire `AddRedis()` |

---

## Directory Layout (post-Phase 1)

```
ecommerce-platform/
├── src/
│   ├── building-blocks/
│   │   └── Contracts/
│   │       ├── Contracts.csproj          ← net10.0, zero NuGet refs (D-03)
│   │       ├── Contracts.sln             ← CI matrix entry
│   │       ├── IMessageEnvelope.cs       ← 4-property interface (D-01)
│   │       ├── Catalog/Events/V1/Placeholder.cs
│   │       ├── Cart/Events/V1/Placeholder.cs
│   │       ├── Checkout/Events/V1/Placeholder.cs
│   │       ├── Orders/Events/V1/Placeholder.cs
│   │       ├── Identity/Events/V1/Placeholder.cs
│   │       ├── Payments/Events/V1/Placeholder.cs
│   │       ├── Fulfillment/Events/V1/Placeholder.cs
│   │       └── Notifications/Events/V1/Placeholder.cs
│   ├── services/
│   │   ├── catalog/
│   │   │   ├── Catalog.sln
│   │   │   └── ECommerce.Catalog.API/
│   │   │       ├── ECommerce.Catalog.API.csproj
│   │   │       └── Program.cs
│   │   ├── cart/           (same structure)
│   │   ├── checkout/       (same structure)
│   │   ├── orders/         (same structure)
│   │   ├── identity/       (same structure)
│   │   ├── payments/       (same structure)
│   │   ├── fulfillment/    (same structure)
│   │   └── notifications/  (same structure)
│   └── ecommerce.AppHost/
│       ├── ecommerce.AppHost.sln
│       ├── ecommerce.AppHost.csproj      ← Sdk="Aspire.AppHost.Sdk/13.4.0"
│       └── Program.cs                   ← 8 AddProject<T>() + infra + AddDockerComposeEnvironment
├── docs/
│   └── adr/
│       ├── 0001-use-madr-format.md
│       ├── 0002-azure-service-bus.md
│       ├── 0003-database-per-service.md
│       ├── 0004-yarp-api-gateway.md
│       ├── 0005-saga-orchestration.md
│       ├── 0006-masstransit-outbox-inbox.md
│       ├── 0007-asb-topic-per-context.md
│       └── 0008-mono-repo-multi-solution.md
├── .github/
│   └── workflows/
│       └── ci.yml                       ← 10-solution matrix, fail-fast: false
└── docker-compose.yml                   ← generated by aspire publish (do not hand-author)
```

---

## Service Inventory

| Service | Project name | Aspire resource name | References |
|---------|-------------|---------------------|------------|
| Catalog | `ECommerce.Catalog.API` | `catalog` | postgres, serviceBus |
| Cart | `ECommerce.Cart.API` | `cart` | postgres, redis, serviceBus |
| Checkout | `ECommerce.Checkout.API` | `checkout` | postgres, serviceBus |
| Orders | `ECommerce.Orders.API` | `orders` | postgres, serviceBus |
| Identity | `ECommerce.Identity.API` | `identity` | postgres |
| Payments | `ECommerce.Payments.API` | `payments` | serviceBus |
| Fulfillment | `ECommerce.Fulfillment.API` | `fulfillment` | serviceBus |
| Notifications | `ECommerce.Notifications.API` | `notifications` | serviceBus |

Aspire type name derivation: dots → underscores. `ECommerce.Catalog.API` → `Projects.ECommerce_Catalog_API`.

---

## Contracts Shape (locked)

```
IMessageEnvelope (interface, ECommerce.Contracts namespace)
├── Guid MessageId { get; }
├── Guid CorrelationId { get; }
├── Guid CausationId { get; }
└── DateTimeOffset OccurredAt { get; }

Placeholder records per namespace (positional parameters → { get; init; } satisfies interface):
  ECommerce.Catalog.Events.V1.CatalogServiceReady
  ECommerce.Cart.Events.V1.CartServiceReady
  ECommerce.Checkout.Events.V1.CheckoutServiceReady
  ECommerce.Orders.Events.V1.OrdersServiceReady
  ECommerce.Identity.Events.V1.IdentityServiceReady
  ECommerce.Payments.Events.V1.PaymentsServiceReady
  ECommerce.Fulfillment.Events.V1.FulfillmentServiceReady
  ECommerce.Notifications.Events.V1.NotificationsServiceReady
```

Phase 2+ will replace placeholders with real domain events and commands. Namespace convention `ECommerce.{ServiceName}.Events.V1` is permanently locked.

---

## Key Constraints for Future Phases

- **MassTransit version:** Pin to 8.3.6 in every csproj that references it. v9.x requires a Massient commercial license. ADR-0006 documents this decision. v8 receives security patches through end of 2026.
- **Contracts purity:** `Contracts.csproj` must never gain a `<PackageReference>` or `<FrameworkReference>`. Violations are caught in PR review.
- **docker-compose.yml:** Always regenerated via `aspire publish -o ./`. Never hand-authored — divergence is silent and difficult to detect.
- **ASB emulator sessions:** The emulator does not support sessions (`RequiresSession: true` is unsupported). Phase 4 saga design must avoid session-based patterns.
- **MassTransit + emulator (Phase 2 concern):** `EmulatorHost()` does not exist in MT 8.3.6 (added in v9). Phase 2 must test AMQP connectivity via Aspire-injected connection string before building all 8 service configs.
- **Outbox/inbox:** Must be wired from Phase 2 (the first phase that adds MassTransit consumers). Cannot be retrofitted later without data migration.

---

*Walking Skeleton created: 2026-06-03*
*Phase: 01-foundations*
