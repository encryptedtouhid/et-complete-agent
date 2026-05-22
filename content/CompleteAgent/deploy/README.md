# Deploy

## Local with docker-compose

```bash
export OPENAI_API_KEY=sk-...
export API_KEY=$(openssl rand -hex 16)
docker compose up --build
curl -H "X-API-Key: $API_KEY" -X POST http://localhost:8080/agent/run \
  -H "Content-Type: application/json" \
  -d '{"input": "hello"}'
```

The compose stack wires `agent → redis → qdrant → aspire-dashboard` with persistent volumes for SQLite and Qdrant.

| Service | URL | Purpose |
| --- | --- | --- |
| Agent API | http://localhost:8080 | `/agent/run`, `/agent/stream`, `/healthz`, `/openapi/v1.json` |
| **Aspire Dashboard** | **http://localhost:18888** | **Traces, metrics, structured logs, resource view** |
| Qdrant UI | http://localhost:6333/dashboard | Vector collections |
| Redis | localhost:6379 | Rate limiter store |

### Observability dashboard

After `docker compose up`, open `http://localhost:18888` and you'll see:

- **Resources** — every running service (agent, redis, qdrant) with health and env
- **Console** — stdout/stderr per container
- **Structured logs** — `ILogger` output with searchable scopes (request id, conversation id, user)
- **Traces** — every HTTP request, agent activity (`agent.run`), LLM call, tool invocation, with timing waterfalls
- **Metrics** — token usage, request rate, latency p50/p95/p99, rate-limit rejections

The dashboard is the .NET Aspire Dashboard image (`mcr.microsoft.com/dotnet/aspire-dashboard`) — first-party Microsoft, OTLP-native, zero config. Anonymous access is enabled for local dev only.

## Kubernetes

```bash
# 1. Build & push the image
docker build -t ghcr.io/<your-org>/completeagent:1.0.0 -f Dockerfile .
docker push ghcr.io/<your-org>/completeagent:1.0.0

# 2. Replace placeholders in deploy/k8s/secret.example.yaml,
#    then RENAME it to secret.yaml (gitignored — never commit real secrets).
#    Better: use External Secrets / Azure Key Vault CSI / Sealed Secrets.

# 3. Apply
kubectl apply -k deploy/k8s

# 4. (Optional) Drop in the Aspire Dashboard for in-cluster observability
kubectl apply -f deploy/k8s/aspire-dashboard.yaml
kubectl -n completeagent port-forward svc/aspire-dashboard 18888:18888
open http://localhost:18888
```

### What's in the manifests

| File | Purpose |
| --- | --- |
| `namespace.yaml` | Dedicated `completeagent` namespace. |
| `configmap.yaml` | Non-secret runtime config (provider, model, endpoints). |
| `secret.example.yaml` | Template for API keys / connection strings — DO NOT commit real values. |
| `deployment.yaml` | 2 replicas, distroless image, non-root, read-only root FS, dropped capabilities, liveness/readiness/startup probes. |
| `service.yaml` | ClusterIP service on port 80. Add Ingress / Gateway for external traffic. |
| `hpa.yaml` | HPA scaling 2-10 replicas on CPU + memory. |
| `networkpolicy.yaml` | Egress restricted to DNS / HTTPS / internal Redis/Qdrant/OTel. |
| `aspire-dashboard.yaml` | Optional — in-cluster observability UI. |
| `kustomization.yaml` | One-line `kubectl apply -k` deploy (does not include the dashboard by default). |

### Choosing an observability backend

The agent emits standard OTLP. Point `COMPLETEAGENT_Telemetry__OtlpEndpoint` at whichever collector you prefer:

| Backend | When to use it | OTLP endpoint pattern |
| --- | --- | --- |
| **Aspire Dashboard** | Dev, demos, low-volume internal | `http://aspire-dashboard:18889` |
| **Grafana + Tempo + Loki + Prometheus** | Self-hosted OSS, high volume, long retention | `http://otel-collector:4317` (with collector in front) |
| **Azure Monitor / Application Insights** | Azure-hosted workloads, integrated with the rest of the stack | use `Azure.Monitor.OpenTelemetry.AspNetCore` directly |
| **Honeycomb / Datadog / New Relic / Lightstep** | Managed APM with rich query / SLO tooling | their OTLP gateway URL + API key header |

### Production hardening reminders

- Replace `secret.example.yaml` with **External Secrets Operator** or **Azure Key Vault CSI driver**.
- Pin the image by digest (`@sha256:...`), not the `latest` tag.
- Add an Ingress + TLS termination (Gateway API, NGINX, Traefik, or cloud-native).
- Run a PodDisruptionBudget so HPA scale-downs don't take down all replicas at once.
- Add a PriorityClass if running alongside lower-priority workloads.
- Enable a service mesh (Istio / Linkerd) for mTLS between agent and Redis/Qdrant.
- For the Aspire Dashboard in any non-dev environment: **disable** `DOTNET_DASHBOARD_UNSECURED_ALLOW_ANONYMOUS` and put it behind your normal SSO / Ingress auth, or replace it with a managed APM.
- Wire the OpenTelemetry collector to your chosen APM and keep the dashboard for dev only.
