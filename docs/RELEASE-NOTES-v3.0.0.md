# Cirreum.Persistence.Azure 3.0.0

**No API changed and nothing needs recompiling.** This is a major because two observable behaviours
changed — what the package logs, and how often it talks to Cosmos — and both are things deployments
build alerting and dashboards around.

Full detail and upgrade steps: [`docs/MIGRATION-v3.md`](MIGRATION-v3.md).

## Why this release exists

Two defects that only show up at the edges: during seeding, and in logs.

A point read that found nothing logged at **`Error`**, with full Cosmos diagnostics attached. But a
miss is ordinary control flow — the repository turns it into `NotFoundException`, which is exactly
what `TryGet` semantics, existence probes and `Result`-pipeline mapping expect. Every legitimate miss
reached production telemetry as a false alarm and could trip log-based alerting.

Separately, every repository call re-resolved its container, and nothing cached the result. With
`IsAutoResourceCreationEnabled` on — **the default** — that meant two Cosmos metadata round trips per
operation, forever. While the database and container did not yet exist, each of those reads returned
404, so seeding a fresh service emitted roughly two expected not-founds per item written.

## What changed

### A point read that finds nothing logs at Debug

Event `20301` is now `Debug`. Both of its call sites raise it for an ordinary miss — a 404 from
Cosmos, or an item present but soft-deleted — so this is not a narrowing of when `Error` applies; the
entire event was the false alarm. Anything genuinely wrong (429, 503, timeouts, auth failures)
propagates from the Cosmos SDK and was never reported through this event at all.

**The event id `20301` is deliberately unchanged**, so log filters keyed on the number keep working.
The severity, message text and event name change — `CosmosPointReadException` becomes
`CosmosPointReadMiss`. The exception is still attached, so diagnostics and the RU charge remain
available to anyone enabling `Debug` for the `Cirreum.Persistence` category.

### Container resolution is cached per service key

What this buys depends entirely on your configuration:

- **Auto-creation off** — the production posture. `GetDatabase`/`GetContainer` are local
  constructions that never touch the network, so there is no latency change. What goes away is
  per-operation overhead: a keyed DI resolve, two SDK allocations, and an async state machine on
  every read and write.
- **Auto-creation on** — development, bootstrapping, and the default. Two metadata round trips per
  operation disappear. If you chart Cosmos metadata call volume, expect it to drop sharply.

Resolution is single-flighted, so a burst of concurrent first callers produces one resolution rather
than one each. Failures are not cached — a transient error during startup would otherwise be
permanent for the process.

**One behavioural change:** auto-creation runs once per key per process rather than on every
operation, so a container deleted underneath a running process is no longer silently recreated by the
next call. That only applies when auto-creation is enabled, and the self-healing was costing two
round trips per operation to provide.

## New

**`AzureCosmosDefaults.HttpClientName`** — gateway traffic now uses a named `IHttpClientFactory`
client, so you can shape the handler under Cosmos without reaching every other client in the
application. The framework names it but deliberately does not configure it; stock defaults apply
unless you opt in. The README covers the emulator dev-loop stall this exists to solve, and the one
property never to set on it.

**Bootstrap logging now says what happened.** Container creation reported "ensured it exists", which
answers the question a bootstrap run is asking — *which containers did I just create?* — for none of
them. It now reports `created` or `already existed` per container, at `Information`, and only when
auto-creation is enabled.

## Fixed

**A blank `ApplicationName` no longer fails client construction.** The user-agent name was built with
`settings.ApplicationName ?? "Cirreum"`, which substitutes the default for `null` but not for an
empty or whitespace string — and the SDK rejects `""` as a User-Agent value. An instance configured
with a blank name failed at construction instead of falling back. Callers setting a non-empty name
defensively to work around this no longer need to.

The framework's identity is also consolidated: `ApplicationName` becomes `"{yours} cirreum/{version}"`,
or `cirreum/{version}` when none is configured. It previously went on the `HttpClient`'s default
request headers, where the SDK — which sets `User-Agent` per request from its own container plus
`ApplicationName` — would likely have overwritten it.

## Documentation

`IsAutoResourceCreationEnabled` is now documented as what it is: a convenience for local development
and for seeding a new environment, to be **turned off in production**, where resources are normally
provisioned as infrastructure as code. That guidance previously existed only as a hedged sentence in
an XML doc comment, while both README samples showed it enabled with no caveat.

A stale duplicate `CHANGELOG.md` at the repository root — frozen at `2.0.0` since April while the
package shipped through `2.1.5` — has been removed.

## Compatibility

- Every public type, method and signature is unchanged
- `NotFoundException` behaviour is unchanged: a miss still throws
- Every other log event, id and severity is unchanged
- Soft-delete, ACL/protected-repository, query and paging behaviour are unchanged

## See also

- [`docs/MIGRATION-v3.md`](MIGRATION-v3.md) — upgrade steps, split by configuration
- [`docs/CHANGELOG.md`](CHANGELOG.md) — the full entry list
- `README.md` — tuning Cosmos HTTP traffic, and the auto-creation guidance
