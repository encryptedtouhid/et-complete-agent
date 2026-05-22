# ADR 0002 — API key + JWT bearer, both supported, neither mandatory

- Status: accepted
- Date: 2026-05-22

## Context

Agent endpoints get called from two very different kinds of clients:

1. **Service-to-service** — backend jobs, CI runners, internal tools. These
   want a single long-lived credential that survives restarts.
2. **End users** — a SPA, mobile app, or another service acting on behalf of
   a logged-in user. These already have a JWT from an IdP (Entra ID, Auth0,
   Okta, Keycloak) and shouldn't need a second secret.

A template that picks one and ignores the other will get rewritten by every
team in the first week.

## Decision

Ship both, opt-in independently:

- `Authentication:ApiKeys` is an array of accepted keys. The `X-API-Key`
  middleware runs first; missing or wrong key → 401. Constant-time compare.
- `Jwt:Enabled` switches on `Microsoft.AspNetCore.Authentication.JwtBearer`.
  The `AgentAccess` authorization policy is empty-pass when JWT is off and
  `RequireAuthenticatedUser()` when on.
- Agent endpoints sit in a route group that calls `.RequireAuthorization(...)`.
- Health, OpenAPI, and Scalar paths bypass both.

When both are enabled, callers must present a valid `X-API-Key` **and** a
valid bearer token — defence in depth.

## Consequences

**Good**

- Service-to-service flows just need a 16-char hex key in an env var.
- Browser / mobile flows can disable the API-key middleware in `Program.cs`
  and rely solely on JWT once they're sure.
- The two layers compose cleanly because they enforce in different middleware
  stages (custom middleware vs authentication/authorization pipeline).

**Trade-offs**

- Defaults are "API key required, JWT optional." A team that wants the
  inverse must remove `app.UseApiKeyAuthentication()`.
- Adding OAuth2 client-credentials, mTLS, or AWS SigV4 each means another
  middleware. We accept that as future work rather than a generic plugin
  system, because two schemes covers >95% of real templates' day-one needs.

## Open follow-ups

- Issuer-scoped JWT (the same agent serving multiple tenants from different
  IdPs) would extend `JwtOptions` to a list and resolve dynamically. Not yet
  needed for the template's stated use case.
