# ADR 0001 — Strict Clean Architecture layering

- Status: accepted
- Date: 2026-05-22

## Context

The template ships SDK code that talks to LLM providers, vector stores,
content-safety services, identity providers, and persistence backends. Without
discipline, that turns into a 5,000-line Program.cs and every layer-crossing
test needs HttpClient mocks.

## Decision

Four projects, strict inward-pointing dependencies:

```
Domain ← Application ← Infrastructure ← Host
```

- **Domain** has no project references and no NuGet references beyond the BCL.
  Contracts, records, value objects.
- **Application** references Domain plus `Microsoft.Extensions.AI` abstractions.
  Use-case orchestration (`AgentRunner`), tool definitions, retry policy,
  prompt loader, moderation contract, retrieval contract, usage tracker
  contract.
- **Infrastructure** references Application. All SDK types live here:
  `OpenAI.Chat.ChatClient`, `Azure.AI.OpenAI.AzureOpenAIClient`,
  `Qdrant.Client`, `EntityFrameworkCore`, `Azure.AI.ContentSafety`.
- **Host** is the composition root. HTTP endpoints, auth middleware, rate
  limiting, telemetry wiring, exception handler, health checks, the Scalar
  UI configuration.

## Consequences

**Good**

- Swapping an LLM provider, vector store, or moderation service touches only
  Infrastructure.
- Tests in `Application.Tests` mock the contracts in their own layer; no SDK
  state to set up.
- New transport (gRPC, message-queue worker) can replace Host without
  rewriting the core.

**Trade-offs**

- A `IChatAgentFactory` abstraction was needed because the
  `Microsoft.Agents.AI.AsAIAgent` extension lives on the OpenAI `ChatClient`,
  not on `IChatClient`. Application would otherwise reach into Infrastructure
  types to call it.
- `Application` ends up with a small amount of duplicate enum-like config
  (e.g. `ConversationStoreKind`) that Infrastructure also references — better
  than letting Application reach into Infrastructure for those constants.

## Status today

Holding. No documented case where the boundary has felt wrong in implementation.
