# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Overview

This is the official FeatureHub .NET SDK (`FeatureHub.SDK` on NuGet), a feature flags management and A/B testing SDK targeting `netstandard2.0`.

## Build, Test, and Pack

```bash
# Restore dependencies
dotnet restore

# Build
dotnet build

# Run all tests
dotnet test

# Run a single test class
dotnet test --filter "ClassName=FeatureHubRepositoryTest"

# Run a single test method
dotnet test --filter "FullyQualifiedName~TestMethodName"

# Pack the NuGet package (version required for CI, optional locally)
cd FeatureHubSDK && dotnet pack /p:PackageVersion=3.0.0 --configuration Release
```

## Architecture

### Two Evaluation Modes

The SDK supports two API key types that determine where rollout strategies are evaluated:

1. **Server-evaluated** (API key without `*`) — Context is sent as an `x-featurehub` header; the server returns only the feature values matching that context. Uses `ServerEvalFeatureContext`.
2. **Client-evaluated** (API key contains `*`) — All features are always received; strategies are applied locally. Uses `ClientEvalFeatureContext`.

### Two Connection Strategies

- **EventSource (SSE, default)** — Real-time updates via `EventServiceListener.cs` using `LaunchDarkly.EventSource`.
- **Polling** — Lazy fetch on access via `EdgeClientPoll` in `Polling.cs`; uses ETag for conditional GETs.

### Core Components

| File | Purpose |
|------|---------|
| `FeatureHub.cs` | `IFeatureHubRepository`, `FeatureHubRepository` (ConcurrentDictionary storage), `FeatureStateBaseHolder`, `Readyness` enum |
| `EdgeFeatureHubConfig.cs` | `IFeatureHubConfig` / `EdgeFeatureHubConfig` — reads `FEATUREHUB_EDGE_URL`, `FEATUREHUB_API_KEY`, `FEATUREHUB_POLL_TIMEOUT` env vars |
| `ClientContext.cs` | `IClientContext` builder pattern; `BaseClientContext`, `ServerEvalFeatureContext`, `ClientEvalFeatureContext` |
| `EventServiceListener.cs` | SSE connection handler; also hosts `FeatureLogging` static event-based logger |
| `Polling.cs` | Polling-based feature fetching |
| `StrategyMatchers.cs` | `ApplyFeature` strategy evaluator, `PercentageMurmur3Calculator`, matcher implementations |

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

Tests live in `FeatureHubTest/` using **NUnit 4** and **Moq**. Key test files map directly to SDK components (e.g. `StrategyMatchersTest.cs`, `PollingTest.cs`, `ContextTest.cs`).

## Project Structure

```
FeatureHubSDK/          # Core SDK (netstandard2.0) — published to NuGet
FeatureHubTest/         # NUnit tests (net8.0)
ConsoleAppExample/      # Console usage example
ToDoWebApi/             # ASP.NET Core 8 usage example
```

## CI/CD

- **build-test.yaml** — runs on PRs to master; tests against .NET 8, 9, and 10
- **build-publish.yaml** — runs on git tags; publishes to NuGet using OIDC authentication
