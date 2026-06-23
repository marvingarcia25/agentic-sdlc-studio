# GoldenPath — AI-Driven Dev Lifecycle Showcase (Design)

**Date:** 2026-06-23
**Status:** Approved (design), pending implementation plan

## Purpose

An interactive, scripted walkthrough that demonstrates what a *mature, AI-driven
enterprise development lifecycle* looks like inside a Microsoft-heavy, highly
secured organization. It follows **one concrete feature** — "add rate-limiting to
an HTTP-triggered Azure Function" — through every stage of the lifecycle, so
engineers can see what it *feels like* to work this way.

The recurring theme across every stage: **the human is always the approver and
steerer; agents do the toil; everything is auditable.**

## Audience

Engineering peers. Optimized for "show me the actual workflow and developer
experience," not for convincing leadership. Tone is concrete and credible —
artifacts look like real C#/Azure/AKS work product.

## Key Decisions

| Decision | Choice |
| --- | --- |
| Form | Interactive showcase / walkthrough |
| Structure | Single feature's journey (narrative spine), linear |
| Fidelity | Fully scripted / simulated — nothing actually runs |
| Showcase app stack | **Blazor Server** (single ASP.NET Core .NET app) |
| Depicted content stack | C# / Azure Functions, GitHub Actions, Docker, AKS |
| Story | Add rate-limiting to an HTTP-triggered Azure Function |
| Data source | Strongly-typed in-memory C# seed data (no DB, no external API) |
| Repo scope | Git repo lives in `day29_GoldenPath/` only — never the `1000` root |

## Architecture

A single **Blazor Server** app. Scripted journey content lives in strongly-typed
C# `record` seed data loaded by a `JourneyService`. Each lifecycle stage is a
Blazor component. A `JourneyState` service tracks the current stage + step and
drives navigation and transition animation. No real agents, pipelines, or deploys
run — content is pre-baked but realistic.

### Components / units

- **`JourneyService`** — owns the scripted journey definition (stages, steps,
  agent transcripts, artifacts). Single source of truth for content.
- **`JourneyState`** — scoped service tracking current position; exposes
  `Advance()` / `GoTo(stage)` and notifies components of changes.
- **Stage components** — one per lifecycle stage, each rendering: acting agent(s),
  scripted agent transcript, the produced artifact, and a human approval gate.
- **Progress rail** — persistent UI showing all 7 stages and current position.
- **Artifact viewer** — syntax-highlighted display of code/YAML/manifests
  (display only; not compiled or executed).

## The 7 Stages

1. **Intake / Plan** — ticket arrives; a *Planning agent* produces a spec + task
   breakdown; human approves the plan.
2. **Build** — *Implementation agent(s)* write the C# rate-limiting change against
   the spec; developer steers and reviews. Shows human-in-the-loop steering DX.
3. **Test** — a *QA/Test agent* writes and runs xUnit tests; coverage + results
   shown.
4. **Review** — a *Code-review agent* and a *Security-review agent* comment on the
   diff; human approves the merge.
5. **Pipeline / CI** — a GitHub Actions run animates through build / lint / scan /
   gates with pass/fail.
6. **Deploy** — containerization (Dockerfile → image) → Helm/k8s manifests → **AKS**
   rollout (canary → prod) with an approval gate.
7. **Operate** — App Insights metrics; a latency alert flows back into a new
   ticket, closing the loop.

## Depicted Artifacts (credibility layer)

Authentic-looking, hand-authored, display-only:

- C# rate-limiting diff on an HTTP-triggered Azure Function
- An xUnit test file
- A `.github/workflows/*.yml` GitHub Actions pipeline
- A `Dockerfile`
- Kubernetes / Helm manifests
- A canary rollout visualization (canary → prod)

These are syntax-highlighted display content, not compiled or executed.

## The Loop

The Operate stage surfaces a latency alert from App Insights, spawns a new ticket,
and visually points back to Intake — demonstrating the lifecycle is a continuous
cycle, not a one-way pipeline.

## Dogfooding (stretch, optional)

The repo includes its own `Dockerfile` + a minimal k8s manifest so the showcase
itself *could* run on AKS — "it practices what it preaches." Built only if time
allows; not required for the core experience.

## Out of Scope (YAGNI)

- No real agent invocation
- No real CI execution
- No real Azure resources or deployment
- No authentication
- No database or persistence beyond in-memory session state

## Logistics

- The git repo is initialized **inside `day29_GoldenPath/`** with its own
  `.gitignore`, never at the `1000` root — fixing the prior "repo swallowed the
  root folder" problem.
- To root the Claude Code workspace here, relaunch Claude Code from inside
  `day29_GoldenPath/` (`cd day29_GoldenPath && claude`); the session working
  directory cannot be changed mid-session.
