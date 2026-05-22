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

The compose stack wires `agent → redis → qdrant` with persistent volumes for SQLite and Qdrant.

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
| `kustomization.yaml` | One-line `kubectl apply -k` deploy. |

### Production hardening reminders

- Replace `secret.example.yaml` with **External Secrets Operator** or **Azure Key Vault CSI driver**.
- Pin the image by digest (`@sha256:...`), not the `latest` tag.
- Add an Ingress + TLS termination (Gateway API, NGINX, Traefik, or cloud-native).
- Run a PodDisruptionBudget so HPA scale-downs don't take down all replicas at once.
- Add a PriorityClass if running alongside lower-priority workloads.
- Enable a service mesh (Istio / Linkerd) for mTLS between agent and Redis/Qdrant.
- Wire the OpenTelemetry collector to your APM (Application Insights, Honeycomb, Grafana Cloud).
