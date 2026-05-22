# Complete Agent — Microsoft Agent Framework Template

A production-grade `dotnet new` template for building AI agents on **Microsoft Agent Framework** in .NET 10.

Generates a Clean-Architecture solution with everything a real deployment needs: dependency injection, validated configuration, versioned prompts, function tools, RAG, conversation memory, content moderation, cost budgeting, JWT + API-key auth, distributed rate limiting, OpenTelemetry, retries, health checks, structured JSON output, streaming, plus Docker and Kubernetes manifests.

- **Package ID** `ET.AgentFramework.Templates`
- **Short name** `et-complete-agent`
- **Target framework** `net10.0`
- **License** MIT

---

## Quick start

```bash
# 1. Install the template
dotnet new install ET.AgentFramework.Templates

# 2. Generate a new solution
dotnet new et-complete-agent -n CompleteAgent
cd CompleteAgent

# 3. Set a provider + API key (Azure OpenAI shown below)
dotnet user-secrets set "Agent:AzureOpenAI:Endpoint" "https://<resource>.openai.azure.com" \
    --project src/ET.CompleteAgent.Host
az login   # DefaultAzureCredential picks this up

# 4. Set an API key for your agent endpoint
dotnet user-secrets set "Authentication:ApiKey" "$(openssl rand -hex 16)" \
    --project src/ET.CompleteAgent.Host

# 5. Run
dotnet run --project src/ET.CompleteAgent.Host
```

```bash
# Call the agent
curl -X POST http://localhost:5000/agent/run \
  -H "X-API-Key: <your-key>" \
  -H "Content-Type: application/json" \
  -d '{"input": "Summarise Clean Architecture in two sentences."}'
```

---

## What you get

```
CompleteAgent/
├── CompleteAgent.sln
├── Dockerfile                                Multi-stage, distroless, non-root
├── docker-compose.yml                        Agent + Redis + Qdrant for local dev
├── deploy/k8s/                               Namespace, Deployment, HPA, NetworkPolicy, ...
├── src/
│   ├── ET.CompleteAgent.Domain/                    Contracts, value records (zero deps)
│   ├── ET.CompleteAgent.Application/               Runner, tools, workflows, sanitiser, retry
│   ├── ET.CompleteAgent.Infrastructure/            LLM client, EF Core, Qdrant, moderation
│   └── ET.CompleteAgent.Host/                      Program.cs, endpoints, auth, telemetry
└── tests/
    └── ET.CompleteAgent.Application.Tests/         xUnit + NSubstitute + FakeTimeProvider
```

All assemblies use the `ET.<ProjectName>.*` namespace convention. The `ET.` prefix is the org marker and stays fixed; the middle segment is whatever you pass to `dotnet new -n`.

---

## Feature inventory

### Agent core
| | |
| --- | --- |
| **LLM providers** | Azure OpenAI (`DefaultAzureCredential`) and OpenAI (`OPENAI_API_KEY`), switchable at runtime |
| **Function tools** | `GetCurrentTimeTool`, `SearchKnowledgeBaseTool`, `RetrieveDocumentsTool` (RAG) — extend via `AIFunctionFactory.Create` |
| **Workflow** | `ResearchAndSummariseWorkflow` (multi-agent pipeline) |
| **Streaming** | `IAsyncEnumerable<string>` runner + SSE HTTP endpoint |
| **Structured output** | Sentiment classifier endpoint returning typed JSON |
| **Conversation memory** | `IConversationStore` with `InMemory` and `Sqlite` (EF Core) implementations, TTL + message cap |
| **RAG** | `IDocumentRetriever` with `InMemory` cosine-similarity + Qdrant gRPC implementations |
| **Versioned prompts** | `src/<Name>.Host/Prompts/v1/{system,guardrails,examples}.md` — bump to `v2/` for a roll |

### Safety & policy
| | |
| --- | --- |
| **Input sanitisation** | `InputSanitiser` delimits and escapes user input to mitigate prompt injection |
| **Output guardrail** | `OutputGuardrail` scrubs emails, secrets, phone numbers from agent responses |
| **Content moderation** | `IContentModerator` abstraction with no-op default + Azure AI Content Safety implementation |
| **PII-safe logging** | `PromptRedactor` truncates and redacts before logs; source-gen `LoggerMessage` everywhere |
| **Prompt evals** | Golden-set tests verifying prompt structure and absence of leaked secrets |

### Platform & ops
| | |
| --- | --- |
| **Authentication** | `X-API-Key` middleware (constant-time compare) + optional JWT bearer (Entra ID / Auth0 / Okta) |
| **Authorization** | `AgentAccess` policy — requires JWT when enabled, open when not |
| **Rate limiting** | Per-key fixed window — in-memory default, Redis Lua-script for multi-instance |
| **Cost budget** | Per-key daily token cap, returns `402 Payment Required` when exceeded |
| **Retries** | Exponential backoff on `HttpRequestException`, `TimeoutException` |
| **Telemetry** | OpenTelemetry traces + metrics, OTLP + console exporters, instruments `Microsoft.Agents.AI`, ASP.NET Core, HttpClient |
| **Health checks** | `/healthz` (liveness), `/readyz` (validates LLM config is present) |
| **OpenAPI** | `MapOpenApi()` exposes the schema; wire your Swagger UI of choice |

### Engineering quality
| | |
| --- | --- |
| **Architecture** | Strict Clean Architecture: `Domain ← Application ← Infrastructure ← Host` |
| **DI** | Microsoft.Extensions.DependencyInjection throughout; layer-scoped `Add*` extensions |
| **Config** | Options pattern with `DataAnnotations` + `IValidatableObject` + `ValidateOnStart` |
| **Analyzers** | `NetAnalyzers`, `SonarAnalyzer.CSharp`, `SecurityCodeScan.VS2019` |
| **Build hygiene** | `TreatWarningsAsErrors=true`, nullable enabled, implicit usings, central package versioning |
| **Tests** | xUnit + NSubstitute + `FakeTimeProvider` — all MIT/Apache, **zero commercial libraries** |
| **AOT-friendly JSON** | `JsonSerializerContext` source-gen for endpoint DTOs |

---

## HTTP API surface

| Method | Path | Description |
| --- | --- | --- |
| `POST` | `/agent/run` | Single-turn request. Auto-persists turn when `conversationId` is supplied. |
| `POST` | `/agent/stream` | Server-Sent Events stream of agent output. |
| `POST` | `/agent/classify` | Structured-output example returning typed sentiment JSON. |
| `DELETE` | `/agent/conversations/{conversationId}` | Wipes a stored conversation. |
| `GET` | `/healthz` | Liveness — bypasses auth and rate limiting. |
| `GET` | `/readyz` | Readiness — validates LLM provider configuration. |
| `GET` | `/openapi/v1.json` | OpenAPI schema. |

Request body for `/agent/run` and `/agent/stream`:

```json
{ "input": "...", "conversationId": "optional-uuid" }
```

---

## Configuration

Every knob lives in `appsettings.json` (or env vars prefixed `COMPLETEAGENT_`, or `dotnet user-secrets`). Defaults are dev-friendly so the template runs out of the box; production hardens by flipping switches.

| Section | What it controls |
| --- | --- |
| `Agent` | LLM provider (`AzureOpenAI`/`OpenAI`), model, endpoint, API key |
| `Authentication` | API key for the `X-API-Key` header |
| `Jwt` | OIDC bearer auth (authority, audience, issuers) — disabled by default |
| `RateLimit` | `InMemory` or `Redis`, permits, window, Redis connection string |
| `CostBudget` | Daily token cap per key — disabled by default |
| `Resilience` | Retry attempts, backoff seconds |
| `Telemetry` | Service name, OTLP endpoint, console exporter toggle |
| `Conversation` | TTL minutes, max messages per conversation |
| `Persistence` | `InMemory` or `Sqlite`, connection string |
| `Retrieval` | Embedding model, vector store (`InMemory` or `Qdrant`), Qdrant host/port |
| `Moderation` | `None` or `AzureContentSafety`, endpoint, max severity threshold |

---

## Deploy

### Local — docker-compose

```bash
export OPENAI_API_KEY=sk-...
export API_KEY=$(openssl rand -hex 16)
docker compose up --build
```

Wires `agent → redis → qdrant → aspire-dashboard` with persistent volumes for SQLite and Qdrant.

| Service | URL |
| --- | --- |
| Agent API | http://localhost:8080 |
| **Aspire Dashboard** (traces, metrics, logs) | **http://localhost:18888** |
| Qdrant UI | http://localhost:6333/dashboard |

The bundled [Aspire Dashboard](https://learn.microsoft.com/dotnet/aspire/fundamentals/dashboard) is OTLP-native and shows resources, console output, structured logs, distributed traces (`agent.run`, LLM calls, tool calls), and per-token metrics — zero config. Swap to Grafana / Application Insights / Honeycomb for production by pointing `COMPLETEAGENT_Telemetry__OtlpEndpoint` at their gateway.

### Kubernetes

```bash
docker build -t ghcr.io/<your-org>/completeagent:1.0.0 .
docker push ghcr.io/<your-org>/completeagent:1.0.0

# Edit deploy/k8s/secret.example.yaml with real values (or use External Secrets / Key Vault CSI).
kubectl apply -k deploy/k8s
```

Bundled manifests: `namespace`, `configmap`, `secret.example`, `deployment` (non-root, read-only FS, dropped capabilities, 3 probes), `service`, `hpa` (2–10 replicas on CPU+memory), `networkpolicy` (DNS, HTTPS, internal services only), `kustomization`.

---

## Develop the template

```bash
# Pack
dotnet pack -c Release -o ./artifacts

# Install from local nupkg
dotnet new install ./artifacts/ET.AgentFramework.Templates.0.1.0.nupkg

# Generate a sample and smoke-test
dotnet new et-complete-agent -n Demo -o /tmp/Demo
(cd /tmp/Demo && dotnet build && dotnet test)

# Uninstall when iterating
dotnet new uninstall ET.AgentFramework.Templates
```

CI runs the same pack → install → generate → build → test flow on every push (`.github/workflows/ci.yml`).

---

## Versions

| | Version |
| --- | --- |
| .NET SDK | `10.0.x` (via `global.json`, `rollForward: latestMajor`) |
| `Microsoft.Agents.AI` | `1.6.2` |
| `Microsoft.Extensions.AI` | `10.6.0` |
| `Azure.AI.OpenAI` | `2.1.0` |
| `OpenTelemetry` | `1.15.x` |
| `EntityFrameworkCore.Sqlite` | `10.0.8` |
| `Qdrant.Client` | `1.18.1` |

---

## Roadmap / not yet included

- WebApplicationFactory-based integration tests
- Conversation summarisation when history exceeds context budget
- Embedding cache
- Idempotency keys, circuit breaker
- Document chunker / ingestion pipeline
- Citations from RAG, hallucination scoring
- MCP server tools, multi-step planner
- Multimodal (image / audio)

See `Roadmap` discussion in repo issues to vote or contribute.

---

## License

MIT — see [LICENSE](LICENSE).
