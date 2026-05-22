# Contributing

Thanks for considering a contribution. This repo is a `dotnet new` template,
not a runtime library — so changes affect every future project generated from
it.

## What we accept

- Bug fixes in the generated solution (anything under `content/CompleteAgent/`).
- New optional layers (extra `IConversationStore` providers, additional
  `IContentModerator` implementations, additional vector store backends).
- Documentation improvements.
- Build / CI / packaging fixes.
- New deploy targets (e.g. Helm chart, Bicep / Terraform modules).

## What we won't accept without discussion

- Switching the default LLM provider, default persistence backend, or default
  vector store. The defaults are deliberately the lowest-friction option.
- Adding a new top-level project. Layer boundaries are load-bearing.
- Pulling in commercial or restrictive-license dependencies. **All runtime and
  test dependencies must be MIT or Apache.**
- Adding `[SuppressMessage]` attributes to silence analyzer rules. Either fix
  the underlying code or scope the rule in a folder-level `.editorconfig`.

Open an issue first if you're unsure.

## Local development

```bash
# Build the template and a sample project from it
dotnet pack -c Release -o ./artifacts
dotnet new install ./artifacts/ET.AgentFramework.Templates.0.1.0.nupkg --force
rm -rf /tmp/Demo
dotnet new et-complete-agent -n Demo -o /tmp/Demo
(cd /tmp/Demo && dotnet build && dotnet test)
dotnet new uninstall ET.AgentFramework.Templates
```

The CI pipeline (`.github/workflows/ci.yml`) runs exactly this flow on every
push and pull request.

## Coding standards

- **Layer hygiene** — `Domain` has no references; `Application` only references
  `Domain` + `Microsoft.Extensions.AI`; `Infrastructure` is allowed SDK types;
  `Host` is the only project allowed to know about HTTP, auth, or rate limiting.
- **Warnings as errors** — the build fails on every analyzer warning. Don't
  suppress; fix.
- **Options pattern** — every configurable knob gets a strongly-typed
  `*Options` class with `DataAnnotations` validation and `ValidateOnStart`.
- **Logging** — use source-generated `LoggerMessage` partials (CA1848 is on).
- **Tests** — xUnit only; assertions via the built-in `Assert.*`. NSubstitute
  for mocks. `FakeTimeProvider` for time. No FluentAssertions (commercial in
  v7+) and no Moq (also commercial in 4.20+).

## Commit messages

Conventional Commits style, but pragmatic:

```
type(scope): short summary

Optional body explaining what changed and why.
```

Common types: `feat`, `fix`, `docs`, `refactor`, `test`, `chore`, `ci`, `build`.

Each commit should compile and pass tests on its own. Use `git rebase -i` to
clean up before opening a PR.

## Pull request checklist

- [ ] `dotnet pack && dotnet new install && dotnet new et-complete-agent ...
      && dotnet build && dotnet test` succeeds locally
- [ ] No new analyzer warnings (`TreatWarningsAsErrors=true` is enforced)
- [ ] New configuration options have validation + `ValidateOnStart`
- [ ] New external dependencies are MIT / Apache and pinned in
      `Directory.Packages.props`
- [ ] `CHANGELOG.md` updated under `[Unreleased]`
- [ ] If the change affects security, `SECURITY.md` reviewed
