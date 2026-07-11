# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Added

- `AzureCosmosClientSettings` — a curated, configuration-bindable subset of `CosmosClientOptions`, exposed as `AzureCosmosInstanceSettings.ClientOptions`. Applications can now set safe Cosmos client knobs from configuration: `ConnectionMode`, `LimitToEndpoint`, `ConsistencyLevel`, `ApplicationRegion`/`ApplicationPreferredRegions`, request timeout and 429 retry settings, and gateway/direct-mode tuning. The headline case is `ConnectionMode: Gateway`, required by the Linux (vnext) Cosmos DB emulator and recommended for containerized workloads.

### Changed

- `AzureCosmosInstanceSettings.ClientOptions` now binds the curated `AzureCosmosClientSettings` instead of the raw SDK type. Configured values overlay the underlying `CosmosClientOptions` exactly once, before the `configureClientOptions` callback runs, so code-level configuration always wins over configuration-bound values. The raw SDK options remain internal (`SdkClientOptions`). Provider-managed options (`Serializer`, `HttpClientFactory`, `ApplicationName`, `EnableContentResponseOnWrite`, `AllowBulkExecution`) are not exposed to configuration.

> Note: a previously-inert `"ClientOptions"` block in an existing consumer's configuration will start taking effect for the curated options after upgrading (most significantly `ConnectionMode`). Review existing `"ClientOptions"` configuration before upgrading.

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
