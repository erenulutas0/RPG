# Foundry night pass — 2026-10-01

Three helper agents (each owning its own files) and a read-only code review; integration, captures, gate, commits
and the device check by the main session. Matched actual Unity captures: `../../2026-09-30/foundry-reference-pass-01/after/`
(before) and `after/`; `index.html` sets them side by side.

## What changed (commits 8c1c21e..d65e294)

- **Mixed-size ashlar court** (`FoundryRegionFinish.cs`): the 2x1 running bond that read as brickwork became a seeded
  packing of 1x1..3x3-unit flagstones with short, broken joints; brass lanes and compass rays are forced joints; the
  seal inset and west setts kept; light sampled on a shared unit lattice, refined near the fires.
- **Rounded furnace-pit collar** (`FoundryRegionMassing.cs`): the balustrade follows a rounded square (corner radius
  3.2), lantern pedestals on the corner arcs, solid collar stone in the corners, lining along the curve; the walkable
  hole is unchanged; lantern light pools moved with the lanterns.
- **Rim clutter and gate braziers** (new `FoundryRegionClutter.cs`): crates, barrels, a sack and an ingot pile in five
  clusters off the walkable floor (crane loading bay, crane footing roof, pipe shelf, two boiler yards), each in a depth
  scope; fire braziers with light pools on the gate's apron rail end posts.
- **Floor decals sort under raised structures** (`FoundryRegionView.cs`, review finding): decals now sort at the
  paving's rank and draw right after it, so the bridge's end kerb no longer paints across its coping and the west
  kerbs no longer paint over the crane footing.
- **Chain bronze under the colour ceiling** (`FoundryRegionDressing.cs`, review finding) and a new region test that
  every vertex colour x material tint stays in [0, 2) (`FoundryRegionTests`).

## Budget, gate and device

- Static budget: 17 renderers (limit unchanged), 63,644 -> 71,456 vertices.
- Full gate: EditMode 410/410, PlayMode 134/134, Android build exit 0; integrity 525 / 1,099 / 568 passed.
- APK 32,374,835 bytes, SHA-256 `81299787DDBDEFEE455560EF17F8102873C1CCC931787417A7B14A687487497D`, **installed** on the
  S23 (launcher in front, game not running; hash matches; profile revision 221 unchanged; backups in
  `TestResults/foundry-night-install-20261001`). `device-*.png`: the ashlar court with the compass, the rounded pit
  collar. `render-stats.log` (development RenderStatsProbe, flag removed afterwards): 59.9 fps at the vsync cap, GPU
  3.0-3.3 ms, 52-81 draw calls, 61k triangles, 128 MB. Logcat clean.

## Limits

- The pit's collar stone reads as a square base under the round rail at the corners.
- The ashlar's longest straight joints are the seal inset's 2x2 grid and the west setts, by design.
- No clutter along the court's south and west rail runs: the walkable floor reaches the rail there.
