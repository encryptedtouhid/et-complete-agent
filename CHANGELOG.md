# Changelog

All notable changes to this template are documented here. The format follows
[Keep a Changelog](https://keepachangelog.com/en/1.1.0/) and the project follows
[Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

## [0.4.1] — 2026-05-23

### Added
- Azure OpenAI now accepts an API key via `Agent:AzureOpenAI:ApiKey` (uses
  `AzureKeyCredential`). When unset, the provider keeps using
  `DefaultAzureCredential` exactly as before — no behaviour change for
  existing managed-identity / `az login` flows.

### Fixed
- Host crashed at startup with the default config (`Jwt.Enabled = false`)
  because `app.UseAuthentication()` ran unconditionally while
  `AddAuthentication()` is only registered when JWT is enabled.
  `UseAuthentication()` is now gated on `Jwt.Enabled`; the API-key
  middleware is unaffected.

## [0.4.0] — 2026-05-23

### Added

#### Conversation memory — pluggable backends
- `IConversationStore` now ships with seven concrete backings, picked via
  `Persistence:ConversationStore`:
  - `InMemory` (default, `IMemoryCache` with sliding TTL)
  - `Sqlite` (EF Core, file or `:memory:`)
  - `SqlServer` (EF Core, retry-on-failure enabled)
  - `AzureSql` (EF Core, same provider as SqlServer)
  - `Postgres` (EF Core via Npgsql, retry-on-failure)
  - `MySql` (EF Core via Pomelo; explicit `MySqlServerVersion` or auto-detect)
  - `Cosmos` (native `Microsoft.Azure.Cosmos`, container partitioned by
    `/conversationId`, container-level TTL)
  - `Mongo` (native `MongoDB.Driver`, compound + TTL indexes)
- `IAuditLog` now has matching EF Core, Cosmos, and Mongo implementations so
  audit follows the conversation store automatically.
- Schema bootstrap hosted services (`RelationalSchemaBootstrapper`,
  `CosmosSchemaBootstrapper`, `MongoSchemaBootstrapper`) ensure the schema /
  containers / indexes exist on startup — idempotent and safe to re-run.
- `RelationalDbHealthCheck` replaces the SQLite-specific check; covers every
  EF Core provider via `CanConnectAsync`. New `CosmosHealthCheck` and
  `MongoHealthCheck` cover the document stores. All tagged `ready` so
  `/readyz` fails fast when the configured store is unreachable.
- `PersistenceOptions` is now `IValidatableObject` — provider-specific config
  is checked at startup (`ValidateOnStart`) with clear error messages.

#### Tests
- New unit suites under `tests/EncryptedTouhid.CompleteAgent.Application.Tests`:
  `PersistenceOptionsTests`, `InfrastructureDispatchTests`,
  `InMemoryConversationStoreTests`, and SQLite-in-memory
  `EfCoreConversationStoreTests` exercising the provider-agnostic EF path.
- New `EncryptedTouhid.CompleteAgent.Infrastructure.IntegrationTests` project
  with Testcontainers fixtures for MSSQL, Postgres, MySQL, Cosmos emulator,
  and MongoDB. Tests carry `[Trait("Category", "Integration")]` so default
  `dotnet test --filter "Category!=Integration"` skips them; CI runs both
  unit and integration suites.

## [0.3.0] — 2026-05-23

### Added

#### Versioning + Source Link
- **Automatic SemVer from git tags** via MinVer in both the template package
  and every project in the generated solution. Tag `v1.2.3` produces a
  `1.2.3` build; pre-tag builds emit `0.0.0-alpha.0`. `MinVerAutoIncrement`
  is `minor` by default, `MinVerTagPrefix` is `v`.
- **Source Link** with embedded sources and symbols on the template package
  so consumers can step into the template's own code in their debugger.
- **`/version` endpoint** exposing AssemblyInformationalVersion, parsed commit
  SHA, dirty-flag, and environment. Auth-bypassed alongside `/healthz`.

#### Security & resilience
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

#### Developer workflow
- **Tracked git hooks under `.githooks/`** with `scripts/install-hooks.sh`
  to activate them. `pre-commit` runs three gates in fail-fast order:
  `dotnet format --verify-no-changes` against the template source, then
  `dotnet pack` of the template package, then install + `dotnet new` +
  build + test on a generated sample. Bypass with `git commit --no-verify`
  when you really must.
- Canonical formatting applied across the entire template source via
  `dotnet format` (no manual alignment spaces, consistent indentation,
  final newlines).
- Tag-triggered NuGet publish job in CI. Push a `v*` tag, CI packs and
  pushes the matching version. Requires `NUGET_API_KEY` repository secret
  scoped to the `nuget` environment.

#### Docs
- Repo-health files: SECURITY, CONTRIBUTING, CODE_OF_CONDUCT, GitHub issue /
  PR templates.
- Architecture decision records (`docs/adr/0001..0003`).
- Operational runbook (`docs/runbook.md`).
- README badges for CI status, NuGet version, downloads, license, .NET version.

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

[Unreleased]: https://github.com/EncryptedTouhid/et-complete-agent/compare/v0.4.0...HEAD
[0.4.0]: https://github.com/EncryptedTouhid/et-complete-agent/compare/v0.3.0...v0.4.0
[0.3.0]: https://github.com/EncryptedTouhid/et-complete-agent/compare/v0.1.0...v0.3.0
[0.1.0]: https://github.com/EncryptedTouhid/et-complete-agent/releases/tag/v0.1.0
