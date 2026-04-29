# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Phase 3 — Major redesign (breaking, target 2.0.0)

#### Added

- Multi-targeting `net8.0;net10.0` for every library. Tests target both frameworks.
- `XSpecification.Elasticsearch.Abstractions` — new package shipping a backend-agnostic
  query AST (`IQueryNode`, `IQueryBackend<TQuery>`).
- `XSpecification.Elasticsearch.V8` — new package targeting `Elastic.Clients.Elasticsearch` 8.x.
- `XSpecification.SourceGenerator` — Roslyn 4.x incremental generator that emits filter
  property accessors and warns about unmapped filter properties (`XSPEC001`).
- `BoolFilter` (`XSpecification.Core`) — tri-state boolean filter (unset / `true` / `false`).
- `OrGroup` / `AndGroup` fluent helpers on the LINQ and Elasticsearch `SpecificationBase`s.
- `SpecificationCompositionExtensions.And` / `.Or` — compose two specs sharing the same model
  and filter into a combined predicate / query.
- Async surface: `CreateFilterExpressionAsync` (LINQ) and `CreateFilterQueryAsync` (ES) accept
  `CancellationToken`.
- `SpecificationConfiguration` record-style class — replaces `IOptions<Options>` injection.
- Documentation site under `docs/` (mkdocs-material) and `MIGRATION.md` (1.x → 2.0).

#### Changed

- `XSpecification.Elasticsearch` package id now ships with the NEST 7.x backend
  (`PackageId=XSpecification.Elasticsearch.Nest`) and depends on
  `XSpecification.Elasticsearch.Abstractions`.
- `IFilterHandlerCollection` no longer exposes `LinkedListNode<Type>` based members;
  `FilterHandlerCollection` now composes `LinkedList<Type>` instead of inheriting it.
- `SpecificationBase.UnmatchedProps` is `[Obsolete]`; new code uses the read-only
  `UnhandledFilterProperties` collection.
- Linq / Elasticsearch `SpecificationBase` constructors take `SpecificationConfiguration`;
  the `IOptions<Options>` overload is preserved as `[Obsolete]` for one release.
- `Add*Specification` now accepts an optional `Action<SpecificationConfiguration>` delegate and
  no longer returns `OptionsBuilder<Options>`.
- Removed direct dependency on `Microsoft.Extensions.Options.ConfigurationExtensions`.

#### Deprecated

- `XSpecification.Core.Options` — use `SpecificationConfiguration` instead.
- `IOptions<Options>` constructor overloads on the LINQ and Elasticsearch `SpecificationBase`s.
- `SpecificationBase.UnmatchedProps` (mutable) — read `UnhandledFilterProperties` instead.

### Phase 1 — Infrastructure & Build hygiene

#### Added

- Central Package Management via `Directory.Packages.props` — unified versions of `Microsoft.Extensions.*` and the test stack across all projects.
- `Microsoft.SourceLink.GitHub`, `StyleCop.Analyzers` and `Roslynator.Analyzers` are added globally through `Directory.Build.props` (PrivateAssets=all).
- `Directory.Build.props` now enables: `GenerateDocumentationFile`, `Deterministic`, `ContinuousIntegrationBuild`, `EmbedUntrackedSources`, snupkg symbol packages, `PublishRepositoryUrl`, package tags and `TreatWarningsAsErrors`.
- New CI workflow ([`.github/workflows/dotnet.yml`](.github/workflows/dotnet.yml)): `actions/checkout@v4`, `actions/setup-dotnet@v4` with both `8.0.x` and `10.0.x`, NuGet cache, Cobertura coverage upload and Codecov.
- New release workflow ([`.github/workflows/release.yml`](.github/workflows/release.yml)) for tag-triggered NuGet publish + GitHub release.
- Dependabot config ([`.github/dependabot.yml`](.github/dependabot.yml)) for `nuget` and `github-actions`, weekly, with grouped PRs.
- `CONTRIBUTING.md`, `CHANGELOG.md`, PR template.

#### Changed

- [`.github/workflows/codeql-analysis.yml`](.github/workflows/codeql-analysis.yml) updated to `actions/checkout@v4` + CodeQL v3 actions.
- [`global.json`](global.json) bumped to `.NET 10` SDK with `rollForward: latestFeature`.
- [`README.md`](README.md) updated: removed completed Elasticsearch TODO, added CI / NuGet / coverage badges, added Elastic DI usage section.
- [`.github/workflows/dotnet.yml`](.github/workflows/dotnet.yml) targets `net8.0` and `net10.0` instead of the previous `dotnet 6.0.x` / `net6.0` configuration that did not match the actual project target framework.

#### Fixed

- Version drift between projects: `Microsoft.Extensions.Logging.Abstractions` was `10.0.7` in `XSpecification.Linq` but `6.0.4` in `XSpecification.Elasticsearch`; `Microsoft.Extensions.Options.ConfigurationExtensions` was `10.0.7` vs `9.0.2`. Both pinned to `10.0.7` via CPM.

[Unreleased]: https://github.com/xumix/XSpecification/compare/v1.0.0...HEAD
