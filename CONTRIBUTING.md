# Contributing to XSpecification

Thanks for your interest in contributing! This guide explains how to set up your environment, the conventions we follow, and the contribution workflow.

## Quick start

```powershell
# Clone
git clone https://github.com/xumix/XSpecification.git
cd XSpecification

# Restore + build (uses Central Package Management; SDK roll-forward picks up .NET 10 if available)
dotnet restore XSpecification.slnx
dotnet build XSpecification.slnx -c Release

# Run the full test suite
dotnet test XSpecification.slnx -c Release
```

> .NET SDK requirements come from [`global.json`](global.json). `rollForward: latestFeature` accepts any `10.0.*` release. `net8.0` is also supported by the project target frameworks; CI builds against both runtimes.

## Repository layout

| Path | Purpose |
| --- | --- |
| `XSpecification.Core/` | Filter primitives (`StringFilter`, `RangeFilter<T>`, `ListFilter<T>`, etc.), abstract `SpecificationBase<,,>`, pipeline infrastructure. |
| `XSpecification.Linq/` | LINQ implementation that produces `Expression<Func<TModel,bool>>`. |
| `XSpecification.Elasticsearch/` | NEST 7.x implementation that produces `QueryContainer`. |
| `XSpecification.Linq.Tests/` | NUnit tests for the LINQ backend (uses EF Core SQLite). |
| `XSpecification.Elastic.Tests/` | NUnit tests for the Elasticsearch backend. |
| `Directory.Build.props` | Centralised compiler settings, analyzers, SourceLink, packaging metadata. |
| `Directory.Packages.props` | Centralised NuGet package versions. **Do not put `Version=` on `<PackageReference>` in csproj files.** |

## Coding conventions

- C# `LangVersion=latest`, nullable reference types **enabled**.
- Static analysis: `Microsoft.CodeAnalysis.NetAnalyzers` (`AllEnabledByDefault`), `StyleCop.Analyzers`, `Roslynator.Analyzers`.
- The build is configured with `TreatWarningsAsErrors=true`. New warnings will fail the build.
- Suppressions live in [`Directory.Build.props`](Directory.Build.props) under `<NoWarn>`. Phase 2 of the modernization roadmap removes most of those suppressions; new contributions should not add to that list without explicit justification.
- Public API changes are documented in [`CHANGELOG.md`](CHANGELOG.md) (Keep a Changelog format).

## Adding or upgrading a NuGet package

1. Edit [`Directory.Packages.props`](Directory.Packages.props) — add a `<PackageVersion Include="..." Version="..." />` entry.
2. In the consuming `*.csproj`, add `<PackageReference Include="..." />` **without** a `Version=` attribute.

Dependabot ([`.github/dependabot.yml`](.github/dependabot.yml)) opens grouped PRs weekly for NuGet and GitHub Actions.

## Tests

- All non-trivial changes must include or update unit tests.
- Tests use NUnit + AutoFixture + FluentAssertions + NSubstitute.
- Coverage is collected via `coverlet.collector` and uploaded by CI.

## Pull request workflow

1. Fork the repo (or create a branch in the upstream if you have access).
2. Open a PR against `master`. The [pull request template](.github/PULL_REQUEST_TEMPLATE.md) lists the required checklist.
3. CI must be green: build, test, CodeQL.
4. Update [`CHANGELOG.md`](CHANGELOG.md) under `## [Unreleased]` for any user-visible change.
5. Squash + merge once approved.

## Releasing

Releases are produced by [`.github/workflows/release.yml`](.github/workflows/release.yml) when a tag matching `v*` is pushed:

```powershell
git tag v1.2.0
git push origin v1.2.0
```

The workflow builds, tests, packs (`*.nupkg` + `*.snupkg`), pushes to NuGet.org and creates a GitHub Release with auto-generated notes. The NuGet API key lives in the `NUGET_API_KEY` repository secret.

## Reporting issues

Please include:

- Minimal repro (filter type + spec + expected vs actual expression / query).
- Target framework, package versions, EF Core / NEST version (where relevant).
- Stack trace if any.
