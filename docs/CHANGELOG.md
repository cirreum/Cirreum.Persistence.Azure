# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

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

- **Container resolution is now cached per service key instead of repeated on every operation.**
  Every repository call — read, write, delete, count, query — began by resolving its container, and
  resolving meant two Cosmos metadata round trips: `CreateDatabaseIfNotExistsAsync` and
  `CreateContainerIfNotExistsAsync`, each of which reads before it creates. Nothing cached the
  result, so every operation paid both for the life of the process.

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
