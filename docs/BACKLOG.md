# Backlog

Deferred work for **Cirreum.Persistence.Azure**. Items here are tracked but not yet ready
to ship — either because the cost outweighs the benefit in isolation, or
because they're waiting on a forcing function (a related change, a consumer
upgrade, a coordinated multi-repo rollout).

## How this file works

- Each item is a `###` heading so it can be linked to and parsed.
- Each item declares **`SemVer:`** (`Patch` | `Minor` | `Major` | `Unspecified`),
  **`Trigger:`** (the human-readable condition that will make it ready), and
  **`Noted:`** (the date the item was added).
- The Cirreum DevOps release scripts (`PatchRelease`, `MinorRelease`,
  `MajorRelease`) surface items at-or-below the requested bump level so the
  operator can decide whether to fold them in before tagging.
- Items that ship: move from this file to `docs/CHANGELOG.md` under
  `[Unreleased]`. Items that grow into design discussions: promote to an ADR.

## Queued

### Downgrade expected point-read misses from Error to Debug

**SemVer:** Major
**Trigger:** The next major release of this package.
**Noted:** 2026-07-23

`DefaultRepository<TEntity>.GetAsync` logs event `20301` ("Point read encountered
an exception for item type {Type} total RU cost {RU}") at **Error** severity for
every `CosmosException`, including `NotFound (404)` — but a point-read miss is
normal control flow: the repository translates it to `NotFoundException`, which
callers routinely expect (`TryGet*` semantics, `Result`-pipeline mapping,
existence probes). At Error severity, every legitimate miss lands in production
telemetry as a false alarm and can trip log-based alerting.

Change: make the logging status-aware — `404` logs at **Debug** (with the same
RU/diagnostic payload for those who opt in), while every other `CosmosException`
(429, 503, 408, auth failures, etc.) stays at **Error**. Audit the sibling
partial files (`.read.cs`, `.exists.cs`, `.query.cs`, and the
`DefaultProtectedRepository` variants) for the same pattern while in there.

Deferred to the major because observable logging severity is part of the
package's operational contract — consumers may have alerting or log-volume
budgets built around current behavior, and a severity change lands cleanest
behind a major-version boundary with migration notes.
