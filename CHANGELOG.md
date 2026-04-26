# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

## [2.0.0] — 2026-04-25

### Changed (BREAKING)

- **`IProtectedRepository<T>` no longer extends `IRepository<T>`.** The unprotected repository surface is reachable only via the new `UseInnerRepositoryAsync` escape hatch. Handlers that previously called inherited `IRepository<T>` methods on a protected-repo reference must either inject `IRepository<T>` separately or move the call into a `UseInnerRepositoryAsync(...)` scope. Motivation: an inherited surface meant a handler could accidentally bypass ACL with no compiler warning. The type system now enforces an explicit, audited verb for every non-ACL operation.

### Added

- **`IProtectedRepository<T>.UseInnerRepositoryAsync` escape hatch** — two overloads (`void` + `TResult`) that scope a borrow of the underlying `IRepository<T>` for operations outside the ACL-aware surface (system maintenance, projections, cross-cutting reads). Every entry is logged at `Information` level (event id `20_101`) with entity type, caller member, file path, and line number for audit. Caller-info attributes live on the interface declaration so call-site values bind through interface dispatch.
- **Four-overload `DeleteAsync` pattern on `IProtectedRepository<T>`** — mirrors `IRepository<T>` with `void` (soft-delete only) and `Result<T>`-returning overloads accepting `bool softDelete = true`. Soft-delete plumbed end-to-end through the protected surface.
- **`InnerRepositoryScopeOpened` log event** (`EventIds.CosmosInnerRepositoryScopeOpenedId = 20_101`) and matching `LogInnerRepositoryScopeOpened<TEntity>` extension method.

### Changed

- **`Cirreum.Core` 3.0.4 → 4.0.0** (major dependency bump).
- **`Microsoft.Azure.Cosmos` 3.58.0 → 3.59.0**.
- **`Cirreum.ServiceProvider` 1.0.14 → 1.0.15**.
- **`DefaultRepository<T>.DeleteAsync(value, ct, softDelete)`** — collapsed an unreachable defensive `else` branch (the `IsAssignableFrom` check above the pattern match guarantees the cast succeeds). Reordered hard-delete branch before soft-delete branch for readability — the opt-in destructive path reads more naturally first.

### Removed

- **`DefaultProtectedRepository.delegated.cs`** (197 lines) — every member existed solely to satisfy the dropped `IRepository<T>` inheritance.
- **`IsEmulatorConnectionString` helper and `EmulatorAccountKey` constant** in `CosmosRegistrationExtensions` — unused since the last emulator-handling refactor. Also removed the unused `System.Data.Common` using directive.

### Fixed

- **Repository line-ending normalization** — added `* text=auto eol=crlf` to `.gitattributes` so all text files land in the working tree as CRLF regardless of contributor environment, plus `*.sh text eol=lf` as a defensive override. Resolves the "LF will be replaced by CRLF" warnings that surfaced when files created by tooling (or contributors with `autocrlf=input`) ended up in the index as LF.

### Documentation

- **README:** added sections covering the protected-resources surface (defining a protected entity, consuming from a handler, the escape hatch), soft-delete semantics with the safer-default rationale, and an updated Quick Start example using primary-constructor style.

## Pre-2.0.0

History prior to 2.0.0 is not tracked in this changelog. Refer to the Git log and GitHub Release notes for earlier versions.

[Unreleased]: https://github.com/cirreum/Cirreum.Persistence.Azure/compare/v2.0.0...HEAD
[2.0.0]: https://github.com/cirreum/Cirreum.Persistence.Azure/releases/tag/v2.0.0
