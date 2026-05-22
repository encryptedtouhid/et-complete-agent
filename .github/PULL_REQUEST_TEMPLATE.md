<!-- Thanks for the contribution. Please fill in what's relevant; delete what isn't. -->

## Summary

<!-- One paragraph: what this PR changes and why. -->

## Type of change

- [ ] Bug fix (non-breaking)
- [ ] New feature (non-breaking)
- [ ] Breaking change (template parameters, public API, or generated-project shape)
- [ ] Documentation only
- [ ] Build / CI / packaging

## Checklist

- [ ] `dotnet pack` succeeds and produces a valid nupkg
- [ ] `dotnet new install` + `dotnet new et-complete-agent -n SmokeTest` works
- [ ] Generated project builds with **zero warnings** (TreatWarningsAsErrors)
- [ ] `dotnet test` passes against the generated project
- [ ] New options have `DataAnnotations` validation and `ValidateOnStart`
- [ ] Any new dependency is MIT or Apache 2.0 and pinned in `Directory.Packages.props`
- [ ] No new `[SuppressMessage]` attributes
- [ ] `CHANGELOG.md` updated under `[Unreleased]`
- [ ] If security-relevant, `SECURITY.md` reviewed

## Test plan

<!-- How did you verify this? Concrete commands or scenarios. -->

## Screenshots / output

<!-- Only if UI-affecting (Scalar UI, Aspire Dashboard) or behaviour-changing. -->
