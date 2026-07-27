# Migration to Cirreum.Persistence.Azure v3.0

**From:** `Cirreum.Persistence.Azure 2.x`
**To:** `Cirreum.Persistence.Azure 3.0.0`

## Why v3

One change, and it is not a code change: **a point read that finds nothing now logs at `Debug`
instead of `Error`.**

No API changed. No behaviour changed. Nothing needs recompiling. The major exists because observable
log severity is part of a package's operational contract — deployments build alerting rules and
log-volume budgets around it, and quietly changing what shows up at `Error` is exactly the kind of
thing that should be announced rather than discovered.

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

## What didn't change

- Every public type, method and signature
- `NotFoundException` behaviour: a miss still throws, exactly as before
- Every other log event, id and severity in the package
- Soft-delete semantics, ACL/protected-repository behaviour, query and paging
- Configuration, registration and health checks
