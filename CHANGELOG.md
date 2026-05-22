# Changelog

All notable changes to this template are documented here. The format follows
[Keep a Changelog](https://keepachangelog.com/en/1.1.0/) and the project follows
[Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Added
- Security headers middleware (HSTS, X-Content-Type-Options, X-Frame-Options,
  Referrer-Policy, Permissions-Policy, Content-Security-Policy). HSTS only
  emits over HTTPS; all settings tunable via `SecurityHeaders` config.
- Configurable CORS named policy `AgentCors`; disabled by default,
  allow-list driven via `Cors:AllowedOrigins`.
- Idempotency keys via `Idempotency-Key` header on `/agent/run`,
  `/agent/classify`, `/agent/workflow/research`. Responses cached per
  (subject, path, key, body-hash) with 10-minute default TTL. Replays carry
  an `Idempotency-Replay: true` header.
- Append-only audit log via `IAuditLog` — `NoOpAuditLog` default; EF Core
  implementation auto-enabled when `Persistence:ConversationStore=Sqlite`.
- Repo-health files: CHANGELOG, SECURITY, CONTRIBUTING, CODE_OF_CONDUCT,
  GitHub issue / PR templates, README badges.
- Architecture decision records (`docs/adr/0001..0003`).
- Operational runbook (`docs/runbook.md`).

## [0.1.0] — 2026-05-23

### Added

#### Agent core
- Clean Architecture solution: `Domain ← Application ← Infrastructure ← Host`.
- LLM provider abstraction supporting Azure OpenAI (`DefaultAzureCredential`)
  and OpenAI (API key), switchable at runtime.
- `AgentRunner` with retry, prompt sanitisation, output guardrail, content
  moderation, conversation memory, structured tracing, source-gen logging.
- Function tools: `GetCurrentTimeTool`, `SearchKnowledgeBaseTool`,
  `RetrieveDocumentsTool`.
- `ResearchAndSummariseWorkflow` multi-agent example.
- Versioned prompt files (`Prompts/v1/{system,guardrails,examples}.md`).
- Streaming via `IAsyncEnumerable<string>` and HTTP SSE.
- Structured output classification example (`/agent/classify`).

#### Safety
- `InputSanitiser` delimits user content (prompt-injection mitigation).
- `OutputGuardrail` scrubs emails / secrets / phone numbers from responses.
- `IContentModerator` abstraction with no-op default and Azure AI Content
  Safety implementation; checks input and output.
- `PromptRedactor` for PII-safe logs.
- Subject-scoped conversation IDs prevent cross-tenant access.

#### Platform
- API key auth with `FixedTimeEquals` constant-time compare and multiple keys.
- Optional JWT bearer (OIDC) — Entra ID, Auth0, Okta, any compliant provider.
- `AgentAccess` authorization policy enforced on `/agent/*` endpoints.
- Per-key rate limiting: in-memory fixed window or Redis Lua script.
- Daily cost budget per subject (token cap, 402 on overrun).
- `RetryPolicy` with exponential backoff on transient errors.
- `IExceptionHandler` mapping SDK exceptions (`RequestFailedException`,
  `ClientResultException`) to clean 502 / 504 responses.

#### Persistence & retrieval
- `IConversationStore` with in-memory (TTL) and EF Core SQLite implementations.
- `IDocumentRetriever` with in-memory cosine similarity and Qdrant gRPC
  implementations.
- `EmbeddingGeneratorFactory` for Azure OpenAI and OpenAI embeddings.

#### Observability
- OpenTelemetry traces + metrics, console + OTLP exporters; instruments
  `Microsoft.Agents.AI`, `Microsoft.Extensions.AI`, ASP.NET Core, HttpClient.
- Token usage logged and tracked per subject.
- Aspire Dashboard bundled in `docker-compose` and as an optional K8s manifest.
- `/healthz` (liveness) and `/readyz` (readiness with actual Redis, SQLite,
  Qdrant connectivity checks).

#### Developer UX
- Scalar API explorer at `/scalar` styled with GitHub Primer (dark/light auto).
- OpenAPI schema at `/openapi/v1.json`.
- AOT-friendly DTOs via `JsonSerializerContext` source-gen.

#### Quality
- `TreatWarningsAsErrors=true`, nullable enabled, central package management.
- Analyzers: NetAnalyzers, SonarAnalyzer.CSharp, SecurityCodeScan.VS2019.
- Zero `[SuppressMessage]` attributes — rules satisfied by real code or
  scoped `.editorconfig`.
- xUnit + NSubstitute + `FakeTimeProvider`. All MIT/Apache, zero commercial
  test dependencies. 32 tests covering sanitiser, guardrail, retry,
  retriever, usage tracker, runner null-guards, prompt evals.

#### Deploy
- Multi-stage Dockerfile on `aspnet:10.0-noble-chiseled` (non-root, distroless).
- docker-compose stack: agent, Redis, Qdrant, Aspire Dashboard.
- Kubernetes manifests: namespace, ConfigMap, Secret stub, Deployment
  (read-only FS, dropped caps, three probes), Service, HPA, NetworkPolicy,
  optional Aspire Dashboard, Kustomization.

[Unreleased]: https://github.com/EncryptedTouhid/et-complete-agent/compare/v0.1.0...HEAD
[0.1.0]: https://github.com/EncryptedTouhid/et-complete-agent/releases/tag/v0.1.0
