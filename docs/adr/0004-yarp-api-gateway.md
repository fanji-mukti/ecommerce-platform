---
status: accepted
date: 2026-06-03
decision-makers: [Fanji Ari Mukti]
consulted: []
informed: []
---

# Use YARP as the API Gateway

## Context and Problem Statement

The Angular SPA and external clients need a single entry point to reach 8 independently-deployed services. Without a gateway, the frontend must know the URL of each service, CORS must be configured per-service, and auth token validation is duplicated. A reverse proxy centralises routing, auth enforcement, and cross-cutting concerns.

## Decision Drivers

- Must be .NET-native (no Java or Go processes in the stack)
- Must support dynamic route configuration via appsettings (no code recompilation to add routes)
- Must be actively maintained with a clear support trajectory
- Must support JWT pass-through or validation for auth delegation

## Considered Options

- YARP (Yet Another Reverse Proxy) by Microsoft
- Ocelot
- Azure API Management (APIM)
- No gateway (Angular calls services directly)

## Decision Outcome

Chosen: **YARP 2.x** — actively maintained by Microsoft, integrates naturally as an ASP.NET Core middleware, configures routes via appsettings without recompilation, and supports transforms and auth middleware. Ocelot is in maintenance mode. APIM adds cloud cost and complexity without portfolio learning value.

### Consequences

- Good: Single CORS configuration point; single JWT validation point.
- Good: Route changes require only appsettings edits, not redeployment of gateway code.
- Bad: YARP adds another service to operate (implemented in Phase 2).
- Neutral: YARP is implemented in Phase 2; this ADR locks the choice now to prevent services from exposing direct public endpoints.

## Pros and Cons of the Options

### YARP
- Pro: Microsoft-maintained, actively developed, .NET-native
- Pro: Declarative routing via appsettings
- Pro: Full ASP.NET Core middleware pipeline available for auth, rate limiting, etc.
- Con: Requires a dedicated service process

### Ocelot
- Pro: Established in the .NET community
- Con: In maintenance mode as of 2024 — no new features, limited PR activity
- Con: Less flexible than YARP for complex transforms

### Azure API Management
- Pro: Fully managed, rich policy engine
- Con: High cost at scale; overkill for a portfolio project
- Con: Obscures the gateway mechanics — portfolio loses the "I built it" narrative

### No Gateway
- Pro: Fewer moving parts
- Con: CORS must be configured on each of 8 services
- Con: Angular must know 8 service URLs — couples the SPA to infra topology
- Con: JWT validation duplicated across services

## More Information

- YARP implemented in Phase 2 alongside the Identity service.
- YARP documentation: https://microsoft.github.io/reverse-proxy/
