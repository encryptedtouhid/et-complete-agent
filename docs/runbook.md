# Runbook

Operational procedures for an agent service generated from this template.

## Quick health check

```bash
curl http://agent/healthz                          # liveness
curl http://agent/readyz                           # readiness — checks Redis, SQLite, Qdrant if configured
curl -H "X-API-Key: $KEY" http://agent/openapi/v1.json   # auth + routing
```

If `/readyz` returns 503, the response body names the failing dependency. Most
common: Redis or Qdrant not reachable from the pod's network namespace.

## Common incidents

### "The agent could not reach the model provider" in responses

`HttpRequestException` from the LLM provider after retries are exhausted.

1. Open the Aspire Dashboard, filter traces by `agent.run` with status `Error`.
2. The child HTTP span shows the actual provider URL and HTTP status.
3. If 5xx from provider: wait and retry; check provider status page.
4. If DNS / connection refused: check NetworkPolicy egress rules.
5. If 401/403: the LLM credential is wrong — rotate it (see below).

### "Upstream provider returned 429"

Provider-side rate limit. Options:

- Raise `Resilience:MaxRetryAttempts` and `Resilience:BackoffSeconds`.
- Lower local request rate (`RateLimit:PermitLimit`).
- Bump the Azure OpenAI / OpenAI quota with the provider.

### 402 Payment Required

A caller exceeded `CostBudget:DailyTokenLimitPerKey`. Identify the subject via
the Aspire Dashboard's structured logs (filter on subject_id). Either:

- Raise the limit globally,
- Move the heavy caller to a separate API key with a higher cap, or
- Send them away (this is what the budget exists for).

### 401 Unauthorized

Either no `X-API-Key`, wrong key, or (with JWT on) the bearer is invalid or
expired. The `WWW-Authenticate` header names the failure mode.

### Rate limiter rejecting legitimate traffic

When using Redis-backed rate limiting, all instances share the same counter.
If a caller is exceeding their allotment, they really are exceeding it. Check
the Aspire Dashboard metric `rate-limiter.rejections` grouped by the partition
key.

## Routine operations

### Rotate an API key

1. Add the new key to the `ApiKeys` array (don't remove the old yet).
2. Roll the deployment so all pods accept both keys.
3. Switch clients to the new key. Watch logs for any remaining traffic on the
   old key.
4. Remove the old key from the array and roll again.

### Roll a new prompt version

1. Copy `src/<Name>.Host/Prompts/v1/` to `v2/`.
2. Edit `v2/` files.
3. Change `PromptVersion.V1` references (or add `V2`) in code.
4. Add a `PromptEvalHarness` test that loads `v2` and asserts the new
   structure.
5. Deploy. Roll back by reverting the `PromptVersion` constant.

### Drain conversations before a destructive migration

The conversation store is keyed by `{subject-hash}:{user-conversationId}`. To
clear everything for a subject:

```bash
# SQLite — query the store directly via a temporary pod
sqlite3 /data/agent.db "DELETE FROM ConversationMessages WHERE ConversationId LIKE 'k:<subject-hash-prefix>%'"
```

For in-memory store, conversations evict on TTL (`Conversation:TtlMinutes`).

### Re-index the RAG store

`InMemoryDocumentRetriever` rebuilds on every restart. `QdrantDocumentRetriever`
persists in Qdrant — to re-index, delete the collection:

```bash
curl -X DELETE http://qdrant:6333/collections/agent-docs
# Next IndexAsync call will recreate it.
```

### Deploy a new version

The default `Deployment` is `RollingUpdate` with `maxSurge: 1`, `maxUnavailable: 0`.
A new image will roll out one pod at a time, only progressing if `/readyz`
passes on the new pod.

To pause a roll-out mid-flight: `kubectl rollout pause deployment/completeagent -n completeagent`.

## When you need to dig deeper

- **Aspire Dashboard** — http://localhost:18888 (local) or `kubectl port-forward`
  in cluster. Filter traces by span name (`agent.run`), by tag (`agent.subject_id`,
  `ai.tokens.total`), by status.
- **Structured logs** — every endpoint logs a redacted preview of the input;
  search by request id or conversation id.
- **OpenAPI / Scalar** — `/scalar` lets you reproduce a request from the browser
  to confirm an issue is server-side, not client.
- **OpenTelemetry export** — flip `Telemetry:OtlpEndpoint` to your APM
  (Application Insights, Honeycomb, Grafana Cloud) for production-grade query
  + retention.
