# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Updated

- Updated NuGet packages (Cirreum spine 4.2.0 wave: `Cirreum.Contracts` 4.2.0 / `Cirreum.Domain` 4.2.0 and current patch releases).

## [4.0.2] - 2026-07-31

### Updated

- Updated NuGet packages (Cirreum spine 4.0.1 wave: `Cirreum.Contracts` 4.0.1 / `Cirreum.Domain` 4.0.1 / `Cirreum.Kernel` 2.0.1 / `Cirreum.AuthenticationProvider` 2.0.3).

## [4.0.1] - 2026-07-30

### Security

- **`IProtectedRepository<T>.CreateAsync` verifies the authorized parent IS the persisted
  parent.** It previously authorized the caller against the `parentResourceId` argument without
  checking it matched the entity's declared `ParentResourceId` — a caller could authorize against
  a parent they held rights on while persisting the entity under (and inheriting the ACL of) a
  different parent nobody authorized. A mismatch now throws `ArgumentException` before any
  permission check or write.

### Fixed

- **ACL ancestor-chain patches and the move-cascade query now use the instance's configured
  serializer naming** (`[JsonPropertyName]` → configured naming policy) instead of hardcoded
  camelCase strings. Under a snake/kebab naming policy the previous behavior silently wrote a
  phantom camelCase property, leaving the real ancestor chain unmaintained. The expression-based
  `PatchOperationBuilder` paths honor the configured naming policy for the same reason.
- **`CreateAsync` returns the persisted entity** — after the ancestor-chain patch, the entity is
  re-read so callers observe the materialized `AncestorResourceIds` instead of the pre-patch
  values.
- **In-memory repository applies path-based patch operations.** `SetByPath`/`ReplaceByPath`/
  `AddByPath`/`RemoveByPath` were silently ignored by `InMemoryRepository.UpdatePartialAsync`
  (only expression-based operations were applied), so the protected repository's ancestor patches
  were no-ops in tests. Path operations now resolve back to the entity property
  (`[JsonPropertyName]` → camelCase → case-insensitive name) and are applied. The partial-update
  item lookup also compared the raw partition-key string to `PartitionKey.ToString()` (the JSON
  form) and could never match — it now compares constructed `PartitionKey` values as
  `RestoreAsync` does.
- **`MoveAsync` documents its cascade semantics** — the descendant cascade is not atomic;
  re-running the move with the same target heals a partially-updated tree.
- **First test suite** (`tests/Cirreum.Persistence.Azure.Tests`): protected-repository create/move
  enforcement, parent cross-check, ancestor materialization, patch-path naming, and in-memory
  patch application. README and internal docs corrected (the ACL surface intentionally does not
  extend `IRepository<T>`; the authorization-pipeline dependency and in-memory raw-SQL limitation
  are now stated).

### Updated

- Re-pinned `Cirreum.Domain` `2.0.0` → `3.0.0` (restores operation-authorization enforcement;
  carries the `IPolicyAuthorizer` rename — see Cirreum.Domain `MIGRATION-v3.md`).

## [4.0.0] - 2026-07-29

### Breaking

- **Identity-based authentication with `IsAutoResourceCreationEnabled` is rejected at startup.**
  When the connection value is an account endpoint URI (identity auth), registration throws
  `NotSupportedException` if `IsAutoResourceCreationEnabled` is `true`: Cosmos DB data-plane RBAC
  cannot create databases or containers, so auto-creation under identity auth 403s at the first
  missing resource — at runtime, in production, after startup looked healthy. The flag defaults to
  `true`, so **every existing endpoint-auth configuration must now set it to `false` explicitly**
  and provision resources as infrastructure-as-code. Key-based connection strings are unaffected.
  See `MIGRATION-v4.md`.

### Added

- **Configurable credentials for identity-based authentication** via the nested `Credential` block
  from `Cirreum.ServiceProvider` 1.1.0 (`Mode`: `Default` / `ManagedIdentity` / `Developer`, plus
  optional `IdentityId` selecting a user-assigned managed identity). The endpoint path previously
  hardcoded `new DefaultAzureCredential()` with no options — no tenant pinning, no identity
  selection.
- `Identifier` on the instance settings now resolves as the Entra tenant, forwarded to every
  tenant-aware credential.
- A `Credential` block alongside a key-based connection string fails at startup with
  `InvalidOperationException` — identity configuration cannot apply to key authentication, and
  silently ignoring it would misrepresent how the instance authenticates.
- An unrecognized `CredentialMode` value fails at startup instead of silently degrading to the
  default chain.

## [3.0.0] - 2026-07-27

### Added

- **`AzureCosmosDefaults.HttpClientName`** — Cosmos gateway traffic now goes through a *named*
  `IHttpClientFactory` client (`"Cirreum.Cosmos"`) instead of the factory's unnamed one. Previously
  the only way to shape the handler underneath Cosmos was `ConfigureHttpClientDefaults`, which
  reaches every default client in the application rather than just this one:

  ```csharp
  builder.Services.AddHttpClient(AzureCosmosDefaults.HttpClientName)
      .ConfigurePrimaryHttpMessageHandler(() => new SocketsHttpHandler {
          PooledConnectionLifetime = TimeSpan.FromSeconds(30),
          ConnectTimeout = TimeSpan.FromSeconds(2)
      });
  ```

  The framework names the client but deliberately does **not** configure it. The right handler values
  are environment-specific — aggressive recycling suits a local emulator behind a proxy and causes
  needless churn against real Azure — and `ConfigurePrimaryHttpMessageHandler` is last-write-wins, so
  a framework-supplied handler would fight the application's own. Stock defaults stay in place unless
  the application says otherwise, so nothing changes for anyone who doesn't opt in.

### Fixed

- **A blank `ApplicationName` no longer reaches the Cosmos SDK.** The user-agent name was built with
  `settings.ApplicationName ?? "Cirreum"`, which substitutes the default for `null` but not for an
  empty or whitespace string — and the SDK rejects `""` as an HTTP User-Agent value, so an instance
  configured with a blank name failed at client construction rather than falling back. The check is
  now `IsNullOrWhiteSpace`.

  The framework's own identity is also folded in: `ApplicationName` becomes
  `"{yours} cirreum/{version}"`, or just `cirreum/{version}` when none is configured. It previously
  went on the `HttpClient`'s default request headers, where the SDK — which sets `User-Agent` per
  request from its own container plus `ApplicationName` — would likely have overwritten it. One
  mechanism now, and it is the SDK's documented one.
- **Container resolution is now cached per service key instead of repeated on every operation.**
  Every repository call — read, write, delete, count, query — began by resolving its container, and
  nothing cached the result. What that cost depends on `IsAutoResourceCreationEnabled`:

  - **Off** (the production posture): `GetDatabase`/`GetContainer` are local constructions that never
    touch the network, so there is no latency change. What goes away is per-operation overhead — a
    keyed DI resolve, two SDK object allocations, and an async state machine on every read and write.
  - **On** (the default, and how development and bootstrapping run): resolving meant two Cosmos
    metadata round trips — `CreateDatabaseIfNotExistsAsync` and `CreateContainerIfNotExistsAsync`,
    each of which reads before it creates — paid on every operation for the life of the process.

  While the database and container did not yet exist, each of those reads returned **404** — so
  seeding a fresh service emitted a stream of expected not-founds through logs and, with distributed
  tracing enabled, through telemetry as failed dependency calls. That is the actual source of the
  not-found noise during seeding; the severity change below is a separate and smaller improvement.

  Resolution is now single-flighted per key, so a burst of concurrent first callers produces one
  resolution rather than one each. A failure is not cached — a transient error during startup would
  otherwise be permanent for the process.

  **Behavioural note:** auto-creation now runs once per key per process rather than on every
  operation. A database or container deleted underneath a running process is no longer silently
  recreated by the next call; operations against it fail until the process restarts. This only
  affects `IsAutoResourceCreationEnabled`, which is a development convenience rather than a
  production posture.

### Changed

- **A point read that finds nothing now logs at `Debug` instead of `Error`.** Event `20301` fired at
  `Error` severity for every miss — a 404 from Cosmos, or an item present but soft-deleted — even
  though the repository translates both into `NotFoundException`, which is the outcome `TryGet`
  semantics, existence probes and `Result`-pipeline mapping routinely expect. Every legitimate miss
  therefore reached production telemetry as a false alarm and could trip log-based alerting.

  Both of the event's call sites raise it for an ordinary miss, so this is not a narrowing of when
  `Error` is used — it is the whole event. Nothing else in the package logged a miss at `Error`; the
  one remaining `Error` (`ContainerFactory`, "failed to get container") is a genuine fault and is
  unchanged.

  **The event id `20301` is deliberately unchanged**, so log filters keyed on the number keep
  working. What changes is the severity, the message text, and the event name — `CosmosPointReadException`
  becomes `CosmosPointReadMiss`. The exception is still attached, so Cosmos diagnostics and the RU
  charge remain available to anyone enabling `Debug` for this category.

### Removed

- A stale duplicate `CHANGELOG.md` at the repository root, superseded by `docs/CHANGELOG.md` when the
  repo adopted the standard layout. It had been frozen at `2.0.0` since 2026-04-25 while the package
  shipped through `2.1.5`, and nothing referenced it — but it was the first thing a visitor to the
  repository saw.

## [2.1.5] - 2026-07-24

### Updated

- Updated NuGet packages.

## [2.1.4] - 2026-07-24

### Updated

- Updated NuGet packages.

## [2.1.3] - 2026-07-20

### Updated

- Updated NuGet packages.

## [2.1.2] - 2026-07-19

### Updated

- Updated NuGet packages.

## [2.1.1] - 2026-07-11

### Fixed

- Replaced leftover legacy `corr` naming with `cirreum`:
  - The Cosmos client's HTTP `User-Agent` product token is now `cirreum/{version}` (was `corr/{version}`).
  - The default `AzureCosmosInstanceSettings.DatabaseId` is now `cirreum-db` (was `corr-db`).

> **Upgrade note:** applications that relied on the default `DatabaseId` (i.e. never set it explicitly) will, after upgrading, target a database named `cirreum-db` instead of `corr-db`. With auto resource creation enabled (the default), an empty `cirreum-db` will be created and the existing `corr-db` data will appear missing. To keep using the existing database, set `"DatabaseId": "corr-db"` explicitly in configuration.

## [2.1.0] - 2026-07-11

### Added

- `AzureCosmosClientSettings` — a curated, configuration-bindable subset of `CosmosClientOptions`, exposed as `AzureCosmosInstanceSettings.ClientOptions`. Applications can now set safe Cosmos client knobs from configuration: `ConnectionMode`, `LimitToEndpoint`, `ConsistencyLevel`, `ApplicationRegion`/`ApplicationPreferredRegions`, request timeout and 429 retry settings, and gateway/direct-mode tuning. The headline case is `ConnectionMode: Gateway`, required by the Linux (vnext) Cosmos DB emulator and recommended for containerized workloads.

### Changed

- `AzureCosmosInstanceSettings.ClientOptions` now binds the curated `AzureCosmosClientSettings` instead of the raw SDK type. Configured values overlay the underlying `CosmosClientOptions` exactly once, before the `configureClientOptions` callback runs, so code-level configuration always wins over configuration-bound values. The raw SDK options remain internal (`SdkClientOptions`). Provider-managed options (`Serializer`, `HttpClientFactory`, `ApplicationName`, `EnableContentResponseOnWrite`, `AllowBulkExecution`) are not exposed to configuration.

> Note: a previously-inert `"ClientOptions"` block in an existing consumer's configuration will start taking effect for the curated options after upgrading (most significantly `ConnectionMode`). Review existing `"ClientOptions"` configuration before upgrading.

## [2.0.10] - 2026-07-07

### Updated

- Updated NuGet packages.

## [2.0.9] - 2026-07-05

### Updated

- Updated NuGet packages.

## [2.0.8] - 2026-07-04

### Updated

- Updated NuGet packages.

## [2.0.7] - 2026-07-04

### Updated

- Updated NuGet packages.

## [2.0.6] - 2026-07-04

### Updated

- Updated NuGet packages.

## [2.0.5] - 2026-05-10

### Updated

- Updated NuGet packages.

## [2.0.4] - 2026-05-07

### Updated

- Updated NuGet packages.

## [2.0.3] - 2026-05-01

### Updated

- Updated NuGet packages.
