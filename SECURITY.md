# Security Policy

## Reporting a vulnerability

**Please do not open public GitHub issues for security reports.**

Email security findings to **me@tuhidulhossain.com** with:

- Affected component (template version, generated project version)
- Steps to reproduce
- Impact assessment (confidentiality / integrity / availability)
- Suggested fix, if any
- Whether you intend to disclose publicly and on what timeline

You will receive an acknowledgement within **3 business days**. We aim to:

- Triage within 7 days
- Issue a patch release for confirmed criticals within 30 days
- Credit the reporter (with permission) in the release notes

## Scope

In scope:

- The template package itself (`ET.AgentFramework.Templates`)
- The code shipped under `content/CompleteAgent/` that ends up in users'
  generated projects
- The bundled `Dockerfile`, `docker-compose.yml`, and Kubernetes manifests

Out of scope:

- Vulnerabilities in upstream NuGet packages — please report to the upstream
  maintainer (e.g. `Microsoft.Agents.AI`, `OpenAI`, `Qdrant.Client`).
- Vulnerabilities in services the template integrates with (Azure OpenAI,
  OpenAI, Redis, Qdrant, the Aspire Dashboard) — report to those vendors.
- Issues that require the operator to have already compromised the cluster,
  configuration, or secret store.

## Hardening that is on the operator

The generated project ships safe defaults but **operators are responsible**
for these in production:

- Rotate API keys regularly; store them in a secrets backend (Key Vault, AWS
  Secrets Manager, External Secrets), never in `appsettings.json`.
- Enable JWT bearer authentication (`Jwt:Enabled=true`) when serving user
  traffic; disable anonymous Aspire Dashboard access.
- Pin container images by digest, not the `latest` tag.
- Restrict `NetworkPolicy` egress to your specific LLM provider host.
- Run `dotnet nuget audit` (or equivalent) in CI to catch transitive CVEs.
- Configure CORS allow-list explicitly — do not run with `*` in production.

## Supported versions

| Version | Supported |
| --- | --- |
| 0.1.x | ✓ |
| < 0.1.0 | ✗ |
