using AgenticSdlcStudio.Models;

namespace AgenticSdlcStudio.Services;

/// <summary>
/// Single source of truth for the scripted walkthrough. Everything here is
/// hand-authored, realistic, and display-only — no agents, pipelines, or
/// deployments actually run.
/// </summary>
public class JourneyService
{
    public string FeatureTitle => "RL-482 · Add rate limiting to the public GetQuote Azure Function";

    public IReadOnlyList<Stage> Stages { get; } = Build();

    private static IReadOnlyList<Stage> Build() =>
    [
        // 1 ─────────────────────────────────────────────────────────────────
        new Stage(
            Key: "intake",
            Name: "Intake & Plan",
            Icon: "🗂️",
            Tagline: "A ticket becomes an approved spec.",
            Agents: ["Planning agent"],
            Narrative:
                "A ticket lands from on-call. The Planning agent reads it, pulls context " +
                "from the repo and the incident, and proposes a spec broken into tasks. " +
                "Nothing proceeds until a human approves the plan — the agent drafts, you decide.",
            Transcript:
            [
                new("On-call (PagerDuty)", "system",
                    "RL-482: The public GetQuote function is being hammered by a single client (~4k req/min). " +
                    "p95 latency is up 6x and downstream pricing service is throttling us. We need rate limiting."),
                new("Planning agent", "agent",
                    "Read the ticket + the GetQuote function + last week's incident timeline. Root cause: no per-client " +
                    "throttling on an anonymous-but-keyed endpoint. Proposing a sliding-window limiter keyed by API key, " +
                    "returning 429 + Retry-After. Drafted a spec and 4 tasks."),
                new("You", "human",
                    "Good. Key by API key, not IP — clients sit behind shared NATs. And make the limit configurable per tier."),
                new("Planning agent", "agent",
                    "Updated: key = API key; limits read from config per subscription tier (free/std/premium). " +
                    "Spec + tasks ready for your approval."),
            ],
            Artifacts:
            [
                new("Approved spec", "plan.md", ArtifactKind.Markdown,
                    """
                    # RL-482 — Rate limit GetQuote

                    ## Problem
                    The public `GetQuote` HTTP function has no throttling. A single API key can
                    saturate the downstream pricing service, degrading latency for everyone.

                    ## Approach
                    Use the built-in .NET rate limiter (`System.Threading.RateLimiting`) with a
                    **sliding window** partitioned by **API key**. Limits are configurable per
                    subscription tier. Over-limit requests get `429 Too Many Requests` + `Retry-After`.

                    ## Tasks
                    1. Add rate-limiter middleware, partitioned by `x-api-key`.
                    2. Read per-tier limits from configuration.
                    3. Return 429 + `Retry-After`; emit a metric on rejection.
                    4. xUnit tests: under-limit passes, over-limit gets 429, window resets.

                    ## Non-goals
                    Global/IP throttling, distributed limiter state (single-region is fine for now).
                    """),
            ],
            ApprovalPrompt: "Approve the plan and start the build"),

        // 2 ─────────────────────────────────────────────────────────────────
        new Stage(
            Key: "build",
            Name: "Build",
            Icon: "🛠️",
            Tagline: "Agents write the code; you steer.",
            Agents: ["Implementation agent"],
            Narrative:
                "The Implementation agent works task-by-task against the approved spec. You stay in the " +
                "loop — reading the diff, steering decisions, and correcting course. This is the core " +
                "developer experience: you direct, the agent does the toil, and every change is reviewable.",
            Transcript:
            [
                new("Implementation agent", "agent",
                    "Task 1–3: registered a partitioned sliding-window limiter keyed by `x-api-key`, wired " +
                    "per-tier limits from config, and added the 429 + Retry-After response. Diff below."),
                new("You", "human",
                    "Window of 60s with 4 segments is fine. One thing — reject with a structured problem+json " +
                    "body, not a bare 429, so clients get a machine-readable reason."),
                new("Implementation agent", "agent",
                    "Done — rejection now returns `application/problem+json` with the retry window and tier. " +
                    "Updated the diff."),
            ],
            Artifacts:
            [
                new("Limiter registration", "Program.cs", ArtifactKind.Diff,
                    """
                    @@ Program.cs @@
                     var builder = WebApplication.CreateBuilder(args);

                    +builder.Services.AddRateLimiter(options =>
                    +{
                    +    options.AddPolicy("per-api-key", httpContext =>
                    +    {
                    +        var apiKey = httpContext.Request.Headers["x-api-key"].ToString();
                    +        var tier = TierResolver.Resolve(apiKey);          // free | std | premium
                    +        var limit = builder.Configuration.GetValue<int>($"RateLimits:{tier}");
                    +
                    +        return RateLimitPartition.GetSlidingWindowLimiter(apiKey, _ =>
                    +            new SlidingWindowRateLimiterOptions
                    +            {
                    +                PermitLimit = limit,
                    +                Window = TimeSpan.FromSeconds(60),
                    +                SegmentsPerWindow = 4,
                    +                QueueLimit = 0
                    +            });
                    +    });
                    +
                    +    options.OnRejected = async (ctx, ct) =>
                    +    {
                    +        ctx.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
                    +        ctx.HttpContext.Response.Headers.RetryAfter = "60";
                    +        Metrics.RateLimitRejected.Add(1);
                    +        await ctx.HttpContext.Response.WriteAsJsonAsync(
                    +            new ProblemDetails { Status = 429, Title = "Rate limit exceeded",
                    +                                 Detail = "Retry after the window resets." }, ct);
                    +    };
                    +});
                    """),
                new("Endpoint", "GetQuote.cs", ArtifactKind.CSharp,
                    """
                    [Function("GetQuote")]
                    [EnableRateLimiting("per-api-key")]
                    public async Task<HttpResponseData> Run(
                        [HttpTrigger(AuthorizationLevel.Function, "get")] HttpRequestData req)
                    {
                        var symbol = req.Query["symbol"];
                        var quote = await _pricing.GetQuoteAsync(symbol);
                        var res = req.CreateResponse(HttpStatusCode.OK);
                        await res.WriteAsJsonAsync(quote);
                        return res;
                    }
                    """),
            ],
            ApprovalPrompt: "Looks good — send it to test"),

        // 3 ─────────────────────────────────────────────────────────────────
        new Stage(
            Key: "test",
            Name: "Test",
            Icon: "🧪",
            Tagline: "Tests written, run, and green.",
            Agents: ["QA agent"],
            Narrative:
                "The QA agent writes xUnit tests straight from the spec's acceptance criteria, runs them, " +
                "and reports coverage. Tests are the contract — they encode what the feature must do, so the " +
                "rest of the pipeline can trust the change.",
            Transcript:
            [
                new("QA agent", "agent",
                    "Wrote 5 tests from the spec: under-limit passes, over-limit returns 429, Retry-After present, " +
                    "window resets, and limits differ by tier. Running them now…"),
                new("QA agent", "agent",
                    "✓ 5/5 passing in 1.2s. Branch coverage on the limiter path: 94%."),
            ],
            Artifacts:
            [
                new("xUnit tests", "RateLimitTests.cs", ArtifactKind.CSharp,
                    """
                    public class RateLimitTests
                    {
                        [Fact]
                        public async Task Under_limit_requests_succeed()
                        {
                            var client = _factory.WithApiKey("std").CreateClient();
                            for (var i = 0; i < 10; i++)
                                (await client.GetAsync("/api/GetQuote?symbol=MSFT"))
                                    .StatusCode.Should().Be(HttpStatusCode.OK);
                        }

                        [Fact]
                        public async Task Over_limit_returns_429_with_retry_after()
                        {
                            var client = _factory.WithApiKey("free").CreateClient();   // limit: 5/min
                            for (var i = 0; i < 5; i++) await client.GetAsync("/api/GetQuote?symbol=MSFT");

                            var rejected = await client.GetAsync("/api/GetQuote?symbol=MSFT");
                            rejected.StatusCode.Should().Be(HttpStatusCode.TooManyRequests);
                            rejected.Headers.RetryAfter.Should().NotBeNull();
                        }
                    }
                    """),
                new("Test run", "dotnet-test.txt", ArtifactKind.Bash,
                    """
                    $ dotnet test
                    Passed!  - Failed: 0, Passed: 5, Skipped: 0, Duration: 1.2s
                    Coverage: 94% (limiter path)
                    """),
            ],
            ApprovalPrompt: "Tests green — open for review"),

        // 4 ─────────────────────────────────────────────────────────────────
        new Stage(
            Key: "review",
            Name: "Review",
            Icon: "🔍",
            Tagline: "Code & security agents review; you merge.",
            Agents: ["Code-review agent", "Security-review agent"],
            Narrative:
                "Two specialist agents review in parallel — one for correctness and style, one for security. " +
                "They comment on the diff like any reviewer would. The merge itself is a human decision: you " +
                "read the findings and approve. Nothing merges unattended.",
            Transcript:
            [
                new("Code-review agent", "agent",
                    "LGTM overall. One nit: `TierResolver.Resolve` should treat an unknown key as `free`, not throw — " +
                    "fail closed to the strictest limit."),
                new("Security-review agent", "agent",
                    "Checked: 429 body carries no PII, API key is never logged, and the limiter fails closed if config " +
                    "is missing. Confirms the spec's threat model. No blockers."),
                new("You", "human",
                    "Agreed on the unknown-key default. Pushed a one-liner, re-ran tests — still green. Merging."),
            ],
            Artifacts:
            [
                new("Review summary", "review.md", ArtifactKind.Markdown,
                    """
                    # Review — PR #312 (RL-482)

                    ## Code-review agent
                    - ✅ Sliding window + per-tier config matches the spec.
                    - 🟡 `TierResolver.Resolve`: default unknown keys to `free` (fail closed). **Fixed.**

                    ## Security-review agent
                    - ✅ No PII in the 429 response body.
                    - ✅ API key never written to logs or metrics dimensions.
                    - ✅ Missing-config path fails closed to the strictest limit.

                    **Verdict:** approved by *you* after fixes. Merged to `main`.
                    """),
            ],
            ApprovalPrompt: "Approve & merge to main"),

        // 5 ─────────────────────────────────────────────────────────────────
        new Stage(
            Key: "pipeline",
            Name: "Pipeline (CI)",
            Icon: "⚙️",
            Tagline: "Build, test, scan, gate — automatically.",
            Agents: ["GitHub Actions"],
            Narrative:
                "Merge to main triggers the pipeline. Every gate is automated and visible: build, test, " +
                "format check, CodeQL security scan, and a container build. A green pipeline is the price of " +
                "admission to deploy — and it's the same pipeline for everyone, so the path is consistent.",
            Transcript:
            [
                new("GitHub Actions", "system", "Workflow `ci-cd` triggered by merge to `main` (sha 9f3c1ab)."),
                new("GitHub Actions", "system",
                    "✓ build (8.0)   ✓ test (5 passed)   ✓ format   ✓ CodeQL (0 alerts)   ✓ container build & push"),
                new("GitHub Actions", "system", "All gates green. Deploy stage is now unlocked."),
            ],
            Artifacts:
            [
                new("CI/CD workflow", ".github/workflows/ci-cd.yml", ArtifactKind.Yaml,
                    """
                    name: ci-cd
                    on:
                      push:
                        branches: [ main ]
                    jobs:
                      build-test:
                        runs-on: ubuntu-latest
                        steps:
                          - uses: actions/checkout@v4
                          - uses: actions/setup-dotnet@v4
                            with: { dotnet-version: '8.0.x' }
                          - run: dotnet build --configuration Release
                          - run: dotnet test --configuration Release
                          - run: dotnet format --verify-no-changes
                      security-scan:
                        uses: github/codeql-action/.github/workflows/codeql.yml@v3
                      container:
                        needs: [ build-test, security-scan ]
                        runs-on: ubuntu-latest
                        steps:
                          - uses: docker/build-push-action@v6
                            with: { push: true, tags: acr.azurecr.io/getquote:${{ github.sha }} }
                    """),
            ],
            ApprovalPrompt: "Pipeline green — proceed to deploy"),

        // 6 ─────────────────────────────────────────────────────────────────
        new Stage(
            Key: "deploy",
            Name: "Deploy",
            Icon: "🚀",
            Tagline: "Containers to AKS, canary first.",
            Agents: ["Deployment agent"],
            Narrative:
                "The image is rolled out to AKS as a canary — a small slice of traffic first. Health and " +
                "latency are watched automatically; promotion to 100% is gated on a human approval after the " +
                "canary proves healthy. Progressive delivery means a bad change is caught small, not in prod.",
            Transcript:
            [
                new("Deployment agent", "agent",
                    "Deployed `getquote:9f3c1ab` to AKS as a 10% canary. Watching error rate and p95 for 5 min…"),
                new("Deployment agent", "agent",
                    "Canary healthy: error rate 0.0%, p95 within SLO, 429s landing only on the abusive key. " +
                    "Requesting promotion to 100%."),
                new("You", "human", "Canary looks clean. Promote to prod."),
                new("Deployment agent", "agent", "Promoted to 100%. Old ReplicaSet scaled to zero."),
            ],
            Artifacts:
            [
                new("Container image", "Dockerfile", ArtifactKind.Dockerfile,
                    """
                    FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
                    WORKDIR /src
                    COPY . .
                    RUN dotnet publish GetQuote.csproj -c Release -o /app

                    FROM mcr.microsoft.com/dotnet/aspnet:8.0
                    WORKDIR /app
                    COPY --from=build /app .
                    ENTRYPOINT ["dotnet", "GetQuote.dll"]
                    """),
                new("AKS rollout", "k8s/deployment.yaml", ArtifactKind.Yaml,
                    """
                    apiVersion: apps/v1
                    kind: Deployment
                    metadata:
                      name: getquote
                    spec:
                      replicas: 3
                      strategy:
                        canary: { steps: [ { setWeight: 10 }, { pause: { duration: 5m } }, { setWeight: 100 } ] }
                      template:
                        spec:
                          containers:
                            - name: getquote
                              image: acr.azurecr.io/getquote:9f3c1ab
                              readinessProbe: { httpGet: { path: /healthz, port: 80 } }
                    """),
            ],
            ApprovalPrompt: "Promote canary to production"),

        // 7 ─────────────────────────────────────────────────────────────────
        new Stage(
            Key: "operate",
            Name: "Operate",
            Icon: "📈",
            Tagline: "Watch in prod — and close the loop.",
            Agents: ["Observability agent"],
            Narrative:
                "In production, the change is measured against the goal that started it. App Insights confirms " +
                "latency recovered and abuse is contained. Later, a new traffic pattern trips an alert — which " +
                "the agent turns into a fresh ticket, feeding the next loop. The lifecycle is a cycle, not a line.",
            Transcript:
            [
                new("Observability agent", "agent",
                    "Post-deploy (1h): p95 latency back to baseline (−83%), downstream throttling gone, 429s isolated " +
                    "to the one abusive key. RL-482 met its goal."),
                new("App Insights", "system",
                    "⚠ Alert: a different key is now approaching the premium-tier limit during market open."),
                new("Observability agent", "agent",
                    "Opened RL-501: 'Premium limit may be too low for market-open bursts.' Routed to Intake for triage. " +
                    "→ The loop begins again."),
            ],
            Artifacts:
            [
                new("New ticket (loop)", "RL-501.json", ArtifactKind.Json,
                    """
                    {
                      "id": "RL-501",
                      "source": "App Insights alert",
                      "title": "Premium rate limit may be too low for market-open bursts",
                      "evidence": { "key": "premium-tier", "peak_rpm": 980, "limit_rpm": 1000 },
                      "routed_to": "Intake & Plan",
                      "parent": "RL-482"
                    }
                    """),
            ],
            ApprovalPrompt: "Start the next loop (back to Intake)"),
    ];
}
