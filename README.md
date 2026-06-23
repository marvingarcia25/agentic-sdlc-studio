# Agentic SDLC Studio

Agentic SDLC Studio is an interactive, scripted walkthrough of what mature,
agent-driven enterprise software delivery looks like in practice. It follows a
single feature — **adding rate limiting to the public `GetQuote` Azure
Function** — through seven stages of the development lifecycle:

1. **Intake & Plan** — a ticket becomes an approved spec.
2. **Build** — agents write the code; a human steers.
3. **Test** — tests are written, run, and reported.
4. **Review** — code and security agents review; a human merges.
5. **Pipeline (CI)** — build, test, scan, and gate, automatically.
6. **Deploy** — a canary rollout, promoted by human approval.
7. **Operate** — production telemetry closes the loop, feeding the next cycle.

Each stage shows the agents involved, a transcript of the (scripted) agent/human
conversation, and the real artifacts that conversation would produce — specs,
diffs, tests, review notes, pipeline output, and manifests.

## What this is — and isn't

This is a **Blazor Server** app. All content is hand-authored and
display-only: no agents run, no pipelines execute, and nothing deploys. It's a
guided narrative, not a live system, built to demonstrate what a healthy
human-in-the-loop agentic SDLC looks like end to end.

## Running locally

Requires the [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0).

```bash
dotnet run
```

The app starts on the URL printed in the console (see `Properties/launchSettings.json`).

## Dogfooding its own pipeline

This repo practices what the walkthrough preaches — it ships with its own
real CI/CD assets:

- **`Dockerfile`** — multi-stage build that publishes and runs this app.
- **`.github/workflows/ci.yml`** — GitHub Actions workflow that builds the
  project on every push/PR to `main`.
- **`k8s/deployment.yaml`** — a Deployment + Service manifest for running the
  container in Kubernetes.

## Project structure

- `Models/Journey.cs` — domain model for stages, transcripts, and artifacts.
- `Services/JourneyService.cs` — the hand-authored content for all 7 stages.
- `Services/JourneyState.cs` — tracks the current stage and drives navigation.
- `Components/Layout/MainLayout.razor` — header bar + progress rail shell.
- `Components/Parts/ProgressRail.razor` — vertical stepper for the 7 stages.
- `Components/Parts/TranscriptView.razor` — chat-style agent/human/system transcript.
- `Components/Parts/ArtifactViewer.razor` — tabbed viewer for stage artifacts.
- `Components/Pages/Home.razor` — renders the current stage.
