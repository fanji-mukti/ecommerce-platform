---
status: accepted
date: 2026-06-03
decision-makers: [Fanji Ari Mukti]
consulted: []
informed: []
---

# Use MADR 4.0 for Architectural Decision Records

## Context and Problem Statement

The project needs a lightweight, reviewable, file-based format for recording architectural decisions that integrates naturally with git. Without a standard format, decisions get lost in chat history or commit messages, making it impossible to understand why technology choices were made when revisiting the codebase months later.

## Decision Drivers

- Must be text-based and git-friendly (diffable, reviewable in PRs)
- Should provide consistent structure so readers know where to look
- Must not require external tooling to read — plain markdown in any editor
- Should support tooling if needed (frontmatter enables future automation)
- Low ceremony: writing an ADR should take minutes, not hours

## Considered Options

- MADR 4.0 (Markdown Architectural Decision Records)
- Plain markdown (freeform, no required structure)
- RFC-style (heavyweight, numbered sections, long prose)
- Log4Brains (dedicated ADR toolchain with web UI)

## Decision Outcome

Chosen: **MADR 4.0** — it provides structured frontmatter (enabling future tooling), requires all the right sections (context, options, outcome, consequences), and is simple enough to write without a template wizard. The 7-section structure ensures every decision captures the "why not the alternatives" reasoning that makes ADRs actually useful.

### Consequences

- Good: All ADRs follow the same structure, making them scannable.
- Good: YAML frontmatter `status` field supports lifecycle tracking (proposed → accepted → deprecated).
- Bad: Writers must remember to fill in all 7 sections; partial ADRs are worse than none.
- Neutral: ~26 ADRs planned across 6 phases — MADR's lightweight format scales to this count without becoming a maintenance burden.

## Pros and Cons of the Options

### MADR 4.0
- Pro: Structured without being heavyweight
- Pro: Widely adopted in the .NET / Java OSS community
- Pro: YAML frontmatter enables tooling (status queries, link checking)
- Con: Requires discipline to fill all 7 sections

### Plain Markdown (Freeform)
- Pro: Zero learning curve
- Con: No consistent structure; different authors produce incomparable documents
- Con: Status and dates are buried in prose, not queryable

### RFC-Style
- Pro: Very thorough
- Con: Disproportionate overhead for a portfolio project
- Con: Long prose discourages writing ADRs at all

### Log4Brains
- Pro: Web UI for browsing decisions
- Con: Requires running a separate tool; adds a dependency
- Con: Overkill for a single-team portfolio project

## More Information

- MADR specification: https://adr.github.io/madr/
- This ADR establishes the format used by ADRs 0002–0008 and all future decisions in the project (~26 total by Phase 6).
