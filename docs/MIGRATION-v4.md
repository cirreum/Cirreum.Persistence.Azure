# Migration to Cirreum.Persistence.Azure v4.0

**From:** `Cirreum.Persistence.Azure 3.x`
**To:** `Cirreum.Persistence.Azure 4.0.0`

## Why v4

The endpoint-authentication path grew a real credential surface (the shared `Credential` block),
and one long-standing trap became a startup error instead of a production surprise:

**Identity-based authentication with `IsAutoResourceCreationEnabled` is now rejected at
registration.** Cosmos DB data-plane RBAC cannot create databases or containers — those are
control-plane operations — so auto-creation under Entra auth has never actually worked. It merely
*looked* like it worked, because `CreateIfNotExists` reads before it creates: as long as every
resource already existed, nothing failed. The first genuinely missing database or container turned
into a 403 at runtime, in production, long after startup looked healthy. v4 moves that failure to
startup, where it names the fix.

## Who is affected

### You use an account endpoint URI as the connection value (identity auth)

`IsAutoResourceCreationEnabled` defaults to `true`, so **unless you already set it to `false`,
v4 will refuse to start** with:

> Identity-based authentication with IsAutoResourceCreationEnabled is not a supported
> configuration: Cosmos DB data-plane RBAC cannot create databases or containers. Set
> "IsAutoResourceCreationEnabled": false and provision resources as infrastructure-as-code.

**Action:** add `"IsAutoResourceCreationEnabled": false` to every endpoint-auth instance, and make
sure your databases and containers are provisioned (Bicep/Terraform/portal — anything but the app).
If the resources already exist, this is the *only* change the guard demands, and runtime behavior
is identical to 3.x afterwards.

### You use a key-based connection string

**No action.** Nothing about key authentication changes, including auto-creation. One new guard:
if you configure a `Credential` block *alongside* a key connection string, startup fails with
`InvalidOperationException` — the block cannot apply to key auth, and silently ignoring it would
misrepresent how the instance authenticates.

## New capabilities (endpoint auth)

3.x constructed `new DefaultAzureCredential()` with no options. v4 reads the shared `Credential`
block from `Cirreum.ServiceProvider` 1.1.0:

```json
"default": {
  "Name": "MyCosmosDb",
  "DatabaseId": "MyDatabase",
  "Identifier": "<tenant-id, optional>",
  "IsAutoResourceCreationEnabled": false,
  "Credential": { "Mode": "ManagedIdentity", "IdentityId": "<user-assigned-client-id>" }
}
```

- `Mode` — `Default` (full `DefaultAzureCredential` chain), `ManagedIdentity` (deterministic, no
  chain probing, with the SDK's resilient managed-identity retry behavior), or `Developer`
  (Visual Studio → Azure CLI → Azure PowerShell, as the signed-in developer).
- `IdentityId` — selects a user-assigned managed identity; under `Default` it pins the chain's
  managed-identity leg. Omit for system-assigned.
- `Identifier` — the Entra tenant, forwarded to every tenant-aware credential. New: 3.x had no
  tenant control at all.
- No `Credential` block — the `Default` chain, as before (now with tenant pinning available).

An unrecognized `Mode` value fails at startup instead of silently using the default chain.

## RBAC checklist for identity auth

The identity needs **data-plane** RBAC on the Cosmos account — the built-in
*Cosmos DB Data Contributor* role covers item CRUD, queries, and the `readMetadata` action the
health check needs to read the database. Note the asymmetry that motivates the guard: control-plane
roles (ARM *Contributor*) grant no data access, and data-plane roles grant no resource creation.

## Migration walkthrough

1. Update the package to `4.0.0` (pulls `Cirreum.ServiceProvider` ≥ 1.1.0).
2. For every instance whose connection value is an endpoint URI, set
   `"IsAutoResourceCreationEnabled": false` and confirm the database/containers are provisioned.
3. Optionally add a `Credential` block to make production auth deterministic
   (`"Mode": "ManagedIdentity"`, plus `IdentityId` on multi-identity hosts).
4. Confirm the identity holds data-plane RBAC (see checklist above).
5. Key-based instances: no changes.

## What didn't change

- Every repository type, method, and signature — no code changes for consumers
- Key-based authentication, including auto-creation under it
- Container resolution caching, soft-delete, ACL/protected repositories, query, paging, batch
- Health checks (under identity auth they need `readMetadata`, which Data Contributor includes)
- Serialization, the named HTTP client, and telemetry

## Downstream package impact

None — no Cirreum package depends on the endpoint-auth path's construction details. Applications
using endpoint auth are the affected consumers, via the auto-creation guard above.
