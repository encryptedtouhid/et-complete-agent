# ADR 0003 — Source generators for logging and JSON

- Status: accepted
- Date: 2026-05-22

## Context

The agent log path runs on every request — at least once for redaction and
once for usage. JSON serialisation runs on every endpoint response. Both have
hot loops where reflection-based defaults pay a non-trivial cost on a process
that's also calling out to an LLM (i.e. latency budget is tight).

Two analyzer rules also fire on the non-generated path:

- `CA1873` — log argument may be expensive if the level is disabled
- `CA1848` — prefer LoggerMessage source generator

Trim and AOT make this worse: reflection-based `System.Text.Json` chokes
without `JsonSerializerContext`.

## Decision

- All `ILogger` calls use partial methods with `[LoggerMessage]`.
- All endpoint DTOs are listed on `AgentJsonContext : JsonSerializerContext`
  and registered via `ConfigureHttpJsonOptions`.
- The JSON context naming policy is camelCase to match common JS clients.

## Consequences

**Good**

- Zero allocation on disabled-level log calls.
- AOT-friendly publish path is reachable without rewriting any endpoint code.
- Both `CA1873` and `CA1848` pass without suppression.

**Trade-offs**

- A new DTO needs an entry in `AgentJsonContext`. Forgetting it surfaces at
  serialisation time as a clear "type not in context" exception.
- `[LoggerMessage]` partials require the containing class to be `partial`,
  which we accept project-wide.

## Status

Holding. Cost is one-line declarations; benefit is real on every request.
