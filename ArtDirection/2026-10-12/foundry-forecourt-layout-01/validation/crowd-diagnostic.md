# Stationary immortal-crowd diagnostic

This experiment exposed an unresolved pursuit/congestion limitation; it is **not** an accepted test result or proof that all crowds reach every forecourt position. The first failing experiment and its positions remain in `crowd-diagnostic/initial-failure*.trx`.

Setup: ten living 50-HP enemies enter around design `(-25,-25)` through `PlacePack(0,10)`. Slots 0–1 move at 1.6, the others at 2.2; spacing is 0.9 and attack reach 1.5. The hero is instantly placed at the target and held there for 3,600 steps of 1/60 s. No enemy dies. Every step checks floor clearance, swept floor validity and pair separation. The additional end premise requires every enemy beyond the southern join, within 3 units of the hero, and at least one at attack range.

## Bounds-only A/B

Both small projects link the same current pure production sources. Their copied `FoundryMap.cs` files differ in exactly one line: baseline `West=-36,South=-14` versus current `West=-32,South=-18`. This is an isolated bounds comparison, **not a full checkout/retest of the old commit**. Pursuit and crowd-solving code were not changed.

| Held hero target (design) | Old bounds | New bounds |
| --- | --- | --- |
| `(-10,-12)` (valid in both) | End premise fails; furthest actor 3.84 units away and crowd spills onto bridge mouth | Passes; all ten within 2.30 units |
| `(0,0)` (valid in both) | Passes | Passes; same final positions |
| `(0,-12)` (valid in both) | Nine actors stalled around the `(-14,-14)` waypoint, 14.13–17.22 units away | Passes; all ten within 2.73 units |
| `(-10,-15)` (new forecourt) | Not a valid old-layout target; deliberately not tested | Three actors stalled near `(-23,-23)`, 15.12–16.04 units away |

All tested cases pass their per-step floor/swept-path/separation checks. `current.trx` is **3 passed / 1 failed**; `baseline.trx` is **1 passed / 2 failed**. Exit code 1 is intentional evidence of the unmet end premise. These failures must not be counted among the accepted suite's passes.

`PackMotion` explicitly permits waiting behind occupied stopping points and allows only slides that approach the current target. The old-bounds counterexample establishes that distant waypoint queues predate this layout change. It does not establish that the new forecourt is immune, or resolve those queues. A separate gameplay slice would need to investigate crowd progress around fixed pursuit waypoints; this visual/layout package leaves that limitation open.

## Separate accepted integration case

`ActualTenEnemyPackFollowsAWalkingHeroAcrossTheBridgeJoinAndForecourt` uses real `HeroMotion(2.5)` along `(-25,-25) -> (-18,-18) -> (-10,-15) -> (0,-15)`, with the same ten living actors and all per-step constraints. Every actor crosses the join and finishes within 3 units, with at least one at attack range. This replaces the new experiment's unsupported stationary-immortal end premise; it does not relax or ignore any pre-existing test. Full pure Release suite: **438 passed / 0 failed / 0 skipped**, recorded separately in the package's accepted validation.

## Reproduce retained failures

From the repository root, run each command separately:

```powershell
dotnet test ArtDirection/2026-10-12/foundry-forecourt-layout-01/validation/crowd-diagnostic/current/Diagnostic.csproj --logger 'trx;LogFileName=current.trx' --results-directory ArtDirection/2026-10-12/foundry-forecourt-layout-01/validation/crowd-diagnostic
dotnet test ArtDirection/2026-10-12/foundry-forecourt-layout-01/validation/crowd-diagnostic/baseline/Diagnostic.csproj --logger 'trx;LogFileName=baseline.trx' --results-directory ArtDirection/2026-10-12/foundry-forecourt-layout-01/validation/crowd-diagnostic
```

The retained harness contains source/project files and TRX only. `source-hashes.json` identifies the linked repository dependencies used for these runs; future reproduction requires those sources. Generated `bin` and `obj` were moved to `TestResults/FoundryForecourtDiagnosis/retained-harness-build`, outside the proof. No Unity, phone, balance or performance acceptance is implied.

Retained source copies have trailing empty EOF lines normalized after the diagnostic runs; no executable text changed. For those four files, source-hashes.json preserves test_run_sha256 alongside the retained sha256.
