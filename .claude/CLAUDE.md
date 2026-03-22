# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Overview

This is the official FeatureHub .NET SDK (`FeatureHub.SDK` on NuGet), a feature flags management and A/B testing SDK targeting `netstandard2.0`.

## Build, Test, and Pack

Prefer `make` targets over raw `dotnet` commands:

```bash
make            # restore → build → test (everything)
make build      # restore + build solution
make test       # test solution
make format     # reformat all files in-place
make format-check  # dry-run format check (used in CI)

make build-sdk  # build FeatureHubSDK only
make build-otel # build FeatureHubUsageOpenTelemetry only
make build-yaml # build FeatureHubInterceptorYaml only
make test-sdk   # run FeatureHubTest only
make test-otel  # run FeatureHubUsageOpenTelemetryTest only
make test-yaml  # run FeatureHubInterceptorYamlTest only

make pack PACKAGE_VERSION=3.0.0  # pack NuGet
```

Raw `dotnet` equivalents (for reference):

```bash
dotnet restore
dotnet build
dotnet test

# Run a single test class
dotnet test --filter "ClassName=FeatureHubRepositoryTest"

# Run a single test method
dotnet test --filter "FullyQualifiedName~TestMethodName"

# Pack the NuGet package
cd FeatureHubSDK && dotnet pack /p:PackageVersion=3.0.0 --configuration Release
```

## Formatting and Linting

An `.editorconfig` defines code style (2-space indent, brace style, `var` preferences, etc.).
`Directory.Build.props` enables the built-in .NET analyzers (`EnableNETAnalyzers`, `AnalysisLevel=latest-recommended`) across the whole solution, and promotes warnings to errors in Release builds.

Generated code under `FeatureHubSDK/src/` is excluded from both formatting and analyzer rules.

CI runs `dotnet format FeatureHubSDK.sln --verify-no-changes` before the build. Run `make format` locally before pushing.

## After Making Code Changes

After every set of code edits, always run both steps before considering the task done:

```bash
make test          # or make test-sdk / make test-otel for targeted runs
make format-check  # verifies no formatting violations were introduced
```

If `format-check` reports violations, run `make format` to auto-fix them, then re-run `make format-check` to confirm clean. The programmatic Edit tool does not apply Roslyn formatting rules (e.g. `csharp_preserve_single_line_statements = false`), so violations are common when editing without this step.

## Architecture

### Two Evaluation Modes

The SDK supports two API key types that determine where rollout strategies are evaluated:

1. **Server-evaluated** (API key without `*`) — Context is sent as an `x-featurehub` header; the server returns only the feature values matching that context. Uses `ServerEvalFeatureContext`.
2. **Client-evaluated** (API key contains `*`) — All features are always received; strategies are applied locally. Uses `ClientEvalFeatureContext`.

### Two Connection Strategies

- **EventSource (SSE, default)** — Real-time updates via `StreamingEdgeService.cs` using `LaunchDarkly.EventSource`.
- **Polling** — Lazy fetch on access via `PollingEdgeService.cs`; uses ETag for conditional GETs.

### Core Components

| File | Purpose |
|------|---------|
| `FeatureHub.cs` | `Readiness` enum |
| `IFeature.cs` | `IFeature` — the per-feature read API |
| `IFeatureHubRepository.cs` | `IFeatureHubRepository`, `IFeatureRepositoryContext` interfaces |
| `FeatureHubRepository.cs` | `FeatureHubRepository` — ConcurrentDictionary-backed implementation |
| `FeatureStateBaseHolder.cs` | `FeatureStateBaseHolder` — wraps a `FeatureState` and evaluates strategies |
| `EdgeFeatureHubConfig.cs` | `IFeatureHubConfig` / `EdgeFeatureHubConfig` — reads `FEATUREHUB_EDGE_URL`, `FEATUREHUB_API_KEY`, `FEATUREHUB_POLL_TIMEOUT` env vars |
| `ClientContext.cs` | `IClientContext` builder pattern; `BaseClientContext`, `ServerEvalFeatureContext`, `ClientEvalFeatureContext` |
| `StreamingEdgeService.cs` | SSE connection handler; also hosts `FeatureLogging` static event-based logger |
| `PollingEdgeService.cs` | Polling-based feature fetching |
| `StrategyMatchers.cs` | `ApplyFeature` strategy evaluator, `PercentageMurmur3Calculator`, matcher implementations |
| `FeatureValueInterceptor.cs` | `IFeatureValueInterceptor` — intercept and override feature values |
| `LocalYamlValueInterceptor.cs` | YAML-file-based interceptor implementation |
| `Usage.cs` | Usage/analytics event model — `IUsageEvent`, `UsagePlugin`, `UsageAdapter`, `IUsageProvider` and default implementations |

### Usage / Analytics

`Usage.cs` defines the analytics pipeline:

- **`IUsageEvent`** / **`IUsageEventWithFeature`** / **`IUsageFeaturesCollection`** — event interfaces
- **`IUsageEvent.CollectUsageRecord()`** — returns a flat `IReadOnlyDictionary<string, object?>` snapshot of the event for plugins to consume
- **`UsagePlugin`** — base class for analytics plugins; override `Send(IUsageEvent)`
- **`UsageAdapter`** — wires the repository's usage stream to registered plugins
- **`IUsageProvider`** / **`BaseUsageProvider`** — factory for constructing event objects; replace `DefaultUsageProvider.Instance` to customise

### OpenTelemetry Plugin (`FeatureHubUsageOpenTelemetry`)

A separate project (targets `net8.0` and `net10.0`) that ships two plugins:

- **`OpenTelemetryTrackerUsagePlugin`** — writes evaluated feature values to the current OTel span as span attributes or span events
- **`OpenTelemetryUsagePlugin`** — propagates feature values into OTel Baggage under the `fhub` key (pairs with `OpenTelemetryFeatureValueInterceptor`)

### Local YAML Interceptor (`FeatureHubInterceptorYaml`)

A separate project (targets `netstandard2.0`, NuGet ID `FeatureHub.LocalYamlInterceptor`) containing:

- **`LocalYamlValueInterceptor`** — YAML-file-based feature value interceptor; reads `flagValues` map from a YAML file at construction time

### Generated Code

`FeatureHubSDK/src/IO.FeatureHub.SSE/` is OpenAPI-generated and contains:
- `Model/` — 18 model classes (e.g. `FeatureState`, `FeatureRolloutStrategy`, `SSEResultState`)
- `Api/` — REST API clients
- `Client/` — OpenAPI framework boilerplate

Do not manually edit generated files under `src/IO.FeatureHub.SSE/`.

### Logging

Logging uses static event handlers — consumers subscribe to receive log output:

```csharp
FeatureLogging.InfoLogger += (sender, msg) => Console.WriteLine(msg);
FeatureLogging.ErrorLogger += (sender, msg) => Console.Error.WriteLine(msg);
```

### Tests

- `FeatureHubTest/` — core SDK tests, NUnit 4 + Moq, targets `net10.0`
- `FeatureHubUsageOpenTelemetryTest/` — OTel plugin tests, NUnit 4 + Moq, targets `net8.0` and `net10.0`

## Project Structure

```
FeatureHubSDK/                      # Core SDK (netstandard2.0) — published to NuGet
FeatureHubTest/                     # NUnit tests for the core SDK (net10.0)
FeatureHubUsageOpenTelemetry/       # OpenTelemetry analytics plugin (net8.0, net10.0)
FeatureHubUsageOpenTelemetryTest/   # NUnit tests for the OTel plugin (net8.0, net10.0)
FeatureHubInterceptorYaml/          # Local YAML interceptor (netstandard2.0) — published to NuGet as FeatureHub.LocalYamlInterceptor
FeatureHubInterceptorYamlTest/      # NUnit tests for the YAML interceptor (net10.0)
ConsoleAppExample/                  # Console usage example
ToDoWebApi/                         # ASP.NET Core 8 usage example
```

## CI/CD

- **build-test.yaml** — runs on PRs to master; checks formatting, then tests against .NET 8, 9, and 10
- **build-publish.yaml** — runs on git tags; publishes to NuGet using OIDC authentication