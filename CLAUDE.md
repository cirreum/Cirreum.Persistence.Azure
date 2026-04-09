# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

This is **Cirreum.Persistence.Azure**, a persistence library using Azure Cosmos DB for NoSQL, built on .NET 10.0. It's part of the Cirreum Foundation Framework and provides a repository pattern implementation for Azure Cosmos DB operations.

## Development Commands

### Building the Project
```bash
dotnet build
dotnet build --configuration Release
```

### Running Tests
```bash
# Search for test projects first - none found in current structure
# Tests may be in a separate solution or repository
```

### Package Management
```bash
dotnet pack
dotnet restore
```

### Code Quality
- The project uses EditorConfig for consistent formatting (`.editorconfig`)
- Nullable reference types are enabled
- Documentation file generation is enabled

## Architecture Overview

### Core Components

**Repository Pattern Implementation:**
- `DefaultRepository<TEntity>` - Main Azure Cosmos DB repository implementation
- `InMemoryRepository<TEntity>` - In-memory repository for testing/development
- Both implement `IRepository<TEntity>` from `Cirreum.Persistence`

**Service Registration:**
- `AzureCosmosRegistrar` - Auto-registers Azure Cosmos DB services
- Uses `ServiceProviderRegistrar` pattern from `Cirreum.ServiceProvider`
- Supports keyed service resolution for multiple database instances

**Configuration System:**
- `AzureCosmosSettings` - Root configuration settings
- `AzureCosmosInstanceSettings` - Per-instance configuration
- `AzureCosmosItemConfiguration` - Entity-specific configuration
- Settings are managed via `InstanceSettingsRegistry`

**Internal Architecture:**
- `Factories/` - Container and client factory abstractions
- `Processors/` - Query processing logic (`ICosmosQueryableProcessor`)
- `Providers/` - Cosmos client provider abstractions
- `Resolvers/` - Container name, partition key, unique key, and indexing policy resolution

### Key Patterns

**Indexing Policy Resolution:**
- `IndexingPolicyResolver` reads `[IndexingPolicy]`, `[IncludedPath]`, `[ExcludedPath]`, `[CompositeIndex]`, and `[SpatialIndex]` attributes from entity types
- Builds a `Microsoft.Azure.Cosmos.IndexingPolicy` object used during container auto-creation
- Returns `null` when no `[IndexingPolicy]` attribute is present (Cosmos defaults apply)
- Path auto-derivation: explicit path on attribute → `[JsonPropertyName]` → camelCase via `Camelize()`
- Composite indexes are grouped by `GroupName` and ordered by `Position`

**Partial Classes:** Repository functionality is split across multiple partial class files:
- `DefaultRepository<TEntity>`: `.create.cs`, `.read.cs`, `.update.cs`, `.delete.cs`, `.query.cs`, `.batch.cs`, `.count.cs`, `.exists.cs`, `.paging.cs`
- `DefaultProtectedRepository<TEntity>`: `.create.cs`, `.read.cs`, `.update.cs`, `.delete.cs`, `.query.cs`, `.move.cs`, `.delegated.cs`

**Dependency Injection:** Heavy use of Microsoft DI with:
- Keyed services for multi-tenancy
- Factory pattern for Azure Cosmos containers
- Health check integration
- Auto-registration of `IProtectedRepository<>` → `DefaultProtectedRepository<>` and `IAccessEntryProvider<>` → `DefaultAccessEntryProvider<>` (both keyed and non-keyed) via `CosmosRegistrationExtensions`

**Security Integration:** Uses `IUserStateAccessor` from `Cirreum.Security` for user context

**Protected Resource System (ACL):**
- `IProtectedRepository<TEntity>` — extends `IRepository<TEntity>` with permission-aware CRUD operations for entities implementing `IProtectedResource`
- `DefaultProtectedRepository<TEntity>` — sealed implementation; splits across partial files (`.read.cs`, `.create.cs`, `.update.cs`, `.delete.cs`, `.query.cs`, `.move.cs`, `.delegated.cs`)
- `DefaultAccessEntryProvider<TEntity>` (internal, in `Internal/Providers/`) — zero-config `IAccessEntryProvider<T>` auto-registered via DI; implements `GetByIdAsync` and `GetManyByIdAsync` (Cosmos `ReadManyItemsAsync`). Other members use interface defaults
- `RepositoryAccessEntryProvider<TEntity>` — optional abstract base class for custom hierarchy logic; provides try/catch pattern for `GetByIdAsync` and batch `GetManyByIdAsync`
- Uses exception flow (throw on auth failure), not `Result<T>` pipeline — consistent with the persistence repo's convention
- `MoveAsync` — built-in reparenting with ancestor chain cascade to all descendants via `ARRAY_CONTAINS` query
- Auto-populates `AncestorResourceIds` on create (Cosmos patch via `SetByPath` to bypass init-only setters)
- `SupportsAncestorPath()` — cached `InterfaceMap` reflection to detect entity opt-in for materialized ancestors

## Framework Dependencies

- **Core Frameworks:** Microsoft.AspNetCore.App, Microsoft.Azure.Cosmos
- **Cirreum Libraries:** Cirreum.Core, Cirreum.Persistence, Cirreum.ServiceProvider
- **Identity:** Azure.Identity for authentication
- **Serialization:** Newtonsoft.Json (legacy), System.Text.Json (internal serializer)

## Build Configuration

- **Target Framework:** .NET 10.0
- **Language Version:** Latest C#
- **Nullable:** Enabled
- **Implicit Usings:** Enabled
- **CI/CD Detection:** Supports Azure DevOps and GitHub Actions
- **Versioning:** Local builds use 1.0.100-rc, CI uses proper semantic versioning

## Development Guidelines

### Code Style
- Uses tabs for indentation (EditorConfig configured)
- Pascal case for public members, interfaces prefixed with 'I'
- Expression-bodied properties preferred
- `this.` qualification required for properties/methods/events
- Block-scoped namespaces

### Testing Strategy
- `InMemoryRepository` for unit testing without Azure dependencies
- Health checks available via `AzureCosmosHealthCheck`
- InternalsVisibleTo configured for test assemblies (local builds only)

### Security Considerations
- Uses Azure Identity for authentication
- User context tracked via `IUserStateAccessor`
- Optimized bandwidth options for production scenarios