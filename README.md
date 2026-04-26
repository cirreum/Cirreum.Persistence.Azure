# Cirreum.Persistence.Azure

[![NuGet Version](https://img.shields.io/nuget/v/Cirreum.Persistence.Azure.svg?style=flat-square&labelColor=1F1F1F&color=003D8F)](https://www.nuget.org/packages/Cirreum.Persistence.Azure/)
[![NuGet Downloads](https://img.shields.io/nuget/dt/Cirreum.Persistence.Azure.svg?style=flat-square&labelColor=1F1F1F&color=003D8F)](https://www.nuget.org/packages/Cirreum.Persistence.Azure/)
[![GitHub Release](https://img.shields.io/github/v/release/cirreum/Cirreum.Persistence.Azure?style=flat-square&labelColor=1F1F1F&color=FF3B2E)](https://github.com/cirreum/Cirreum.Persistence.Azure/releases)
[![License](https://img.shields.io/github/license/cirreum/Cirreum.Persistence.Azure?style=flat-square&labelColor=1F1F1F&color=F2F2F2)](https://github.com/cirreum/Cirreum.Persistence.Azure/blob/main/LICENSE)
[![.NET](https://img.shields.io/badge/.NET-10.0-003D8F?style=flat-square&labelColor=1F1F1F)](https://dotnet.microsoft.com/)

**Enterprise-grade Azure Cosmos DB persistence layer for .NET applications**

## Overview

**Cirreum.Persistence.Azure** provides a production-ready persistence layer for Azure Cosmos DB with NoSQL support. Built on the repository pattern, it seamlessly integrates with the Cirreum Foundation Framework to deliver consistent, scalable data access patterns.

## Key Features

- **Repository Pattern Implementation** - Clean abstraction over Azure Cosmos DB operations
- **Object-Level ACL** - `IProtectedRepository<T>` enforces per-entity permissions through `IResourceAccessEvaluator`, with hierarchy walks and per-request L1 caching
- **Soft Delete with Audit** - Opt-in soft-delete is the default destructive path; tracks `DeletedBy`, `DeletedOn`, and time zone via `IDeletableEntity`
- **Multi-Instance Support** - Keyed service registration for multiple database connections
- **Declarative Indexing Policies** - Attribute-driven indexing configuration on entity classes
- **In-Memory Testing** - Built-in in-memory repository for unit testing
- **Health Checks** - Native ASP.NET Core health check integration
- **Performance Optimized** - Bandwidth optimization and efficient query processing
- **Security Integrated** - User context tracking and Azure Identity support
- **Strongly Typed** - Full generic support with compile-time type safety

## Quick Start

```csharp
// Program.cs - Register with IHostApplicationBuilder
builder.AddCosmosDb("default", settings => {
    settings.ConnectionString = "AccountEndpoint=https://...;AccountKey=...";
    settings.DatabaseId = "MyDatabase";
    settings.OptimizeBandwidth = true;
});

// Inject and use repository (primary constructor)
public sealed class UserService(
    [FromKeyedServices("default")] IRepository<User> repository) {

    public Task<User?> GetUserAsync(string id, CancellationToken ct) =>
        repository.GetAsync(id, cancellationToken: ct);
}
```

## Configuration

### Programmatic Configuration

```csharp
// Simple connection string
builder.AddCosmosDb("default", "AccountEndpoint=https://...;AccountKey=...");

// Full configuration
builder.AddCosmosDb("default", settings => {
    settings.ConnectionString = "AccountEndpoint=https://...;AccountKey=...";
    settings.DatabaseId = "MyDatabase";
    settings.ApplicationName = "MyApp";
    settings.OptimizeBandwidth = true;
    settings.AllowBulkExecution = false;
    settings.IsAutoResourceCreationEnabled = true;
}, clientOptions => {
    clientOptions.ConnectionMode = ConnectionMode.Direct;
}, healthOptions => {
    healthOptions.ContainerIds = ["users", "orders"];
});
```

### Multiple Database Instances

```csharp
// Register multiple databases with keyed services
builder.AddCosmosDb("primary", "AccountEndpoint=https://primary.documents.azure.com;AccountKey=...");
builder.AddCosmosDb("analytics", "AccountEndpoint=https://analytics.documents.azure.com;AccountKey=...");

// Inject specific instance
public class AnalyticsService([FromKeyedServices("analytics")] IRepository<Event> repository)
{
    // Uses the analytics database connection
}
```

### appsettings.json Configuration

```json
{
  "ServiceProviders": {
    "Persistence": {
      "Azure": {
        "default": {
          "Name": "MyCosmosDb",
          "DatabaseId": "MyDatabase",
          "ApplicationName": "MyApp",
          "OptimizeBandwidth": true,
          "AllowBulkExecution": false,
          "IsAutoResourceCreationEnabled": true,
          "HealthOptions": {
            "ContainerIds": ["users", "orders"]
          }
        }
      }
    }
  }
}
```

The `Name` property is used to resolve the connection string via `Configuration.GetConnectionString(name)`. For production, store connection strings in Azure Key Vault using the naming convention `ConnectionStrings--{Name}` (e.g., `ConnectionStrings--MyCosmosDb`).

## Declarative Indexing Policy

When `IsAutoResourceCreationEnabled` is `true`, containers are auto-created with indexing policies defined directly on your entity classes via attributes from `Cirreum.Persistence.NoSql`:

```csharp
[Container("tasks")]
[PartitionKeyPath("/clientId")]
[IndexingPolicy(IndexingMode.Consistent, Automatic = true)]
[ExcludedPath("/description/*")]
[ExcludedPath("/content/*")]
[ExcludedPath("/*")]
public record TaskItem : Entity {

    [IncludedPath]
    [CompositeIndex("type-client-date", CompositePathSortOrder.Ascending, position: 0)]
    [CompositeIndex("type-client-status-date", CompositePathSortOrder.Ascending, position: 0)]
    public string Type { get; set; }

    [IncludedPath]
    [CompositeIndex("type-client-date", CompositePathSortOrder.Ascending, position: 1)]
    [CompositeIndex("type-client-status-date", CompositePathSortOrder.Ascending, position: 1)]
    public string ClientId { get; set; }

    [IncludedPath]
    [CompositeIndex("type-client-status-date", CompositePathSortOrder.Ascending, position: 2)]
    public string Status { get; set; }

    [IncludedPath]
    [CompositeIndex("type-client-date", CompositePathSortOrder.Descending, position: 2)]
    [CompositeIndex("type-client-status-date", CompositePathSortOrder.Descending, position: 3)]
    public DateTimeOffset CreatedAt { get; set; }

    public string Description { get; set; }
    public string Content { get; set; }

}
```

The resolver automatically derives JSON paths from `[JsonPropertyName]` attributes or camelCase property names. Entities without `[IndexingPolicy]` use the Cosmos DB default policy (auto-index all paths).

### Supported Attributes

| Attribute | Target | Description |
|-----------|--------|-------------|
| `[IndexingPolicy]` | Class | Sets indexing mode (`Consistent`, `Lazy`, `None`) and `Automatic` flag |
| `[IncludedPath]` | Property | Includes the property path in indexing (auto-derives `/{name}/?`) |
| `[ExcludedPath]` | Class | Excludes a path from indexing (supports wildcards) |
| `[CompositeIndex]` | Property | Groups properties into composite indexes by name, ordered by position |
| `[SpatialIndex]` | Property | Adds spatial indexing (`Point`, `LineString`, `Polygon`, `MultiPolygon`) |

## Soft Delete

Entities that implement `IDeletableEntity` participate in soft-delete semantics. The `softDelete` parameter on `DeleteAsync` defaults to `true` — destructive hard-delete requires explicit opt-in:

```csharp
// Soft delete (default) — sets DeletedBy / DeletedOn / IsDeleted, preserves the row
await repository.DeleteAsync(entity, ct);

// Hard delete — removes the row from Cosmos
await repository.DeleteAsync(entity, ct, softDelete: false);

// Restore a soft-deleted entity
var (restored, entity) = await repository.RestoreAsync(id, ct);
```

Soft-deleted entities are filtered out by default on reads. Pass `includeDeleted: true` to include them. Hard-delete and soft-delete on entities that don't implement `IDeletableEntity` will throw — the safer default forces a deliberate choice.

## Protected Resources (Object-Level ACL)

For entities that carry embedded ACLs, inject `IProtectedRepository<T>` instead of `IRepository<T>`. The protected repository composes `IResourceAccessEvaluator` and runs the ACL check before every operation, walking the resource hierarchy and applying root defaults as needed.

### Defining a protected entity

```csharp
[Container("documents")]
[PartitionKeyPath("/folderId")]
public sealed record DocumentEntity : Entity, IProtectedResource, IDeletableEntity {

    public required string FolderId { get; init; }

    // IProtectedResource — embedded ACL
    public string ResourceId => Id;
    public string? ParentResourceId => FolderId;
    public IReadOnlyList<string> AncestorResourceIds { get; init; } = [];
    public IReadOnlyList<AccessEntry> AccessList { get; init; } = [];
    public bool InheritPermissions { get; init; } = true;

    // IDeletableEntity — soft-delete fields
    public bool IsDeleted { get; set; }
    public DateTimeOffset? DeletedOn { get; set; }
    public string? DeletedBy { get; set; }
    public string? DeletedInTimeZone { get; set; }
}
```

### Consuming from a handler

`IProtectedRepository<T>` does **not** extend `IRepository<T>` — by design. The type system enforces that callers explicitly opt into ACL-bypass via `UseInnerRepositoryAsync` (audited; logged at Info level with caller file/line) rather than accidentally calling an unprotected method.

```csharp
public sealed class GetDocumentHandler(
    IProtectedRepository<DocumentEntity> repository
) : IOperationHandler<GetDocument, Document> {

    public async Task<Result<Document>> HandleAsync(GetDocument request, CancellationToken ct) {
        try {
            var entity = await repository.GetAsync(
                request.DocumentId,
                FolderPermissions.Document.Browse,
                ct);
            return entity.Map();
        } catch (NotFoundException) {
            return Result.NotFound<Document>(request.DocumentId);
        } catch (ForbiddenAccessException ex) {
            return ex; // implicit Exception → Result<Document>.Failure
        }
    }
}
```

For HTTP-only handlers, the `try/catch` blocks can be omitted entirely — the Cirreum default endpoint filter converts the propagating exceptions into Result-shaped HTTP responses. Add the catches when the handler may be composed into non-HTTP flows (sagas, queue consumers, scheduled jobs).

### Inner-repository escape hatch

When a handler genuinely needs the unprotected repository surface (system maintenance, projections, cross-cutting reads), use the audited escape hatch:

```csharp
await repository.UseInnerRepositoryAsync(async (inner, token) => {
    await inner.UpdatePartialAsync(documentId, ops => ops.Set(d => d.Indexed, true), cancellationToken: token);
}, ct);
```

Every call is logged at Info level with the entity type, caller member, file, and line — making ACL-bypass entry points trivially auditable.

## In-Memory Testing

For unit testing, use the built-in in-memory repository:

```csharp
// Register in-memory repository for testing
services.AddInMemoryCosmosRepository("test");

// Inject in tests
public class UserServiceTests
{
    private readonly IRepository<User> _repository;

    public UserServiceTests([FromKeyedServices("test")] IRepository<User> repository)
    {
        _repository = repository;
    }
}
```

## Contribution Guidelines

1. **Be conservative with new abstractions**  
   The API surface must remain stable and meaningful.

2. **Limit dependency expansion**  
   Only add foundational, version-stable dependencies.

3. **Favor additive, non-breaking changes**  
   Breaking changes ripple through the entire ecosystem.

4. **Include thorough unit tests**  
   All primitives and patterns should be independently testable.

5. **Document architectural decisions**  
   Context and reasoning should be clear for future maintainers.

6. **Follow .NET conventions**  
   Use established patterns from Microsoft.Extensions.* libraries.

## Versioning

Cirreum.Persistence.Azure follows [Semantic Versioning](https://semver.org/):

- **Major** - Breaking API changes
- **Minor** - New features, backward compatible
- **Patch** - Bug fixes, backward compatible

Given its foundational role, major version bumps are rare and carefully considered.

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

---

**Cirreum Foundation Framework**  
*Layered simplicity for modern .NET*