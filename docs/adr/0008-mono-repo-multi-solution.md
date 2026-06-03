---
status: accepted
date: 2026-06-03
decision-makers: [Fanji Ari Mukti]
consulted: []
informed: []
---

# Mono-Repo with Independent Per-Service Solution Files

## Context and Problem Statement

Eight services share a Contracts building block and infrastructure (Aspire AppHost, Terraform). The repository structure must allow each service team to work independently in Visual Studio without loading the entire solution, while keeping shared code visible and cross-cutting changes atomic in git history.

## Decision Drivers

- Each service must be independently openable in Visual Studio (own `.sln` file)
- Contracts must be referenced via `ProjectReference` (not NuGet), so changes are immediately visible without publishing a package
- Cross-cutting changes (e.g., adding a new contract) must be possible in a single commit
- CI must verify each service builds independently

## Considered Options

- Mono-repo with one root solution (all projects in one `.sln`)
- Poly-repo (8 separate git repositories)
- Mono-repo with per-service solution files (chosen)

## Decision Outcome

Chosen: **Mono-repo with per-service solution files** — one git repository, but each service has its own `.sln` that includes the service project and Contracts. Contracts is referenced via relative `ProjectReference`, making changes to shared contracts immediately visible in each service solution without a publish step.

### Consequences

- Good: `git log` and `git blame` span all services — cross-cutting changes are atomic and visible.
- Good: Each service `.sln` is independently openable in Visual Studio or Rider.
- Good: Contracts changes propagate immediately — no NuGet publish/restore cycle needed during development.
- Bad: `dotnet build` at the repo root does not work — there is no root-level `.sln`. CI must enumerate each solution path explicitly (handled by the 10-solution matrix in `.github/workflows/ci.yml`).
- Bad: A developer unfamiliar with the structure may expect `dotnet build .` to work at the root — it does not.

## Pros and Cons of the Options

### Mono-Repo with Per-Service Solutions
- Pro: Independent Visual Studio experience per service
- Pro: Atomic cross-cutting commits
- Pro: Contracts via ProjectReference (no publish step)
- Con: No root-level build; CI must enumerate solutions

### Mono-Repo with One Root Solution
- Pro: Single `dotnet build` at root
- Con: Visual Studio loads all 8 services simultaneously — slow, high memory usage
- Con: Teams working on one service are distracted by all other services' build output

### Poly-Repo
- Pro: Complete isolation per service; independent release cycles
- Con: Contracts must be published as a NuGet package; version pinning becomes coordination overhead
- Con: Cross-cutting changes (e.g., interface changes) require PRs across 8 repos simultaneously
- Con: git history is fragmented — no single view of system evolution

## More Information

- CI solution enumeration: `.github/workflows/ci.yml` matrix contains all 10 solution paths (Contracts + AppHost + 8 services). See ADR-0001 for the ADR format used throughout.
- Solution file convention: `{ServiceName}.sln` at `src/services/{service}/`, containing both the service project and `Contracts.csproj`.
