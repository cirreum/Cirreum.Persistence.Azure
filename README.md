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
- **Multi-Instance Support** - Keyed service registration for multiple database connections
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

// Inject and use repository
public class UserService
{
    private readonly IRepository<User> _userRepository;

    public UserService([FromKeyedServices("default")] IRepository<User> userRepository)
    {
        _userRepository = userRepository;
    }

    public async Task<User?> GetUserAsync(string id, CancellationToken ct)
    {
        return await _userRepository.GetAsync(id, cancellationToken: ct);
    }
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