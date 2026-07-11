# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

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
