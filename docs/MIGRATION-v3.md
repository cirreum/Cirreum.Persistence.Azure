# Migration to Cirreum.Persistence.Azure v3.0

**From:** `Cirreum.Persistence.Azure 2.x`
**To:** `Cirreum.Persistence.Azure 3.0.0`

## Why v3

No API changed and nothing needs recompiling. Two observable behaviours changed, and both are the
kind that should be announced rather than discovered:

1. **Container resolution is cached** instead of repeated on every repository operation.
2. **A point read that finds nothing logs at `Debug`** instead of `Error`.

## Container resolution is cached per service key

Every repository call — read, write, delete, count, query — began by resolving its container, and
nothing cached the result. What that cost depends entirely on how you have
`IsAutoResourceCreationEnabled` set, so find yourself below.

### If auto-creation is off — the production posture

`GetDatabase`/`GetContainer` are local constructions that never touch the network, so there were no
round trips to remove and **no latency change to expect**. What goes away is per-operation overhead:
a keyed DI resolve, two SDK object allocations, and an async state machine, on every read and write.
Less allocation pressure on a hot path. Nothing to check, nothing to change.

### If auto-creation is on — development, bootstrapping, and the default

Resolving meant two Cosmos metadata round trips: `CreateDatabaseIfNotExistsAsync` and
`CreateContainerIfNotExistsAsync`, each of which reads before it creates. Every operation paid both,
for the life of the process. And while the database and container did not yet exist, each of those
reads returned 404 — so a fresh service emitted a stream of expected not-founds through logs and,
with distributed tracing enabled, through telemetry as failed dependency calls.

**That flag defaults to `true`**, so this applied to any deployment that had not explicitly turned it
off. If you have Cosmos metadata call volume on a dashboard, expect it to drop sharply.

**One behavioural change here:** auto-creation now runs once per key per process rather than on every
operation, so a database or container deleted underneath a running process is no longer silently
recreated by the next call — operations fail until restart. If you were relying on that self-healing,
it was costing two round trips per operation to provide, and it is a development convenience rather
than something to depend on in production.

### Both configurations

Resolution is single-flighted, so a burst of concurrent first callers produces one resolution rather
than one each. Failures are not cached — a transient error during startup would otherwise be
permanent for the process.

## A point read that finds nothing logs at Debug

## What changed

Event `20301` fired at `Error` for every point-read miss: a 404 from Cosmos, or an item present but
soft-deleted. The repository translates both into `NotFoundException`, which is the ordinary outcome
that `TryGet` semantics, existence probes and `Result`-pipeline mapping expect.

Both of the event's call sites raise it for a miss, so this was not "sometimes a false alarm" — the
entire event was. Anything genuinely wrong (429, 503, timeouts, auth failures) propagates from the
Cosmos SDK and was never reported through this event at all.

| | Before | After |
|---|---|---|
| Severity | `Error` | `Debug` |
| Event id | `20301` | `20301` — unchanged |
| Event name | `CosmosPointReadException` | `CosmosPointReadMiss` |
| Message | "Point read encountered an exception for item type…" | "Point read found no item of type…" |
| Attached exception | yes | yes — unchanged |

**The event id is deliberately unchanged.** It is what log pipelines filter on, and renumbering it
would have broken those filters far more disruptively than the severity change it would be tidying up
after. The id therefore stays in the `20_301+` band even though that band is labelled for errors; the
code says so where someone might otherwise "fix" it.

## What to do

**If you have an alert on `Error` from this package** — expect it to stop firing for point-read
misses. That is the point. Confirm the alert was not silently depending on this event as a proxy for
something else, then leave it alone; the one remaining `Error` in the package
(`ContainerFactory`, "failed to get container") is a genuine infrastructure fault and still fires.

**If you were relying on these entries for diagnostics** — enable `Debug` for the
`Cirreum.Persistence` category. The exception, Cosmos diagnostics and RU charge are all still
attached, so the payload is identical:

```json
{
  "Logging": {
    "LogLevel": {
      "Cirreum.Persistence": "Debug"
    }
  }
}
```

**If you filter by event name** rather than id, update `CosmosPointReadException` to
`CosmosPointReadMiss`. Filtering by the numeric id `20301` needs no change.

**Everything else** — no action.

## Also in this release — additive, no action

**Cosmos gateway traffic uses a named HTTP client.** `AzureCosmosDefaults.HttpClientName`
(`"Cirreum.Cosmos"`) replaces the factory's unnamed client, giving Cosmos its own handler seam:

```csharp
builder.Services.AddHttpClient(AzureCosmosDefaults.HttpClientName)
    .ConfigurePrimaryHttpMessageHandler(() => new SocketsHttpHandler { /* ... */ });
```

The framework names the client without configuring it, so stock defaults apply unless you opt in and
nothing changes for existing deployments. If you were tuning Cosmos via `ConfigureHttpClientDefaults`,
that still works — but you can now scope it to Cosmos alone instead of every default client in the
application.

## What didn't change

- Every public type, method and signature
- `NotFoundException` behaviour: a miss still throws, exactly as before
- Every other log event, id and severity in the package
- Soft-delete semantics, ACL/protected-repository behaviour, query and paging
- Configuration, registration and health checks
