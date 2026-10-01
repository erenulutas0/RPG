# Foundry day pass — 2026-10-01

Two helper agents (each owning its own files) and a read-only review; integration, captures, gate, commits and the
device install by the main session. Matched actual Unity captures: `../foundry-night-01/after/` (before) and
`after/`; `index.html` sets them side by side.

## What changed (commits b529201..90a2c35)

- **Molten floor in the furnace pit** (`FoundryRegionMassing.cs`): from the gameplay camera the coal bed 3.2 below
  the deck was hidden by the near rail. A molten pool now lies 2.0 below the deck between the inner boilers, the
  lining warms toward it in bands with a bright waterline seam and a translucent fire wash, the inner boilers' drums
  reach it with a warm rim; the rounded collar's corner stone has a lip, a chamfer and a dark lower step.
- **Bridge west rail ends at the court** (`FoundryRegionView.cs`, `FoundryRegionSite.cs`; found by a code review): the
  west edge runs inside the court's west room from (-14,-10) to (-18,-14), so its coping, wall and kerb stood in the
  middle of walkable court and drew over the v=-10 brass lane; they now end where the bridge leaves the court.
- **Lantern posts along the bridge rails** (`FoundryRegionSite.cs`, light pools in `FoundryRegionFinish.cs`): one per
  rail halfway along the causeway; two per rail overlapped on screen into one tall stacked pillar, so the main session
  reduced it to one.

## Budget, gate and device

- Static budget: 17 renderers (limit unchanged), 71,456 -> 73,296 vertices.
- Full gate: EditMode 410/410, PlayMode 134/134, Android build exit 0; integrity 525 / 1,099 / 568 passed.
- APK 37,158,301 bytes, SHA-256 `F67765262EB7CBA3FE53285DB4288059375BD813E652B389891E04A7D1B67E67`, **installed** on the
  S23 after the owner chose "install now" over a paused run (Floor 1 room 3/6, level 3, 11 unbanked gold;
  `pre-install-owner-run.png`). Hash matches; profile revision 221 unchanged; backups in
  `TestResults/foundry-day-install-20261001`. **Not looked at on the phone:** the phone locked right after the launch,
  so the guarded drags and screenshots were skipped. Logcat clean.

## Limits

- The pit's translucent wash is the region's first use of vertex alpha in static meshes; the colour test covers RGB.
- The bridge lanterns stand in the rail's screen column, close in front of the gatepost and behind the far pier.
