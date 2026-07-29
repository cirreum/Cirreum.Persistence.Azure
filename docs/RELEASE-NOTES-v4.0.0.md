# Cirreum.Persistence.Azure 4.0.0 — Real Credential Configuration for Identity Auth

## Why this release exists

Identity-based authentication (account endpoint + Entra) has been possible since the endpoint path
existed — but not configurable: the client was constructed with a bare `new DefaultAzureCredential()`.
No tenant pinning. No way to select a user-assigned identity on a host that has several. No
deterministic mode for production. And one trap: auto-resource-creation *appears* to work under
identity auth until the first missing resource 403s in production, because data-plane RBAC cannot
create databases or containers.

4.0.0 fixes both: the endpoint path reads the credential vocabulary every Cirreum provider now
shares, and the auto-creation trap becomes a startup error that names its fix.

## What's new

```json
"default": {
  "Name": "MyCosmosDb",
  "DatabaseId": "MyDatabase",
  "Identifier": "<tenant-id, optional>",
  "IsAutoResourceCreationEnabled": false,
  "Credential": { "Mode": "ManagedIdentity", "IdentityId": "<user-assigned-client-id>" }
}
```

- **`Credential` block** (from `Cirreum.ServiceProvider` 1.1.0): `Default` (full chain),
  `ManagedIdentity` (deterministic, resilient retries), or `Developer` (VS → CLI → PowerShell).
  `IdentityId` selects a user-assigned managed identity; under `Default` it pins the chain's
  managed-identity leg.
- **`Identifier` = Entra tenant**, forwarded to every tenant-aware credential.
- **Fail-fast guards:** identity auth + `IsAutoResourceCreationEnabled` is rejected as not
  supported; a `Credential` block alongside a key-based connection string is rejected as a
  contradiction; an unrecognized mode fails instead of silently defaulting.

## The breaking change

`IsAutoResourceCreationEnabled` defaults to `true`, so **every existing endpoint-auth instance
must now set it to `false`** (and have its resources provisioned as infrastructure-as-code) or the
application will not start. That is the entire migration for a correctly-provisioned deployment —
runtime behavior is otherwise identical. Key-based connection strings are unaffected entirely.
[MIGRATION-v4.md](MIGRATION-v4.md) has the walkthrough and the RBAC checklist.

## Compatibility

No repository API changes — `IRepository<T>`, protected repositories, batching, paging, health
checks, and serialization are untouched. The breaking surface is configuration-level and confined
to identity-auth instances.

## See also

- [MIGRATION-v4.md](MIGRATION-v4.md)
- [CHANGELOG](CHANGELOG.md)
- [Cirreum.Providers — Credential Configuration](https://github.com/cirreum/Cirreum.Providers#credential-configuration)
